using System.IO;
using System.Text.Json;
namespace CamoTracker;
public class Tracker
{
 public const string Official="https://www.callofduty.com/ca/en/blog/2026/10/call-of-duty-modern-warfare-4-ready-for-launch-progression-prestige";
 public static readonly JsonSerializerOptions JsonOptions=new(){WriteIndented=true};
 public TrackerData Data {get;private set;}
 public string? Path {get;}
 public string Notice {get;private set;}="本机记录";
 readonly Stack<string> history=[];
 public Weapon CurrentWeapon=>Data.Weapons.First(w=>w.Id==Data.SelectedWeapon);
 public Challenge Current=>CurrentWeapon.Challenges.First(c=>c.Id==Data.SelectedChallenge);
 public bool CanUndo=>history.Count>0;
 public Tracker(string? path)
 {
  Path=path;Data=Catalog.Create();
  if(path!=null&&File.Exists(path))try{Data=Parse(File.ReadAllText(path));if(Data.CatalogRevision<Catalog.Revision)File.Copy(path,path+".before-catalog-v3",true);}catch{Notice="存档损坏，已保留原文件";try{File.Copy(path,path+".damaged-"+DateTime.Now.ToString("yyyyMMddHHmmss"));}catch{}Data=Catalog.Create();}
  Catalog.Upgrade(Data);Normalize();
 }
 public static TrackerData Seed()=>Catalog.Create();
 public void Normalize()
 {
  if(Data.Weapons.Count==0)Data=Catalog.Create();foreach(var w in Data.Weapons)if(w.Challenges.Count==0)w.Challenges.Add(new());
  if(!Data.Weapons.Any(w=>w.Id==Data.SelectedWeapon))Data.SelectedWeapon=Data.Weapons[0].Id;
  if(!CurrentWeapon.Challenges.Any(c=>c.Id==Data.SelectedChallenge))Data.SelectedChallenge=CurrentWeapon.Challenges[0].Id;
  Data.SessionWeapons??=[];Data.SessionWeapons=Data.SessionWeapons.Where(id=>Data.Weapons.Any(w=>w.Id==id)).Distinct().ToList();
 }
 public static TrackerData Parse(string json)
 {
  if(json.Length>4_000_000)throw new InvalidDataException("文件超过 4 MB");var d=JsonSerializer.Deserialize<TrackerData>(json)??throw new InvalidDataException("空文件");
  if(d.Version!=1||d.Weapons==null||d.Weapons.Count is <1 or >200)throw new InvalidDataException("不支持的存档");var ids=new HashSet<string>();
  foreach(var w in d.Weapons){if(w==null||string.IsNullOrWhiteSpace(w.Name)||w.Name.Length>80||string.IsNullOrWhiteSpace(w.Id)||!ids.Add(w.Id)||w.Challenges==null||w.Challenges.Count is <1 or >200||w.Prestige is <0 or >2||w.WeaponLevel is <0 or >1000)throw new InvalidDataException("武器数据无效");foreach(var c in w.Challenges)if(c==null||string.IsNullOrWhiteSpace(c.Name)||c.Name.Length>100||c.Rule==null||c.Rule.Length>2000||c.Source==null||c.Source.Length>2000||c.Target is <1 or >1000000||c.Count<0||c.Count>c.Target||string.IsNullOrWhiteSpace(c.Id)||!ids.Add(c.Id))throw new InvalidDataException("挑战数据无效");}return d;
 }
 public void Snapshot(){if(history.Count>=100){var keep=history.Take(99).Reverse().ToArray();history.Clear();foreach(var s in keep)history.Push(s);}history.Push(JsonSerializer.Serialize(Data));}
 public void Change(int amount){Snapshot();Current.Count=(int)Math.Clamp((long)Current.Count+amount,0,Current.Target);Current.ProgressKnown=true;Save();}
 public void RecordTotal(Weapon w,Challenge c,int total){Snapshot();c.Count=Math.Clamp(total,0,c.Target);c.ProgressKnown=true;Save();}
 public void MarkUnlocked(Weapon w,Challenge c,bool unlocked)
 {
  Snapshot();c.Unlocked=unlocked;c.Count=unlocked?c.Target:0;c.ProgressKnown=true;
  if(unlocked)
  {
   IEnumerable<Challenge> implied=c.Kind switch{"military"=>w.Challenges.Where(p=>p.Kind=="military"&&p.RequiredWeaponLevel<=c.RequiredWeaponLevel),"gold"=>w.Challenges.Where(p=>p.Kind is "military" or "special"),"platinum"=>w.Challenges.Where(p=>p.Kind is "military" or "special" or "gold"),"harmonic"=>w.Challenges.Where(p=>p.Kind is "military" or "special" or "gold" or "platinum"),_=>[]};
   foreach(var p in implied){p.Unlocked=true;p.Count=p.Target;p.ProgressKnown=true;}
  }
  Save();
 }
 public void SetSession(Weapon w,bool selected){if(selected&&!Data.SessionWeapons.Contains(w.Id)&&Data.SessionWeapons.Count>=5)throw new InvalidOperationException("今天最多选 5 把武器。可先取消一把。");Snapshot();Data.SessionConfigured=true;if(selected){if(!Data.SessionWeapons.Contains(w.Id))Data.SessionWeapons.Add(w.Id);}else Data.SessionWeapons.Remove(w.Id);Save();}
 public bool InSession(Weapon w)=>Data.SessionWeapons.Contains(w.Id);
 public void Undo(){if(history.Count==0)return;Data=Parse(history.Pop());Normalize();Save();}
 public void Import(string json){var d=Parse(json);Snapshot();Data=d;Catalog.Upgrade(Data);Normalize();Save();}
 public void Save()
 {
  Normalize();if(Path==null)return;
  try{Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);File.WriteAllText(Path+".tmp",JsonSerializer.Serialize(Data,JsonOptions));if(File.Exists(Path))File.Copy(Path,Path+".bak",true);File.Move(Path+".tmp",Path,true);Notice="已保存";}catch(Exception e){Notice="保存失败："+e.Message;}
 }
}

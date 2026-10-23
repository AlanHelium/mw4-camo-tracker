using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace CamoTracker;

public class CatalogGate
{
 public string Scope {get;set;}="self";
 public string Kind {get;set;}="";
 public string ChallengeKey {get;set;}="";
 public int Count {get;set;}
 public string Label {get;set;}="前置挑战";
}
public class CamoAsset
{
 public string Key {get;set;}="";
 public string Image {get;set;}="";
 public int[] Crop {get;set;}=[];
 public string Caption {get;set;}="迷彩外观参考";
 public string Source {get;set;}="";
}
public class CatalogDefinition
{
 public int Schema {get;set;}=1;
 public string MinimumApp {get;set;}="0.7.0";
 public long Revision {get;set;}=2026102400;
 public string Updated {get;set;}="2026-10-24";
 public string Notes {get;set;}="首个可独立更新的数据包；33 把首发武器及现有迷彩资料。";
 public List<Weapon> Weapons {get;set;}=[];
 public List<WeaponArt.Entry> WeaponImages {get;set;}=[];
 public List<CamoAsset> CamoImages {get;set;}=[];
}
static class DataCatalog
{
 public static string Home {get;set;}=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CamoTracker","catalogs");
 public static CatalogDefinition Active {get;private set;}=Builtin();
 public static string? Root {get;private set;}
 public static string StartupNotice {get;private set;}="";
 public static T Clone<T>(T value)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
 public static CatalogDefinition Builtin()
 {
  using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("CamoTracker.baseline.json")!;
  return JsonSerializer.Deserialize<CatalogDefinition>(stream)!;
 }

 public static string Asset(string relative)=>Root==null?"":Path.Combine(Root,relative.Replace('/',Path.DirectorySeparatorChar));
 public static void Set(CatalogDefinition definition,string? root){Active=definition;Root=root;StartupNotice="";WeaponArt.Clear();OfficialArt.Clear();}
 public static void Initialize()
 {
  var pointer=Path.Combine(Home,"current.json");if(!File.Exists(pointer))return;
  try
  {
   var folder=JsonSerializer.Deserialize<string>(File.ReadAllText(pointer))!;
   if(!Regex.IsMatch(folder??"",@"^\d{10,14}-[a-f0-9]{12}(-[a-f0-9]{8})?$"))throw new InvalidDataException("数据目录无效");
   var root=Path.Combine(Home,folder!);var definition=JsonSerializer.Deserialize<CatalogDefinition>(File.ReadAllText(Path.Combine(root,"catalog.json")))!;
   DataUpdates.Validate(definition);if(definition.Revision<Active.Revision)return;
   foreach(var asset in definition.WeaponImages.Select(x=>"weapons/"+x.Image).Concat(definition.CamoImages.Select(x=>x.Image)))if(!File.Exists(Path.Combine(root,asset)))throw new InvalidDataException("图片文件缺失");
   Set(definition,root);
  }
  catch{StartupNotice="本地资料包无法读取，暂用内置资料。可重新导入数据包。";}
 }
 public static string? GateReason(TrackerData? data,Weapon w,Challenge c)
 {
  foreach(var gate in c.Gates)
  {
   IEnumerable<Weapon> weapons=gate.Scope=="self"?[w]:(data?.Weapons??[]).Where(x=>x.Category!="模式精通"&&!string.IsNullOrEmpty(x.CatalogKey)&&(gate.Scope!="category"||x.Category==w.Category));
   int total,done;
   bool Match(Challenge x)=>gate.ChallengeKey.Length>0?x.CatalogKey==gate.ChallengeKey:x.Kind==gate.Kind;
   if(gate.Scope=="self"){var goals=w.Challenges.Where(Match).ToList();total=goals.Count;done=goals.Count(x=>x.Done);}
   else{var guns=weapons.Where(x=>x.Challenges.Any(Match)).ToList();total=guns.Count;done=guns.Count(x=>x.Challenges.Any(y=>Match(y)&&y.Done));}
   int needed=gate.Count>0?gate.Count:total;
   if(total==0||done<needed)return $"{gate.Label} {done}/{Math.Max(1,needed)}";
  }
  return null;
 }
}

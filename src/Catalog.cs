using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace CamoTracker;
static class Catalog
{
 public const int Revision=3;
 static string MatchName(string s)=>Regex.Replace(s.Split('·')[0].ToLowerInvariant(),"[^a-z0-9]","");
 public static List<Weapon> Load()
 {
  using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("CamoTracker.catalog.json")!;
  var weapons=JsonSerializer.Deserialize<List<Weapon>>(stream)!;
  foreach(var w in weapons)
  {
   w.Id=Guid.NewGuid().ToString();foreach(var c in w.Challenges)c.Id=Guid.NewGuid().ToString();
   if(w.Name=="HAN 86")w.MaxLevel=72;if(w.Name=="Sang 9mm")w.MaxLevel=38;
   foreach(var (name,key,p,l,max) in new[]{("火山碎屑","pyroclast",1,1,false),("火山碎屑终点","terminus",1,1,true),("火山碎屑散落","scatter",2,150,false),("火山碎屑倾泻","cascade",2,250,false)})
    w.Challenges.Add(new Challenge{Name=name,Stage="武器巅峰",Kind="prestige",CatalogKey=key,ArtKey=key,RequiredPrestige=p,RequiredLevel=l,RequiresPrestigeMax=max,Target=1,Source=Tracker.Official,Rule=max?"武器巅峰 1 达到该武器满级。":$"{(p==1?"武器巅峰 1":"武器巅峰大师")}达到 {l} 级。等级奖励，更新等级即可记录。"});
  }
  var global=new Weapon{Name="模式精通",Category="模式精通",CatalogKey="global-mastery",Pinned=false};
  global.Challenges=[
   G("分歧","divergence",1,"完成 33 把武器的赤律迷彩。","divergence"),
   G("霞流","mp-1",250,"多人：单局 10 击杀，完成 250 局。","mastery-mp"),
   G("重铸原子","mp-2",300,"多人：无伤连续击杀 3 人，完成 300 次。","mastery-mp"),
   G("重铸猎户座","mp-3",500,"多人：不死亡连续击杀 5 人，完成 500 次。","mastery-mp"),
   G("赤流","dmz-1",2500,"DMZ：击杀 2500 名三级及以上敌人。","mastery-dmz"),
   G("涟漪潮","dmz-2",50,"DMZ：单次部署撤离 2 个副官狗牌，完成 50 次。","mastery-dmz"),
   G("日曜","dmz-3",25000,"DMZ：满级特战兵使用传说武器击杀 25000 敌人。","mastery-dmz"),
   G("幻彩","wz-1",1,"战区：条件尚未公布。","mastery-wz"),G("重铸大马士革","wz-2",1,"战区：条件尚未公布。","mastery-wz"),G("炽焰","wz-3",1,"战区：条件尚未公布。","mastery-wz")];
  weapons.Add(global);return weapons;
 }
 static Challenge G(string name,string key,int target,string rule,string kind)=>new(){Name=name,Stage="模式精通",CatalogKey=key,Target=target,Rule=rule,Kind=kind,Source=Tracker.Official,RequirementUnverified=kind=="mastery-wz"};
 public static TrackerData Create()
 {
  var d=new TrackerData{Weapons=Load(),CatalogRevision=Revision};var han=d.Weapons.First(w=>w.Name=="HAN 86");d.SelectedWeapon=han.Id;d.SelectedChallenge=han.Challenges.First(c=>c.CatalogKey=="special-1").Id;return d;
 }
 public static void Upgrade(TrackerData data)
 {
  if(data.CatalogRevision>=Revision)return;
  foreach(var template in Load())
  {
   var old=data.Weapons.FirstOrDefault(w=>MatchName(w.Name)==MatchName(template.Name));
   if(old==null){data.Weapons.Add(template);continue;}
   template.Id=old.Id;template.LevelKnown=old.LevelKnown;template.WeaponLevel=old.WeaponLevel;template.Prestige=old.Prestige;template.MaxLevel=old.MaxLevel>0?old.MaxLevel:template.MaxLevel;template.Pinned=old.Pinned;
   bool prepared=old.Challenges.Any(c=>c.IsPreparation&&(c.Count>=c.Target||c.Unlocked));
   foreach(var c in template.Challenges)
   {
    var previous=old.Challenges.FirstOrDefault(p=>(!string.IsNullOrEmpty(p.CatalogKey)&&p.CatalogKey==c.CatalogKey)||MatchName(p.Name)==MatchName(c.Name)||(!string.IsNullOrEmpty(c.ArtKey)&&p.ArtKey==c.ArtKey));
    if(previous!=null)
    {
     c.Id=previous.Id;c.Count=Math.Clamp(previous.Count,0,c.Target);c.ProgressKnown=previous.ProgressKnown||previous.Count>0;c.Unlocked=previous.Unlocked||(!previous.RequirementUnverified&&previous.Count>=previous.Target);c.OpenConfirmed=previous.OpenConfirmed;
     if(previous.Source.StartsWith("用户")){c.Rule=previous.Rule;c.Target=previous.Target;c.Count=previous.Count;c.RequirementUnverified=previous.RequirementUnverified;c.OpenConfirmed=true;}
     if(c.Name=="Red"&&previous.Target!=c.Target){c.Count=0;c.ProgressKnown=false;}
     if(data.SelectedChallenge==previous.Id)data.SelectedChallenge=c.Id;
    }
    if(prepared&&c.Kind is "military" or "special"){c.Unlocked=true;c.Count=c.Target;c.ProgressKnown=true;}
   }
   template.Challenges.AddRange(old.Challenges.Where(p=>p.Source.StartsWith("用户")&&!template.Challenges.Any(c=>c.Id==p.Id)&&!p.IsPreparation));
   data.Weapons[data.Weapons.IndexOf(old)]=template;
  }
  data.CatalogRevision=Revision;
 }
}

using System.Text.RegularExpressions;
namespace CamoTracker;
static class Catalog
{
 public const int Revision=3;
 static string MatchName(string s)=>Regex.Replace(s.Split('·')[0].ToLowerInvariant(),"[^a-z0-9]","");
 public static List<Weapon> Load()
 {
  var result=DataCatalog.Clone(DataCatalog.Active.Weapons);foreach(var w in result){w.Id=Guid.NewGuid().ToString();foreach(var c in w.Challenges)c.Id=Guid.NewGuid().ToString();}return result;
 }
 public static TrackerData Create()
 {
  var d=new TrackerData{Weapons=Load(),CatalogRevision=Revision,DataRevision=DataCatalog.Active.Revision};var han=d.Weapons.FirstOrDefault(w=>w.CatalogKey=="han-86")??d.Weapons.First();d.SelectedWeapon=han.Id;d.SelectedChallenge=(han.Challenges.FirstOrDefault(c=>c.CatalogKey=="special-1")??han.Challenges.First()).Id;return d;
 }
 public static void Upgrade(TrackerData data)
 {
  if(data.CatalogRevision>=Revision){if(data.DataRevision<DataCatalog.Active.Revision)Merge(data,DataCatalog.Active);return;}
  foreach(var template in Load())
  {
   var old=data.Weapons.FirstOrDefault(w=>w.CatalogKey==template.CatalogKey)??data.Weapons.FirstOrDefault(w=>string.IsNullOrEmpty(w.CatalogKey)&&MatchName(w.Name)==MatchName(template.Name));
   if(old==null){data.Weapons.Add(template);continue;}
   template.Id=old.Id;template.LevelKnown=old.LevelKnown;template.WeaponLevel=old.WeaponLevel;template.Prestige=old.Prestige;template.MaxLevel=old.MaxLevel>0?old.MaxLevel:template.MaxLevel;template.Pinned=old.Pinned;template.FocusChallengeId=old.FocusChallengeId;template.OverlayChallengeId=old.OverlayChallengeId;
   bool prepared=old.Challenges.Any(c=>c.IsPreparation&&(c.Count>=c.Target||c.Unlocked));
   foreach(var c in template.Challenges)
   {
    var previous=old.Challenges.FirstOrDefault(p=>(!string.IsNullOrEmpty(p.CatalogKey)&&p.CatalogKey==c.CatalogKey)||MatchName(p.Name)==MatchName(c.Name)||(!string.IsNullOrEmpty(c.ArtKey)&&p.ArtKey==c.ArtKey));
    if(previous!=null)
    {
     c.Id=previous.Id;c.Count=Math.Clamp(previous.Count,0,c.Target);c.ProgressKnown=previous.ProgressKnown||previous.Count>0;c.Unlocked=previous.Unlocked||(!previous.RequirementUnverified&&previous.Count>=previous.Target);c.OpenConfirmed=previous.OpenConfirmed;
     if(previous.Source.StartsWith("用户")){c.Source=previous.Source;c.Rule=previous.Rule;c.Target=previous.Target;c.Count=previous.Count;c.RequirementUnverified=previous.RequirementUnverified;c.OpenConfirmed=true;}
     if(c.Name=="Red"&&previous.Target!=c.Target){c.Count=0;c.ProgressKnown=false;}
     if(data.SelectedChallenge==previous.Id)data.SelectedChallenge=c.Id;
    }
    if(prepared&&c.Kind is "military" or "special"){c.Unlocked=true;c.Count=c.Target;c.ProgressKnown=true;}
   }
   template.Challenges.AddRange(old.Challenges.Where(p=>p.Source.StartsWith("用户")&&!template.Challenges.Any(c=>c.Id==p.Id)&&!p.IsPreparation));
   data.Weapons[data.Weapons.IndexOf(old)]=template;
  }
  data.CatalogRevision=Revision;
  Merge(data,DataCatalog.Active);
 }
 public static void Merge(TrackerData data,CatalogDefinition definition)
 {
  foreach(var template in DataCatalog.Clone(definition.Weapons))
  {
   var old=data.Weapons.FirstOrDefault(w=>w.CatalogKey==template.CatalogKey);
   if(old==null){template.Id=Guid.NewGuid().ToString();foreach(var c in template.Challenges)c.Id=Guid.NewGuid().ToString();data.Weapons.Add(template);continue;}
   template.Id=old.Id;template.LevelKnown=old.LevelKnown;template.WeaponLevel=old.WeaponLevel;template.Prestige=old.Prestige;template.Pinned=old.Pinned;template.FocusChallengeId=old.FocusChallengeId;template.OverlayChallengeId=old.OverlayChallengeId;
   var art=definition.WeaponImages.FirstOrDefault(x=>x.CatalogKey==template.CatalogKey);if(template.MaxLevel<=0)template.MaxLevel=art?.MaxLevel??old.MaxLevel;
   foreach(var c in template.Challenges.ToList())
   {
    var previous=old.Challenges.FirstOrDefault(x=>x.CatalogKey.Length>0&&x.CatalogKey==c.CatalogKey);
    if(previous==null){c.Id=Guid.NewGuid().ToString();continue;}
    c.Id=previous.Id;
    if(previous.Source.StartsWith("用户")){int index=template.Challenges.IndexOf(c);template.Challenges[index]=DataCatalog.Clone(previous);continue;}
    c.Unlocked=previous.Done;c.OpenConfirmed=previous.OpenConfirmed;
    if(c.ProgressRevision==previous.ProgressRevision){c.Count=Math.Min(previous.Count,c.Target);c.ProgressKnown=previous.ProgressKnown;}else{c.Count=0;c.ProgressKnown=false;c.OpenConfirmed=false;}
    if(c.Unlocked){c.Count=c.Target;c.ProgressKnown=true;}
   }
   template.Challenges.AddRange(old.Challenges.Where(x=>!template.Challenges.Any(c=>c.Id==x.Id)));
   data.Weapons[data.Weapons.IndexOf(old)]=template;
  }
  data.DataRevision=definition.Revision;data.CatalogRevision=Revision;
 }
}

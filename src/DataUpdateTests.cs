using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows;
namespace CamoTracker;
static class DataUpdateTests
{
 public static void Run()
 {
  void Assert(bool value,string message){if(!value)throw new Exception("Data updates: "+message);}
  var original=DataCatalog.Active;var originalRoot=DataCatalog.Root;var originalHome=DataCatalog.Home;
  var temp=Path.Combine(Path.GetTempPath(),"camo-updates-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
  try
  {
   DataCatalog.Home=Path.Combine(temp,"catalogs");var definition=DataCatalog.Builtin();DataUpdates.Validate(definition);
   var t=new Tracker(Path.Combine(temp,"progress.json"));var gun=t.Data.Weapons.First(w=>w.Name=="M4");var goals=gun.Challenges.Where(c=>c.Kind=="special").ToList();
   t.RecordTotal(gun,goals[0],7);t.MarkUnlocked(gun,goals[1],true);t.RecordTotal(gun,goals[2],4);gun.FocusChallengeId=goals[0].Id;gun.OverlayChallengeId=goals[0].Id;t.Data.SelectedWeapon=gun.Id;t.Data.SelectedChallenge=goals[0].Id;t.SetSession(gun,true);
   var custom=new Challenge{Name="自定义任务",Rule="保留我的条件",Target=9,Count=3,ProgressKnown=true};gun.Challenges.Add(custom);
   var customEdit=t.Data.Weapons.First(w=>w.Name=="HAN 86").Challenges.First(c=>c.Kind=="special");customEdit.Rule="用户修改内容";customEdit.Source="用户自定义 / 编辑";t.Save();
   definition.Revision++;definition.Notes="TEST ONLY";
   var changed=definition.Weapons.First(w=>w.CatalogKey==gun.CatalogKey);changed.Name="M4 Updated";changed.Challenges.First(c=>c.CatalogKey==goals[0].CatalogKey).Target=100;changed.Challenges.First(c=>c.CatalogKey==goals[0].CatalogKey).Rule="更新后的条件";
   changed.Challenges.First(c=>c.CatalogKey==goals[2].CatalogKey).ProgressRevision++;
   changed.Challenges.Add(new(){CatalogKey="new-challenge",Name="New",ChineseName="新挑战",EnglishName="New",Kind="custom",Target=10,Gates=[new(){Kind="gold",Count=1,Label="黄金"}]});
   var extra=DataCatalog.Clone(definition.Weapons.First());extra.Id="test-new-gun";extra.CatalogKey="test-new-gun";extra.Name="Test New Gun";extra.ChineseName="测试新增枪型";extra.EnglishName="Test New Gun";foreach(var c in extra.Challenges)c.Id=extra.CatalogKey+":"+c.CatalogKey;definition.Weapons.Add(extra);
   var art=DataCatalog.Clone(definition.WeaponImages.First());art.CatalogKey=extra.CatalogKey;art.ChineseName="测试新增枪型";art.EnglishName="Test New Gun";definition.WeaponImages.Add(art);
   var newCamo=DataCatalog.Clone(definition.CamoImages.First());newCamo.Key="test-new-art";newCamo.Caption="测试新增配图";definition.CamoImages.Add(newCamo);
   var zip=Pack(definition);using var prepared=DataUpdates.Prepare(zip);Assert(DataUpdates.Preview(definition).Contains("新增 1 把武器"),"actual diff preview");
   var bad=DataCatalog.Clone(definition);bad.Schema=2;bool refused=false;try{DataUpdates.Validate(bad);}catch(InvalidDataException){refused=true;}Assert(refused,"new schema refused");
   using(var memory=new MemoryStream()){using(var archive=new ZipArchive(memory,ZipArchiveMode.Create,true)){using(var writer=new StreamWriter(archive.CreateEntry("../escape.json").Open()))writer.Write("{}");using(var writer=new StreamWriter(archive.CreateEntry("catalog.json").Open()))writer.Write("{}");}refused=false;try{using var ignored=DataUpdates.Prepare(memory.ToArray());}catch(InvalidDataException){refused=true;}Assert(refused,"path traversal rejected");}
   // Fail after writing progress; the old save must be restored.
   var oldJson=File.ReadAllText(t.Path!);Directory.CreateDirectory(DataCatalog.Home);Directory.CreateDirectory(Path.Combine(DataCatalog.Home,"current.json.tmp"));
   refused=false;try{DataUpdates.Apply(t,prepared);}catch{refused=true;}
   Assert(refused&&File.ReadAllText(t.Path!)==oldJson&&DataCatalog.Active.Revision==original.Revision,"failed pointer commit restores progress");Directory.Delete(Path.Combine(DataCatalog.Home,"current.json.tmp"));
   using var retry=DataUpdates.Prepare(zip);var backup=DataUpdates.Apply(t,retry);Assert(File.Exists(backup),"pre-update backup exists");
   var updated=t.Data.Weapons.First(w=>w.CatalogKey==gun.CatalogKey);Assert(updated.Id==gun.Id&&updated.Name=="M4 Updated","renaming retains stable identity");
   Assert(updated.Challenges.First(c=>c.Id==goals[0].Id) is {Count:7,Target:100,ProgressKnown:true,Rule:"更新后的条件"},"partial total retained");
   Assert(updated.Challenges.First(c=>c.Id==goals[1].Id).Done,"unlocks retained");Assert(updated.Challenges.First(c=>c.Id==goals[2].Id) is {Count:0,ProgressKnown:false},"changed counter meaning resets unknown");
   Assert(updated.FocusChallengeId==goals[0].Id&&updated.OverlayChallengeId==goals[0].Id&&t.Data.SelectedChallenge==goals[0].Id&&t.Data.SessionWeapons.SequenceEqual(new[]{gun.Id}),"focus and plan retained");
   Assert(updated.Challenges.Any(c=>c.Id==custom.Id&&c.Count==3)&&t.Data.Weapons.SelectMany(w=>w.Challenges).Any(c=>c.Id==customEdit.Id&&c.Rule=="用户修改内容"&&c.Source.StartsWith("用户")),"custom and edited goals retained");
   var newGun=t.Data.Weapons.First(w=>w.CatalogKey==extra.CatalogKey);Assert(Bilingual.WeaponZh(newGun)=="测试新增枪型"&&WeaponArt.Image(newGun)!=null&&OfficialArt.Image("test-new-art")!=null&&OfficialArt.Caption("test-new-art")=="测试新增配图","new bilingual gun art and camo art load");
   var newGoal=updated.Challenges.First(c=>c.CatalogKey=="new-challenge");Assert(Eligibility.Reason(t.Data,updated,newGoal)!=null,"new prerequisite blocks");foreach(var c in updated.Challenges.Where(c=>c.Kind=="gold"))c.Unlocked=true;Assert(Eligibility.Reason(t.Data,updated,newGoal)==null,"new prerequisite resolves");
   var harmonic=updated.Challenges.First(c=>c.Kind=="harmonic");harmonic.Gates=[new(){Scope="all",Kind="platinum",Count=1,Label="测试门槛"}];Assert(Eligibility.Reason(t.Data,updated,harmonic)!=null,"configurable mastery threshold blocks");newGun.Challenges.First(c=>c.Kind=="platinum").Unlocked=true;Assert(Eligibility.Reason(t.Data,updated,harmonic)==null,"threshold not hard-coded to 33");
   DataCatalog.Set(original,null);DataCatalog.Initialize();Assert(DataCatalog.Active.Revision==definition.Revision&&new Tracker(t.Path).Data.SessionWeapons.SequenceEqual(new[]{gun.Id}),"installed pack and plan survive restart");
   var pointer=File.ReadAllText(Path.Combine(DataCatalog.Home,"current.json"));File.WriteAllText(t.Path!,"interrupted");File.WriteAllText(Path.Combine(DataCatalog.Home,"transaction.json"),JsonSerializer.Serialize(new{Progress=t.Path,Backup=backup,PreviousPointer=pointer}));DataUpdates.Recover();Tracker.Parse(File.ReadAllText(t.Path!));Assert(!File.Exists(Path.Combine(DataCatalog.Home,"transaction.json")),"crash recovery");
  }
  finally{DataCatalog.Home=originalHome;DataCatalog.Set(original,originalRoot);Directory.Delete(temp,true);}
 }
 static byte[] Pack(CatalogDefinition d)
 {
  using var memory=new MemoryStream();using(var zip=new ZipArchive(memory,ZipArchiveMode.Create,true))
  {
   using(var writer=new StreamWriter(zip.CreateEntry("catalog.json").Open()))writer.Write(JsonSerializer.Serialize(d));
   foreach(var path in d.WeaponImages.Select(x=>"weapons/"+x.Image).Concat(d.CamoImages.Select(x=>x.Image)).Distinct())
   {
    var resource="pack://application:,,,/Assets/"+(path.StartsWith("weapons/")?"Weapons/":"")+Path.GetFileName(path);
    using var source=Application.GetResourceStream(new Uri(resource)).Stream;using var target=zip.CreateEntry(path,CompressionLevel.Fastest).Open();source.CopyTo(target);
   }
  }
  return memory.ToArray();
 }
}

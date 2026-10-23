using System.IO;
using System.Text.Json;
namespace CamoTracker;
static class ModelTests
{
 public static int Run()
 {
  try
  {
   void Assert(bool value,string message){if(!value)throw new Exception(message);}
   var t=new Tracker(null);Assert(t.Data.Weapons.Count(w=>w.Category!="模式精通")==33,"33 launch weapons");Assert(t.Data.Weapons.Where(w=>w.Category!="模式精通").All(w=>w.Challenges.Count(c=>c.Kind=="special")==3),"99 specials");
   var han=t.CurrentWeapon;var smoke=t.Current;Assert(!smoke.ProgressKnown&&!smoke.Done,"unknown is not zero");t.Change(5);Assert(smoke.Count==5&&smoke.ProgressKnown,"record count");t.Undo();Assert(!t.Current.ProgressKnown&&t.Current.Count==0,"undo known flag");
   han=t.CurrentWeapon;smoke=t.Current;var gold=han.Challenges.First(c=>c.Kind=="gold");Assert(Eligibility.Reason(t.Data,han,gold)!=null,"gold prerequisites");t.MarkUnlocked(han,gold,true);Assert(han.Challenges.Where(c=>c.Kind is "military" or "special").All(c=>c.Done),"historical gold implies prerequisites");
   var platinum=han.Challenges.First(c=>c.Kind=="platinum");Assert(Eligibility.Reason(t.Data,han,platinum)!=null,"class gold gate");foreach(var w in t.Data.Weapons.Where(w=>w.Category==han.Category))t.MarkUnlocked(w,w.Challenges.First(c=>c.Kind=="gold"),true);Assert(Eligibility.Reason(t.Data,han,platinum)==null,"class gate satisfied");
   var harmonic=han.Challenges.First(c=>c.Kind=="harmonic");Assert(Eligibility.Reason(t.Data,han,harmonic)!=null,"33 platinum gate");
   var fresh=new Tracker(null);var m4=fresh.Data.Weapons.First(w=>w.Name=="M4");foreach(var c in m4.Challenges.Where(c=>c.Kind=="military"))fresh.MarkUnlocked(m4,c,true);var specials=m4.Challenges.Where(c=>c.Kind=="special").ToList();foreach(var c in specials)c.OpenConfirmed=true;fresh.RecordTotal(m4,specials[0],15);fresh.RecordTotal(m4,specials[1],19);Assert(Recommendations.ForWeapon(fresh.Data,m4)==specials[1],"closest eligible objective");
   fresh.SetSession(m4,true);fresh.SetSession(fresh.Data.Weapons.First(w=>w.Name=="HAN 86"),true);var plan=Recommendations.Next(fresh.Data,new HashSet<string>(),true);Assert(plan.Count==2&&plan[0].weapon==m4,"session follows user order");var json=JsonSerializer.Serialize(fresh.Data);Assert(Tracker.Parse(json).SessionWeapons.Count==2,"session persistence");fresh.SetSession(m4,false);Assert(Recommendations.Next(fresh.Data,new HashSet<string>(),true).Count==1,"unselect session");
   m4.FocusChallengeId=specials[0].Id;Assert(Recommendations.ForWeapon(fresh.Data,m4)==specials[0],"explicit objective overrides nearest");fresh.MarkUnlocked(m4,specials[0],true);Assert(Recommendations.ForWeapon(fresh.Data,m4)!=specials[0],"completed focus advances");
   var empty=new Tracker(null);empty.Data.SessionConfigured=true;Assert(Recommendations.Next(empty.Data,new HashSet<string>(),true).Count==0,"explicit empty plan stays empty");foreach(var w in empty.Data.Weapons.Take(5))empty.SetSession(w,true);bool limited=false;try{empty.SetSession(empty.Data.Weapons[5],true);}catch(InvalidOperationException){limited=true;}Assert(limited&&empty.Data.SessionWeapons.Count==5,"five weapon limit");
   var red=fresh.Data.Weapons.First(w=>w.Name=="HAN 86").Challenges.First(c=>c.Name=="Red");red.Count=red.Target;red.ProgressKnown=true;Assert(!red.Done,"conflicting source cannot complete");fresh.MarkUnlocked(fresh.Data.Weapons.First(w=>w.Name=="HAN 86"),red,true);Assert(red.Done,"actual historical unlock overrides uncertain text");
   var h=fresh.Data.Weapons.First(w=>w.Name=="HAN 86");var terminus=h.Challenges.First(c=>c.ArtKey=="terminus");h.LevelKnown=true;h.Prestige=1;h.WeaponLevel=71;Assert(Eligibility.Reason(fresh.Data,h,terminus)!=null,"prestige not max");h.WeaponLevel=72;Assert(Eligibility.Reason(fresh.Data,h,terminus)==null,"prestige max");var scatter=h.Challenges.First(c=>c.ArtKey=="scatter");h.Prestige=2;h.WeaponLevel=149;Assert(Eligibility.Reason(fresh.Data,h,scatter)!=null,"master 149");h.WeaponLevel=150;Assert(Eligibility.Reason(fresh.Data,h,scatter)==null,"master 150");
   var old=new TrackerData{Weapons=[new Weapon{Name="HAN 86",Challenges=[new Challenge{Name="Smoke · 烟雾",Count=7,Target=20,Source=Tracker.Official}]}]};old.SelectedWeapon=old.Weapons[0].Id;old.SelectedChallenge=old.Weapons[0].Challenges[0].Id;Catalog.Upgrade(old);Assert(old.Weapons.First(w=>w.Name=="HAN 86").Challenges.Any(c=>c.Count==7&&c.ProgressKnown),"legacy progress retained");
   var path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"camo-tests-"+Guid.NewGuid()+".json");try{var disk=new Tracker(path);disk.Change(8);Assert(new Tracker(path).Current.Count==8,"disk roundtrip");disk.Change(1);Assert(File.Exists(path+".bak"),"backup");File.WriteAllText(path,"invalid");var recovered=new Tracker(path);Assert(recovered.Notice.Contains("损坏"),"corrupt recovery");}finally{foreach(var f in Directory.GetFiles(System.IO.Path.GetDirectoryName(path)!,System.IO.Path.GetFileName(path)+"*"))File.Delete(f);}
   File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"test-results.txt"),"PASS: 33-weapon catalog, 99 specials, unknown progress, history, dependencies, nearest ready objective, session order/persistence/removal, source conflict, prestige boundaries, migration, disk backup/recovery");return 0;
  }
  catch(Exception e){File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"test-results.txt"),"FAIL: "+e);return 1;}
 }
}

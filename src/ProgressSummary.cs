namespace CamoTracker;
static class ProgressSummary
{
 public record Tally(int Done,int Total)
 {
  public double Percent=>Total==0?0:100.0*Done/Total;
  public double Remaining=>Total==0?0:100.0*(Total-Done)/Total;
 }
 public static IEnumerable<Weapon> Weapons(TrackerData data)=>data.Weapons.Where(w=>w.Category!="模式精通");
 public static Tally Collection(TrackerData data)
 {
  var items=Weapons(data).SelectMany(w=>w.Challenges).ToList();return new(items.Count(c=>c.Done),items.Count);
 }
 public static Tally Route(TrackerData data,string kind)
 {
  var goals=Weapons(data).SelectMany(w=>w.Challenges.Where(c=>kind=="all"||c.Kind==kind)).ToList();return new(goals.Count(c=>c.Done),goals.Count);
 }
 public static double? Percent(Challenge c)=>c.Done?100:c.ProgressKnown&&!c.RequirementUnverified?100.0*c.Count/c.Target:null;
 public static Challenge? Nearest(TrackerData data,Weapon w)=>w.Challenges.Where(c=>!c.Done&&!c.IsLevelReward&&c.ProgressKnown&&!c.RequirementUnverified&&Eligibility.Reason(data,w,c)==null).OrderByDescending(c=>1.0*c.Count/c.Target).ThenBy(c=>w.Challenges.IndexOf(c)).FirstOrDefault();
 public static List<(Weapon weapon,Challenge challenge)> Closest(TrackerData data,int count=3)=>Weapons(data).Select(w=>(weapon:w,challenge:Nearest(data,w))).Where(r=>r.challenge!=null&&r.challenge.Count>0).OrderByDescending(r=>1.0*r.challenge!.Count/r.challenge.Target).Take(count).Select(r=>(r.weapon,r.challenge!)).ToList();
}

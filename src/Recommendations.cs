namespace CamoTracker;
static class Recommendations
{
 public static Challenge? ForWeapon(TrackerData data,Weapon w)
 {
  var focused=w.Challenges.FirstOrDefault(c=>c.Id==w.FocusChallengeId&&!c.Done);if(focused!=null)return focused;
  return w.Challenges.Where(c=>!c.Done)
   .OrderBy(c=>Eligibility.Reason(data,w,c)==null?0:1)
   .ThenByDescending(c=>c.ProgressKnown?1.0*c.Count/c.Target:0)
   .ThenBy(c=>c.RequirementUnverified&&!c.GameRewardOnly?1:0).ThenBy(c=>w.Challenges.IndexOf(c)).FirstOrDefault();
 }
 public static List<(Weapon weapon,Challenge challenge)> Next(TrackerData data,ISet<string> skipped,bool session=false)
 {
  var weapons=data.Weapons.Where(w=>w.Pinned&&!skipped.Contains(w.Id));
  if(session&&data.SessionConfigured)weapons=data.SessionWeapons.Select(id=>data.Weapons.FirstOrDefault(w=>w.Id==id)).OfType<Weapon>().Where(w=>!skipped.Contains(w.Id));
  var rows=weapons.Select(w=>(weapon:w,challenge:ForWeapon(data,w))).Where(x=>x.challenge!=null).Select(x=>(x.weapon,challenge:x.challenge!));
  if(session&&data.SessionConfigured)return rows.ToList();
  return rows.OrderBy(r=>Eligibility.Reason(data,r.weapon,r.challenge)==null?0:1).ThenByDescending(r=>r.challenge.ProgressKnown?1.0*r.challenge.Count/r.challenge.Target:0).ThenBy(r=>r.weapon.PlayerUnlockLevel).Take(5).ToList();
 }
}

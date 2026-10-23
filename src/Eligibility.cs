namespace CamoTracker;
static class Eligibility
{
 public static string? Reason(Weapon w,Challenge c)=>Reason(null,w,c);
 public static string? Reason(TrackerData? data,Weapon w,Challenge c)
 {
  if(c.Done)return null;
  if(c.RequirementUnverified)return "条件待游戏内核对";
  if(c.Kind=="military")return null;
  if(c.Kind=="special"&&!c.OpenConfirmed)
  {
   if(c.LevelGateUnknown)return "请确认游戏中已开放挑战";
   if(c.RequiredWeaponLevel>0&&(!w.LevelKnown||w.WeaponLevel<c.RequiredWeaponLevel))return $"需武器等级 {c.RequiredWeaponLevel}";
  }
  if(c.Kind is "gold" or "platinum" or "harmonic")
  {
   int missing=w.Challenges.Count(p=>p.Kind is "military" or "special"&&!p.Done);
   if(missing>0)return $"还需 {missing} 项军事 / 特殊迷彩";
  }
  if(c.Kind is "platinum" or "harmonic")
  {
   if(!w.Challenges.Any(p=>p.Kind=="gold"&&p.Done))return "先解锁本武器黄金";
   var group=data?.Weapons.Where(p=>p.Category==w.Category&&!string.IsNullOrEmpty(p.CatalogKey)&&p.Category!="模式精通").ToList();
   if(group==null)return "需同类别全部黄金";
   int done=group.Count(p=>p.Challenges.Any(x=>x.Kind=="gold"&&x.Done));if(done<group.Count)return $"同类别黄金 {done}/{group.Count}";
  }
  if(c.Kind=="harmonic")
  {
   int total=data?.Weapons.Count(p=>p.Challenges.Any(x=>x.Kind=="platinum"&&x.Done))??0;if(total<33)return $"全武器白金 {total}/33";
  }
  if(c.Kind=="divergence")
  {
   int done=data?.Weapons.Count(p=>p.Challenges.Any(x=>x.Kind=="harmonic"&&x.Done))??0;if(done<33)return $"赤律 {done}/33";
  }
  if(c.Kind.StartsWith("mastery"))
  {
   var prefix=c.CatalogKey.Split('-')[0];int step=int.TryParse(c.CatalogKey.Split('-').Last(),out var n)?n:1;
   if(step==1&&!w.Challenges.Any(p=>p.Kind=="divergence"&&p.Done))return "先解锁分歧";
   if(step>1&&!w.Challenges.Any(p=>p.CatalogKey==prefix+"-"+(step-1)&&p.Done))return "先完成本模式前一阶段";
  }
  if(c.RequiredPrestige==0)return null;
  if(!w.LevelKnown)return "先记录武器巅峰状态";
  if(w.Prestige<c.RequiredPrestige)return c.RequiredPrestige==1?"先进入武器巅峰 1":"先进入武器巅峰大师";
  if(c.RequiresPrestigeMax&&w.Prestige==1){if(w.MaxLevel<=0)return "先记录该武器满级等级";if(w.WeaponLevel<w.MaxLevel)return "武器巅峰 1 需满级";}
  if(w.Prestige==c.RequiredPrestige&&w.WeaponLevel<c.RequiredLevel)return $"需{(c.RequiredPrestige==2?"巅峰大师":"武器巅峰")} {c.RequiredLevel} 级";
  return null;
 }
}

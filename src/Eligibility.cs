namespace CamoTracker;
static class Eligibility
{
 public static string? Reason(Weapon w,Challenge c)=>Reason(null,w,c);
 public static string? Reason(TrackerData? data,Weapon w,Challenge c)
 {
  if(c.Done)return null;
  if(c.RequirementUnverified)return "条件待游戏内核对";
  if(c.Kind=="military"&&(!w.LevelKnown||w.Prestige==0&&w.WeaponLevel<c.RequiredWeaponLevel))return $"先记录武器等级至 Lv.{c.RequiredWeaponLevel}";
  if(c.Kind=="special"&&!c.OpenConfirmed)
  {
   if(c.LevelGateUnknown)return "请确认游戏中已开放挑战";
   if(c.RequiredWeaponLevel>0&&(!w.LevelKnown||w.WeaponLevel<c.RequiredWeaponLevel))return $"需武器等级 {c.RequiredWeaponLevel}";
  }
  if(DataCatalog.GateReason(data,w,c) is {} gate)return gate;
  if(c.RequiredPrestige==0)return null;
  if(!w.LevelKnown)return "先记录武器巅峰状态";
  if(w.Prestige<c.RequiredPrestige)return c.RequiredPrestige==1?"先进入武器巅峰 1":"先进入武器巅峰大师";
  if(c.RequiresPrestigeMax&&w.Prestige==1){if(w.MaxLevel<=0)return "先记录该武器满级等级";if(w.WeaponLevel<w.MaxLevel)return "武器巅峰 1 需满级";}
  if(w.Prestige==c.RequiredPrestige&&w.WeaponLevel<c.RequiredLevel)return $"需{(c.RequiredPrestige==2?"巅峰大师":"武器巅峰")} {c.RequiredLevel} 级";
  return null;
 }
}

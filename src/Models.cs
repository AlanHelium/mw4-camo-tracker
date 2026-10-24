namespace CamoTracker;
public class Challenge
{
 public string ChineseName {get;set;}="";
 public string EnglishName {get;set;}="";
 public int ProgressRevision {get;set;}=1;
 public string Unit {get;set;}="次";
 public bool GameRewardOnly {get;set;}
 public string ReferenceRule {get;set;}="";
 public List<CatalogGate> Gates {get;set;}=[];
 public string Id {get;set;}=Guid.NewGuid().ToString();
 public string CatalogKey {get;set;}="";
 public string Name {get;set;}="自定义迷彩";
 public string Stage {get;set;}="特殊迷彩";
 public string Kind {get;set;}="custom";
 public string Rule {get;set;}="按游戏内要求记录";
 public string Source {get;set;}="用户自定义";
 public string SourceRule {get;set;}="";
 public string DmzRule {get;set;}="";
 public string DmzSourceRule {get;set;}="";
 public int DmzTarget {get;set;}
 public string ArtKey {get;set;}="";
 public int Target {get;set;}=50;
 public int Count {get;set;}
 public bool ProgressKnown {get;set;}
 public bool Unlocked {get;set;}
 public bool OpenConfirmed {get;set;}
 public bool IsPreparation {get;set;}
 public bool IsSingleMatch {get;set;}
 public int RequiredPrestige {get;set;}
 public int RequiredLevel {get;set;}
 public int RequiredWeaponLevel {get;set;}
 public bool LevelGateUnknown {get;set;}
 public bool RequiresPrestigeMax {get;set;}
 public bool RequirementUnverified {get;set;}
 public bool IsLevelReward=>Kind=="prestige"||Kind=="military"&&!GameRewardOnly;
 public bool Done=>Unlocked||(!RequirementUnverified&&ProgressKnown&&Count>=Target);
}
public class Weapon
{
 public string ChineseName {get;set;}="";
 public string EnglishName {get;set;}="";
 public string Id {get;set;}=Guid.NewGuid().ToString();
 public string CatalogKey {get;set;}="";
 public string Name {get;set;}="自定义武器";
 public string Category {get;set;}="自定义";
 public string Mode {get;set;}="多人模式";
 public bool Pinned {get;set;}=true;
 public string OverlayChallengeId {get;set;}="";
 public string FocusChallengeId {get;set;}="";
 public int PlayerUnlockLevel {get;set;}
 public bool LevelKnown {get;set;}
 public int WeaponLevel {get;set;}=1;
 public int MaxLevel {get;set;}
 public int Prestige {get;set;}
 public List<Challenge> Challenges {get;set;}=[];
}
public class TrackerData
{
 public long DataRevision {get;set;}
 public int Version {get;set;}=1;
 public int CatalogRevision {get;set;}
 public List<Weapon> Weapons {get;set;}=[];
 public string SelectedWeapon {get;set;}="";
 public string SelectedChallenge {get;set;}="";
 public string Mode {get;set;}="多人模式";
 public bool Topmost {get;set;}
 public double OverlayOpacity {get;set;}=.82;
 public double OverlayScale {get;set;}=1;
 public string HudStyle {get;set;}="combat";
 public string MainDesign {get;set;}="classic";
 public bool UnlockEffects {get;set;}=true;
 public bool UnlockSound {get;set;}
 public bool SessionConfigured {get;set;}
 public List<string> SessionWeapons {get;set;}=[];
}

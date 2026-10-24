using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace CamoTracker;
public partial class MainWindow
{
 bool hudEditing;
 ContextMenu? hudWeaponsMenu;
 bool HudHasTarget=>!tracker.Data.SessionConfigured||tracker.Data.SessionWeapons.Count>0;
 bool CanConfirm(Weapon w,Challenge c)=>!c.Done&&(c.GameRewardOnly||Eligibility.Reason(tracker.Data,w,c)==null);
 void ShowHudWeapons()
 {
  var menu=new ContextMenu{MaxHeight=440};hudWeaponsMenu=menu;
  var planned=tracker.Data.SessionWeapons.Select(id=>tracker.Data.Weapons.FirstOrDefault(w=>w.Id==id)).OfType<Weapon>();
  var guns=planned.Concat(tracker.Data.Weapons.Where(w=>w.Pinned)).DistinctBy(w=>w.Id);
  foreach(var w in guns)
  {
   var gun=w;var item=new MenuItem{Header=Bilingual.WeaponPair(gun),Tag=gun.Id,IsCheckable=true,IsChecked=tracker.Data.SelectedWeapon==gun.Id};
   item.Click+=(_,_)=>{var c=Recommendations.ForWeapon(tracker.Data,gun)??gun.Challenges.Last();hudEditing=false;tracker.Data.SelectedWeapon=gun.Id;tracker.Data.SelectedChallenge=c.Id;gun.OverlayChallengeId=c.Id;tracker.Save();Render();};menu.Items.Add(item);
  }
  menu.IsOpen=true;
 }
 IEnumerable<string> HudPrerequisites(Weapon w,Challenge c)
 {
  if(c.Done){yield return "已在记录中解锁";yield break;}
  if(c.GameRewardOnly){yield return "游戏前置原文待核对";yield return "以游戏内这款迷彩的实际解锁状态为准";yield break;}
  if(c.Kind=="special"&&!c.OpenConfirmed&&c.LevelGateUnknown)yield return "先在游戏中确认此挑战已开放";
  foreach(var gate in c.Gates)
  {
   bool Match(Challenge x)=>gate.ChallengeKey.Length>0?x.CatalogKey==gate.ChallengeKey:x.Kind==gate.Kind;
   var guns=tracker.Data.Weapons.Where(x=>x.Category!="模式精通"&&x.CatalogKey.Length>0&&(gate.Scope!="category"||x.Category==w.Category)&&x.Challenges.Any(Match)).ToList();
   int count=gate.Scope=="self"?w.Challenges.Count(Match):guns.Count(x=>x.Challenges.Any(Match));
   int done=gate.Scope=="self"?w.Challenges.Count(x=>Match(x)&&x.Done):guns.Count(x=>x.Challenges.Any(y=>Match(y)&&y.Done));
   int required=gate.Count>0?gate.Count:Math.Max(1,count);yield return (done>=required?"✓ ":"")+gate.Label+" "+done+" / "+required+(done<required?$" · 还差 {required-done} {(gate.Scope=="self"?"项":"把")}":"");
  }
  if(Eligibility.Reason(tracker.Data,w,c) is {} reason&&(!c.LevelGateUnknown||c.Kind!="special")&&!c.Gates.Any())yield return reason;
  if(c.RequirementUnverified)yield return "任务数值待核对，不能按参考数值判定完成";
 }
 void RenderFocusedHud()
 {
  if(mini==null)return;var w=tracker.CurrentWeapon;var c=tracker.Current;
  var panel=new StackPanel{Width=344,Margin=new(13)};
  var head=new Grid{Margin=new(0,0,0,12),Background=Brushes.Transparent};head.ColumnDefinitions.Add(new());head.ColumnDefinitions.Add(new(){Width=GridLength.Auto});Put(head,Brand(17));
  var menu=Command("···",ShowHudMenu);menu.Background=Brushes.Transparent;menu.Padding=new(6,0,6,0);menu.Margin=new(0);Put(head,menu,1);
  head.MouseLeftButtonDown+=(_,e)=>{for(var node=e.OriginalSource as DependencyObject;node!=null;node=VisualTreeHelper.GetParent(node)){if(node is Button)return;if(node==head)break;}if(!overlayLocked&&e.LeftButton==MouseButtonState.Pressed)mini.DragMove();};panel.Children.Add(head);
  if(!HudHasTarget)panel.Children.Add(Label("今日计划为空 · 在主界面选择武器",12,Muted));
  else
  {
   var title=new Grid();title.ColumnDefinitions.Add(new(){Width=new GridLength(76)});title.ColumnDefinitions.Add(new());title.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
   Put(title,GunThumbnail(w,66,42));Put(title,Stack(OneLine(Bilingual.WeaponZh(w),18,White,true),OneLine(Bilingual.WeaponEn(w),10,Muted)),1);var change=Command("切枪 ▾",ShowHudWeapons);change.Tag="hud-switch";change.Padding=new(9,5,9,5);change.Margin=new(0);change.IsEnabled=!overlayLocked;Put(title,change,2);panel.Children.Add(title);
   var image=OfficialArt.Image(OfficialArt.Key(c));var art=new Grid{Height=146,Margin=new(0,12,0,10),Background=B("#0B100F"),ClipToBounds=true};
   if(image!=null)art.Children.Add(new Image{Source=image,Stretch=Stretch.Uniform});else{var absent=Label("暂无这款迷彩图片",12,Muted);absent.HorizontalAlignment=HorizontalAlignment.Center;absent.VerticalAlignment=VerticalAlignment.Center;art.Children.Add(absent);}
   var caption=Label(image==null?"待资料补齐":OfficialArt.Caption(OfficialArt.Key(c)),9,White);caption.VerticalAlignment=VerticalAlignment.Bottom;caption.Background=B(Ink);caption.Padding=new(5,3,5,3);art.Children.Add(caption);panel.Children.Add(art);
   panel.Children.Add(Label(Bilingual.Camo(c),14,Acid,true));var goal=Label(c.GameRewardOnly?"军用迷彩 · 游戏进度奖励":c.Rule.Split('。')[0],11,White);goal.TextWrapping=TextWrapping.Wrap;goal.Margin=new(0,5,0,8);panel.Children.Add(goal);
   var conditions=new StackPanel{Margin=new(0,0,0,9)};conditions.Children.Add(Label("解锁前置",9,Muted));foreach(var line in HudPrerequisites(w,c)){var text=Label(line,10,"#C0CDBD");text.TextWrapping=TextWrapping.Wrap;text.Margin=new(0,3,0,0);conditions.Children.Add(text);}panel.Children.Add(conditions);
   if(!c.GameRewardOnly&&!c.IsLevelReward){var count=Label(c.ProgressKnown?$"{c.Count} / {c.Target} {c.Unit}":$"目标 {c.Target} {c.Unit} · 尚未补录",12,White,true);count.Margin=new(0,0,0,6);panel.Children.Add(count);panel.Children.Add(Progress(c,1));}
   if(!overlayLocked)
   {
    if(c.Kind=="special"&&!c.OpenConfirmed&&!c.Done)
    {
     var open=new CheckBox{Content="游戏中已开放此挑战",Foreground=B(Muted),FontSize=10,Margin=new(0,5,0,0)};open.Click+=(_,_)=>{tracker.Snapshot();c.OpenConfirmed=true;tracker.Save();Render();};panel.Children.Add(open);
    }
    if(hudEditing&&!c.GameRewardOnly&&!c.IsLevelReward)
    {
     var input=new TextBox{Width=86,Text=c.ProgressKnown?c.Count.ToString():"",MaxLength=7,Margin=new(0,0,7,0),ToolTip="游戏内当前总数"};var save=Command("保存总数",()=>
     {
      if(!int.TryParse(input.Text,out var total)||total<0||total>c.Target){input.ToolTip=$"填 0 到 {c.Target} 之间的游戏内总数";input.BorderBrush=B("#DF9A83");return;}
      bool wasDone=c.Done;try{tracker.RecordRunTotal(w,c,total);}catch(InvalidOperationException ex){input.ToolTip=ex.Message;return;}hudEditing=false;if(!wasDone&&c.Done)SetJourneyNext(w,c);Render();if(!wasDone&&c.Done)ShowUnlockToast(w,c);
     },true);save.IsEnabled=Eligibility.Reason(tracker.Data,w,c)==null;var cancel=Command("取消",()=>{hudEditing=false;RefreshMini();});var edit=Row(input,save,cancel);edit.Margin=new(0,10,0,0);panel.Children.Add(edit);
    }
    else
    {
     var done=Command(c.Done?"已拥有 ✓":c.GameRewardOnly?"游戏里已解锁":"已解锁 ✓",MarkCurrentUnlocked,true);done.Tag="hud-complete";done.IsEnabled=CanConfirm(w,c);done.ToolTip=c.GameRewardOnly?"仅确认游戏里已经获得的这款迷彩":Eligibility.Reason(tracker.Data,w,c);
     var actions=Row(done);if(!c.GameRewardOnly){var update=Command(c.IsLevelReward?"更新巅峰状态":"局后更新",()=>{if(c.IsLevelReward)RecordWeaponLevels(w,mini);else{hudEditing=true;RefreshMini();}});update.IsEnabled=c.IsLevelReward||!c.Done&&Eligibility.Reason(tracker.Data,w,c)==null;actions.Children.Add(update);}actions.Margin=new(0,10,0,0);panel.Children.Add(actions);
    }
    var details=Command("全部路线 ↗",()=>{var window=CreateJourneyWindow(w);window.Owner=mini;window.Topmost=true;window.ShowDialog();});details.Background=Brushes.Transparent;details.Foreground=B(Muted);details.Padding=new(0,8,0,0);panel.Children.Add(details);
   }
  }
  var hint=Label(overlayLocked?lockShortcut+" 解锁":"Ctrl+Alt+Enter 确认   ·   "+lockShortcut+" 锁定",8,Muted);hint.Margin=new(0,10,0,0);panel.Children.Add(hint);
  var color=(Color)ColorConverter.ConvertFromString(Ink);color.A=(byte)(Math.Clamp(tracker.Data.OverlayOpacity,.2,.95)*255);
  mini.Content=new Border{Child=panel,Background=new SolidColorBrush(color),BorderBrush=B(Line),BorderThickness=new(1),CornerRadius=DesignRadius,LayoutTransform=new ScaleTransform(Math.Clamp(tracker.Data.OverlayScale,.65,1.4),Math.Clamp(tracker.Data.OverlayScale,.65,1.4))};
 }
}

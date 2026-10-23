using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
namespace CamoTracker;
public partial class MainWindow
{
 string expandedWeaponId="",journeyChallengeId="";
 Window? journeyWindow;
 Action? refreshJourney;
 void RefreshJourney()=>refreshJourney?.Invoke();
 static string NodeName(Challenge c)=>c.Kind=="military"?"军用 "+c.CatalogKey.Split('-').Last():Bilingual.CamoZh(c);
 static IEnumerable<Challenge> Lane(Weapon w,bool prestige=false)=>w.Challenges.Where(c=>(c.Kind=="prestige")==prestige);
 UIElement GunThumbnail(Weapon w,double width,double height)
 {
  var grid=new Grid{Width=width,Height=height,ClipToBounds=true};grid.Children.Add(new Image{Source=WeaponArt.Image(w),Stretch=Stretch.Uniform});
  var latest=w.Challenges.LastOrDefault(c=>c.Done);if(latest!=null)
  {
   var art=OfficialArt.Image(OfficialArt.Key(latest));var badge=new Border{Width=26,Height=19,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom,BorderBrush=B(ColorFor(latest)),BorderThickness=new(1),CornerRadius=new(2),Background=B(Surface),ToolTip="已解锁："+Bilingual.Camo(latest)+"\n"+OfficialArt.Caption(OfficialArt.Key(latest))};
   badge.Child=art!=null?new Image{Source=art,Stretch=Stretch.UniformToFill}:Label("✓",10,Acid,true);grid.Children.Add(badge);
  }
  var total=Lane(w).Count();var line=new Grid{Height=2,VerticalAlignment=VerticalAlignment.Bottom,Background=B(Line)};line.Children.Add(new Border{Width=total==0?0:width*Lane(w).Count(c=>c.Done)/total,HorizontalAlignment=HorizontalAlignment.Left,Background=B(Acid)});grid.Children.Add(line);return grid;
 }
 UIElement JourneyRail(Weapon w,Action<Challenge> select,bool prestige=false,bool small=false)
 {
  var goals=Lane(w,prestige).ToList();double step=small?80:96;var track=new Grid{Width=Math.Max(1,goals.Count)*step,Height=small?105:119,Margin=new(3,0,3,0)};
  var line=new Border{Height=2,Background=B(Line),VerticalAlignment=VerticalAlignment.Top,Margin=new(step/2,small?62:72,step/2,0)};track.Children.Add(line);
  double done=goals.Count==0?0:(double)goals.Count(c=>c.Done)/goals.Count;var fill=new Border{Height=2,Width=Math.Max(0,(track.Width-step)*done),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new(step/2,small?62:72,0,0),Background=B(Acid)};track.Children.Add(fill);
  if(SystemParameters.ClientAreaAnimation&&done>0){var animation=new DoubleAnimation(0,fill.Width,TimeSpan.FromMilliseconds(180)){FillBehavior=FillBehavior.Stop};fill.BeginAnimation(WidthProperty,animation);}
  for(int i=0;i<goals.Count;i++)
  {
   var c=goals[i];bool active=c.Id==journeyChallengeId||journeyChallengeId.Length==0&&c.Id==tracker.Data.SelectedChallenge;var reason=Eligibility.Reason(tracker.Data,w,c);
   var body=new StackPanel();var image=OfficialArt.Image(OfficialArt.Key(c));var tile=new Border{Width=small?62:74,Height=small?35:44,Background=B(Surface),BorderBrush=B(active?Acid:Line),BorderThickness=new(active?2:1),CornerRadius=new(4),Child=image!=null?new Image{Source=image,Stretch=Stretch.UniformToFill}:Label("CAMO",9,Muted)};body.Children.Add(tile);
   var name=OneLine(NodeName(c),small?9:10,c.Done?Acid:White,c.Done);name.HorizontalAlignment=HorizontalAlignment.Center;name.MaxWidth=step-8;name.Margin=new(0,3,0,0);body.Children.Add(name);
   var dot=new Border{Width=14,Height=14,CornerRadius=new(7),Background=B(c.Done?Acid:Ink),BorderBrush=B(active?Acid:reason!=null?"#6D6580":Muted),BorderThickness=new(2),Margin=new(0,4,0,3),Child=c.Done?new TextBlock{Text="✓",FontSize=8,Foreground=B(Ink),HorizontalAlignment=HorizontalAlignment.Center}:null};body.Children.Add(dot);
   var state=OneLine(c.Kind=="military"?$"武器 Lv.{c.RequiredWeaponLevel}":c.Done?"已拥有":reason!=null?"前置未满足":"可刷取",8,c.Done?Acid:Muted);state.HorizontalAlignment=HorizontalAlignment.Center;body.Children.Add(state);
   var button=Command("",()=>select(c));button.Tag="node-"+c.Id;button.Content=body;button.Width=step;button.Padding=new(0);button.Margin=new(0);button.Background=Brushes.Transparent;button.HorizontalAlignment=HorizontalAlignment.Left;button.VerticalAlignment=VerticalAlignment.Top;button.SetValue(MarginProperty,new Thickness(i*step,0,0,0));button.ToolTip=Bilingual.Camo(c)+"\n"+Bilingual.Rule(c)+"\n"+(reason??"前置已满足");track.Children.Add(button);
  }
  var scroll=new ScrollViewer{Content=track,HorizontalContentAlignment=HorizontalAlignment.Left,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,CanContentScroll=false};
  int focus=goals.FindIndex(c=>c.Id==journeyChallengeId);if(focus<0)focus=goals.FindIndex(c=>c.Id==tracker.Data.SelectedChallenge);if(focus>=0)scroll.Loaded+=(_,_)=>scroll.ScrollToHorizontalOffset(Math.Max(0,focus*step-scroll.ViewportWidth/2+step/2));return scroll;
 }
 UIElement JourneyBody(Weapon w,bool small,Action repaint,Window owner)
 {
  var panel=new StackPanel();var selected=w.Challenges.FirstOrDefault(c=>c.Id==journeyChallengeId)??Recommendations.ForWeapon(tracker.Data,w)??w.Challenges[0];journeyChallengeId=selected.Id;
  void View(Challenge c){journeyChallengeId=c.Id;repaint();}
  panel.Children.Add(Label("武器迷彩路线",small?12:15,White,true));panel.Children.Add(JourneyRail(w,View,false,small));
  if(w.Challenges.Any(c=>c.Kind=="prestige")){var branch=Label("巅峰分支 · 与黄金 / 白金分开",9,"#B9A0EB");branch.Margin=new(0,9,0,4);panel.Children.Add(branch);panel.Children.Add(JourneyRail(w,View,true,small));}
  var c=selected;var reason=Eligibility.Reason(tracker.Data,w,c);var detail=new Grid{Margin=new(0,13,0,10)};detail.ColumnDefinitions.Add(new(){Width=new GridLength(small?105:170)});detail.ColumnDefinitions.Add(new(){Width=new GridLength(14)});detail.ColumnDefinitions.Add(new());
  var art=OfficialArt.Image(OfficialArt.Key(c));var photo=new StackPanel();photo.Children.Add(new Border{Height=small?85:126,Background=B("#0B100F"),Child=art!=null?new Image{Source=art,Stretch=Stretch.UniformToFill}:Label("暂无确切配图",10,Muted)});var caption=Label(art==null?"等待资料更新":OfficialArt.Caption(OfficialArt.Key(c)),8,Muted);caption.TextWrapping=TextWrapping.Wrap;caption.Margin=new(0,5,0,0);photo.Children.Add(caption);Put(detail,photo);
  var text=Stack(Label(Bilingual.Camo(c),small?13:18,ColorFor(c),true));var rule=Label(c.Rule,small?10:12,White);rule.TextWrapping=TextWrapping.Wrap;rule.Margin=new(0,6,0,4);text.Children.Add(rule);
  var english=Label(Bilingual.RuleEn(c),9,Muted);english.TextWrapping=TextWrapping.Wrap;text.Children.Add(english);var state=Label(c.Done?"已拥有 ✓":reason??"前置已满足，可以刷取",10,c.Done?Acid:reason==null?Acid:"#C5ABDD");state.TextWrapping=TextWrapping.Wrap;state.Margin=new(0,6,0,0);text.Children.Add(state);Put(detail,text,2);panel.Children.Add(detail);
  if(c.Kind=="special"&&c.LevelGateUnknown&&!c.Done)
  {
   var open=new CheckBox{Content="已在游戏内确认此挑战开放",IsChecked=c.OpenConfirmed,Foreground=B(Muted),FontSize=10,Margin=new(0,0,0,10)};open.Click+=(_,_)=>{tracker.Snapshot();c.OpenConfirmed=open.IsChecked==true;tracker.Save();Render();repaint();};panel.Children.Add(open);
  }
  if(c.IsLevelReward)
  {
   var levels=Command("记录武器等级 / 巅峰",()=>{RecordWeaponLevels(w,owner);repaint();},true);panel.Children.Add(levels);
  }
  else
  {
   var input=new TextBox{Text=c.ProgressKnown?c.Count.ToString():"",Width=68,Margin=new(0,0,6,0),VerticalContentAlignment=VerticalAlignment.Center,ToolTip="填写游戏内当前总进度，不是本局新增击杀",MaxLength=7};
   var total=Command("保存总数",()=>
   {
    if(!int.TryParse(input.Text,out var value)||value<0||value>c.Target){MessageBox.Show(owner,$"填写 0 到 {c.Target} 之间的游戏内总进度。","局后更新");return;}
    bool wasDone=c.Done;try{tracker.RecordRunTotal(w,c,value);}catch(InvalidOperationException ex){MessageBox.Show(owner,ex.Message);return;}if(!wasDone&&c.Done)SetJourneyNext(w,c);Render();repaint();if(!wasDone&&c.Done)ShowUnlockToast(w,c);
   });total.IsEnabled=reason==null&&!c.Done;
   var unlock=Command(c.Done?"已拥有 ✓":"本项已解锁",()=>
   {
    try{tracker.CompleteRun(w,c);}catch(InvalidOperationException ex){MessageBox.Show(owner,ex.Message);return;}SetJourneyNext(w,c);Render();repaint();ShowUnlockToast(w,c);
   },true);unlock.IsEnabled=!c.Done&&reason==null;unlock.ToolTip=reason;panel.Children.Add(Row(input,total,unlock));var unit=Label($"当前 { (c.ProgressKnown?c.Count.ToString():"未知") } / {c.Target} {c.Unit} · 填游戏内总数",9,Muted);unit.Margin=new(0,5,0,8);panel.Children.Add(unit);
  }
  var follow=Command("本局冲这个",()=>{try{tracker.SetSession(w,true);}catch(InvalidOperationException ex){MessageBox.Show(owner,ex.Message);return;}tracker.Data.SelectedWeapon=w.Id;tracker.Data.SelectedChallenge=c.Id;w.FocusChallengeId=c.Id;w.OverlayChallengeId=c.Id;tracker.Save();Render();repaint();});follow.IsEnabled=!c.Done&&reason==null;follow.ToolTip=reason;
  var calibrate=Command("校正到此节点",()=>
  {
   string lane=c.Kind=="prestige"?"巅峰分支":"普通迷彩路线";
   if(MessageBox.Show(owner,$"校正「{Bilingual.WeaponZh(w)}」的{lane}到「{Bilingual.CamoZh(c)}」？\n\n本路线到这里的节点均标为已拥有（含前面的特殊迷彩），之后的节点清空。其他武器和另一分支不变。仅用于按游戏实际情况恢复记录，可撤销。","校正已有进度",MessageBoxButton.OKCancel)!=MessageBoxResult.OK)return;
   tracker.RestoreTo(w,c);journeyChallengeId=c.Id;Render();repaint();
  });var undo=Command("撤销",()=>{tracker.Undo();Render();repaint();});undo.IsEnabled=tracker.CanUndo;panel.Children.Add(new Border{Height=10});panel.Children.Add(Row(follow,calibrate,undo));
  return panel;
 }
 void ShowWeaponJourney(Weapon w)=>CreateJourneyWindow(w).ShowDialog();
 Window CreateJourneyWindow(Weapon w)
 {
  journeyChallengeId=w.Challenges.FirstOrDefault(c=>c.Id==w.FocusChallengeId&&!c.Done)?.Id??Recommendations.ForWeapon(tracker.Data,w)?.Id??w.Challenges[0].Id;
  var window=new Window{Title=Bilingual.WeaponPair(w)+" · 迷彩路线",Width=950,Height=740,MinWidth=740,MinHeight=610,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=B(Ink),Foreground=B(White),FontFamily=FontFamily};window.Resources=Resources;window.SourceInitialized+=(_,_)=>DarkTitle(window);journeyWindow=window;
  var key=w.Id;void Paint(){var weapon=tracker.Data.Weapons.FirstOrDefault(x=>x.Id==key);if(weapon==null)return;var header=Stack(Label(Bilingual.WeaponZh(weapon),25,White,true),Label(Bilingual.WeaponEn(weapon),11,Muted),new Border{Height=18},JourneyBody(weapon,false,Paint,window));window.Content=new ScrollViewer{Content=header,Margin=new(24),VerticalScrollBarVisibility=ScrollBarVisibility.Auto};}
  refreshJourney=Paint;window.Closed+=(_,_)=>{journeyWindow=null;refreshJourney=null;};Paint();return window;
 }
 void OpenHudWeapon(int index)
 {
  SelectOverlayRow(index);expandedWeaponId=tracker.CurrentWeapon.Id;journeyChallengeId=tracker.Current.Id;RefreshMini();
 }
 void SetJourneyNext(Weapon w,Challenge previous)
 {
  var next=Recommendations.ForWeapon(tracker.Data,w)??previous;journeyChallengeId=next.Id;tracker.Data.SelectedWeapon=w.Id;tracker.Data.SelectedChallenge=next.Id;w.OverlayChallengeId=next.Id;tracker.Save();
 }
 UIElement HudJourney()
 {
  var w=tracker.Data.Weapons.FirstOrDefault(x=>x.Id==expandedWeaponId)??tracker.CurrentWeapon;
  var panel=new StackPanel();var back=Command("‹  今日计划",()=>{expandedWeaponId="";RefreshMini();});back.Background=Brushes.Transparent;back.HorizontalAlignment=HorizontalAlignment.Left;back.Padding=new(0,0,0,8);panel.Children.Add(back);
  panel.Children.Add(Row(GunThumbnail(w,90,48),new Border{Width=12},Stack(Label(Bilingual.WeaponZh(w),20,White,true),Label(Bilingual.WeaponEn(w),10,Muted))));panel.Children.Add(new Border{Height=14});panel.Children.Add(JourneyBody(w,true,RefreshMini,mini!));return panel;
 }
 async Task CaptureJourneyVariants(string path)
 {
  Button? FindTag(DependencyObject node,string tag){if(node is Button b&&Equals(b.Tag,tag))return b;for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++){var found=FindTag(VisualTreeHelper.GetChild(node,i),tag);if(found!=null)return found;}return null;}
  var axion=tracker.Data.Weapons.First(w=>w.Name=="Axion");tracker.MarkUnlocked(axion,axion.Challenges.First(c=>c.Kind=="gold"),true);var platinum=axion.Challenges.First(c=>c.Kind=="platinum");
  var window=CreateJourneyWindow(axion);window.ShowActivated=false;window.Show();window.UpdateLayout();var node=FindTag(window,"node-"+platinum.Id)??throw new Exception("Long journey node missing");node.RaiseEvent(new RoutedEventArgs(Button.ClickEvent,node));await Task.Delay(250);window.UpdateLayout();
  bool HasBlockedButton(DependencyObject obj){if(obj is Button b&&b.Content as string=="本项已解锁")return !b.IsEnabled;for(int i=0;i<VisualTreeHelper.GetChildrenCount(obj);i++)if(HasBlockedButton(VisualTreeHelper.GetChild(obj,i)))return true;return false;}
  if(!HasBlockedButton(window)||Eligibility.Reason(tracker.Data,axion,platinum)==null)throw new Exception("Platinum action must be blocked by class Gold gate");
  var image=new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(window);SavePng(image,System.IO.Path.ChangeExtension(path,"journey.png"));window.Close();
  tracker.SetSession(axion,true);if(mini is not {IsVisible:true})ToggleMini();mini!.UpdateLayout();await Task.Delay(100);var rows=OverlayRows();int index=rows.FindIndex(r=>r.weapon.Id==axion.Id);if(index<0)throw new Exception("Axion missing from plan");var row=FindTag(mini!,"weapon-"+axion.Id)??throw new Exception("HUD weapon row missing");row.RaiseEvent(new RoutedEventArgs(Button.ClickEvent,row));if(expandedWeaponId!=axion.Id)throw new Exception("HUD weapon interaction failed");journeyChallengeId=platinum.Id;RefreshMini();await Task.Delay(250);mini!.UpdateLayout();if(!HasBlockedButton(mini))throw new Exception("HUD bypassed prerequisites");
  var hud=new System.Windows.Media.Imaging.RenderTargetBitmap((int)mini.ActualWidth,(int)mini.ActualHeight,96,96,PixelFormats.Pbgra32);hud.Render(mini);SavePng(hud,System.IO.Path.ChangeExtension(path,"journey-hud.png"));expandedWeaponId="";RefreshMini();ToggleMini();
 }
}

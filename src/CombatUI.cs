using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace CamoTracker;
public partial class MainWindow
{
 const string Ink="#111615",Surface="#1A2220",Line="#303C36",White="#EDF1E9",Muted="#97A59B";
 static string Acid="#CEEF63";

 static TextBlock Label(string s,double size=12,string color=White,bool bold=false)=>T(s,size,color,bold);
 static UIElement Brand(double size)
 {
  var words=Stack(Label("迷彩猎手",size>=24?15:12,White,true),Label("CAMO TRACKER",size>=24?9:8,Muted));words.Margin=new(12,0,0,0);words.VerticalAlignment=VerticalAlignment.Center;
  return Row(Label("MW",size,White,true),Label("4",size,Acid,true),words);
 }
 Button Command(string text,Action action,bool primary=false)
 {
  var b=Btn(text,action);b.Background=B(primary?Acid:Surface);b.Foreground=B(primary?"#182011":White);b.FontSize=12;b.FontWeight=primary?FontWeights.SemiBold:FontWeights.Normal;b.Padding=new(14,10,14,10);b.Margin=new(0,0,7,0);return b;
 }
 static string ColorFor(Challenge c)=>c.RequiredPrestige>0?"#B9A0EB":OfficialArt.Key(c)=="gold"?"#E9C36D":c.IsPreparation?"#DDAD69":"#73C9BC";
 string StateFor(Weapon w,Challenge c)=>c.Done?"已解锁":Eligibility.Reason(tracker.Data,w,c)!=null?c.RequirementUnverified?"待核对":"待前置":c.IsLevelReward?"等级奖励":c.ProgressKnown?"差 "+(c.Target-c.Count):"未录进度";
 static string ShortName(Challenge c)=>c.Kind=="military"?$"军用迷彩 · 武器 Lv.{c.RequiredWeaponLevel}":Bilingual.Camo(c);
 void Render()
 {
  ApplyMainDesign();if(tracker.Data.MainDesign!="classic"){RenderDesigned();RefreshMini();return;}
  Shell.Children.Clear();Shell.RowDefinitions.Clear();Shell.ColumnDefinitions.Clear();Shell.Background=B(Ink);
  Shell.RowDefinitions.Add(new(){Height=new GridLength(74)});Shell.RowDefinitions.Add(new());Shell.RowDefinitions.Add(new(){Height=new GridLength(43)});
  var header=new Grid{Margin=new(24,0,17,0)};header.ColumnDefinitions.Add(new());header.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  var brand=Brand(26);brand.SetValue(VerticalAlignmentProperty,VerticalAlignment.Center);Put(header,brand);
  var nav=Row(Command("设计 ▾",ShowDesignMenu),Command("全局进度",ShowGlobalProgress),Command("已解锁清单",ShowInventory),Command("游戏小窗 ↗",ToggleMini,true),Command("···",ShowHudMenu));nav.VerticalAlignment=VerticalAlignment.Center;Put(header,nav,1);Put(Shell,header);
  var body=new Grid{Margin=new(24,0,24,0)};body.ColumnDefinitions.Add(new(){Width=new GridLength(315)});body.ColumnDefinitions.Add(new(){Width=new GridLength(26)});body.ColumnDefinitions.Add(new());
  var left=new DockPanel();
  var listHeading=new Grid{Margin=new(0,2,0,16)};listHeading.ColumnDefinitions.Add(new());listHeading.ColumnDefinitions.Add(new(){Width=GridLength.Auto});Put(listHeading,Label(tracker.Data.SessionConfigured?$"今天刷 {tracker.Data.SessionWeapons.Count} 把":"推荐武器",15,White,true));
  var route=Command(overlayTier=="自动推荐"?"自动 ▾":"目标 ▾",ShowTierMenu);route.FontSize=10;route.Padding=new(8,4,8,4);route.Margin=new(0);Put(listHeading,route,1);DockPanel.SetDock(listHeading,Dock.Top);left.Children.Add(listHeading);
  var list=new StackPanel();

  {
   var suggestions=Recommendations.Next(tracker.Data,skippedWeapons);var planned=tracker.Data.SessionConfigured?Recommendations.Next(tracker.Data,new HashSet<string>(),true):new();var rows=planned.Concat(suggestions.Where(r=>!planned.Any(p=>p.weapon.Id==r.weapon.Id))).Take(5).ToList();for(int i=0;i<rows.Count;i++){var item=rows[i];list.Children.Add(QueueRow(item.weapon,item.challenge,i,true,()=>SelectGoal(item.weapon,item.challenge)));}
   if(rows.Count==0)list.Children.Add(Label("暂无目标",13,Muted));
   var reset=Command("挑选其他武器",ShowInventory);reset.Margin=new(0,13,0,0);reset.HorizontalAlignment=HorizontalAlignment.Left;list.Children.Add(reset);
  }
  left.Children.Add(new ScrollViewer{Content=list,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});Put(body,left);
  var current=tracker.Current;var weapon=tracker.CurrentWeapon;var right=new StackPanel();
  var currentTitle=new Grid{Margin=new(0,0,0,5)};currentTitle.ColumnDefinitions.Add(new());currentTitle.ColumnDefinitions.Add(new(){Width=GridLength.Auto});Put(currentTitle,Label(Bilingual.WeaponZh(weapon),24,White,true));
  var tag=Label(current.RequiredPrestige>0?"PRESTIGE":OfficialArt.Key(current)=="gold"?"GOLD":"CAMO",10,ColorFor(current),true);tag.VerticalAlignment=VerticalAlignment.Center;Put(currentTitle,tag,1);right.Children.Add(currentTitle);
  var weaponChinese=Label(Bilingual.WeaponEn(weapon)+" · "+weapon.Category,11,Muted);weaponChinese.Margin=new(0,0,0,10);right.Children.Add(weaponChinese);
  right.Children.Add(Label(Bilingual.Camo(current),13,ColorFor(current)));
  var art=new Grid{Height=150,Margin=new(0,12,0,12)};art.ColumnDefinitions.Add(new());art.ColumnDefinitions.Add(new(){Width=new GridLength(10)});art.ColumnDefinitions.Add(new(){Width=new GridLength(144)});
  var gun=new Grid{Background=B("#0B100F"),ClipToBounds=true};var gunImage=WeaponArt.Image(weapon);gun.Children.Add(new Image{Source=gunImage,Stretch=Stretch.Uniform,Margin=new(3,5,3,20)});if(gunImage==null)gun.Children.Add(Label("暂无对应枪型",11,Muted));var gunLabel=Label("原厂枪型 / BASE WEAPON",9,Muted);gunLabel.VerticalAlignment=VerticalAlignment.Bottom;gunLabel.Margin=new(10,0,0,8);gun.Children.Add(gunLabel);Put(art,gun);
  var camoRef=new Grid{Background=B("#0B100F"),ClipToBounds=true};var picture=OfficialArt.Image(OfficialArt.Key(current));camoRef.Children.Add(new Image{Source=picture,Stretch=Stretch.UniformToFill});if(picture==null){var absent=Label("暂无迷彩配图",10,Muted);absent.VerticalAlignment=VerticalAlignment.Center;absent.HorizontalAlignment=HorizontalAlignment.Center;camoRef.Children.Add(absent);}camoRef.Children.Add(new Border{Background=new LinearGradientBrush(Color.FromArgb(0,0,0,0),Color.FromArgb(210,8,13,10),90)});var refLabel=Label(picture==null?"迷彩外观 / CAMO":OfficialArt.Caption(OfficialArt.Key(current)),9,"#D1D7CE");refLabel.VerticalAlignment=VerticalAlignment.Bottom;refLabel.Margin=new(8,0,0,8);camoRef.Children.Add(refLabel);
  var artButton=Command("",()=>{if(OfficialArt.Image(OfficialArt.Key(current))!=null)ShowArt(OfficialArt.Key(current));else ChooseArt();});artButton.Content=camoRef;artButton.Padding=new(0);artButton.Margin=new(0);artButton.HorizontalContentAlignment=HorizontalAlignment.Stretch;artButton.ToolTip="官方迷彩样式参考，示例枪身为 HAN 86；左侧为当前武器的实际原厂枪型。";Put(art,artButton,2);right.Children.Add(art);
  var gate=Eligibility.Reason(tracker.Data,weapon,current);
  var description=current.IsPreparation?"完成全部军事与特殊迷彩":current.Rule.Split('。')[0].Replace("多人模式：","");
  var rule=Label(description,12,Muted);rule.Margin=new(0,0,0,4);rule.ToolTip=Bilingual.Rule(current);right.Children.Add(rule);
  var englishRule=Bilingual.RuleEn(current);if(englishRule.Length>0){var english=Label(englishRule,10,"#84988F");english.Margin=new(0,0,0,11);right.Children.Add(english);}
  if(gate!=null){var prerequisite=Label("前置："+gate,10,"#BCA4DC");prerequisite.Margin=new(0,0,0,9);right.Children.Add(prerequisite);}
  var count=new Grid{Margin=new(0,0,0,13)};count.ColumnDefinitions.Add(new());count.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  var digits=Label(current.Done?"已解锁":current.ProgressKnown?current.Count.ToString("D2"):"未录进度",current.ProgressKnown&&!current.Done?43:28,White,true);digits.FontFamily=new("Bahnschrift, Microsoft YaHei UI");Put(count,digits);
  var target=Label(current.IsLevelReward?"等级奖励":"/ "+current.Target,15,Muted);target.VerticalAlignment=VerticalAlignment.Bottom;target.Margin=new(0,0,0,5);Put(count,target,1);right.Children.Add(count);
  right.Children.Add(Progress(current,1));
  var controls=Row(Command(current.Done?"已解锁 ✓":"标记已解锁",MarkCurrentUnlocked,true),Command("补录进度",()=>RecordProgress()),Command("↶",Undo));controls.Margin=new(0,15,0,0);
  ((Button)controls.Children[0]).MinWidth=145;((Button)controls.Children[0]).IsEnabled=!current.Done&&gate==null;((Button)controls.Children[0]).ToolTip=gate;((Button)controls.Children[1]).IsEnabled=current.IsLevelReward||gate==null;((Button)controls.Children[2]).IsEnabled=tracker.CanUndo;
  right.Children.Add(controls);
  var today=new CheckBox{Content="今天刷这把",IsChecked=tracker.InSession(weapon),Foreground=B(Acid),Margin=new(0,14,0,0)};today.Click+=(_,_)=>SetToday(weapon,today.IsChecked==true);right.Children.Add(Row(today,Command("武器路线",()=>ShowWeaponJourney(weapon))));
  Put(body,new ScrollViewer{Content=right,VerticalScrollBarVisibility=ScrollBarVisibility.Auto},2);Put(Shell,body,0,1);
  var footer=new Grid{Margin=new(24,0,17,0)};footer.ColumnDefinitions.Add(new());footer.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  var saved=Label(tracker.Notice.Contains("失败")?"● 保存失败":"● 已保存",10,tracker.Notice.Contains("失败")?"#E9A882":Muted);saved.ToolTip=tracker.Notice+"\n"+hotkeyStatus;Put(footer,saved);
  var support=Command(Branding.Author+" · 赞赏作者",ShowSupport);support.Background=Brushes.Transparent;support.Foreground=B(Muted);support.Padding=new(0);Put(footer,support,1);Put(Shell,footer,0,2);RefreshMini();
 }
 UIElement Progress(Challenge c,double opacity)
 {
  var g=new Grid{Height=3,Background=B("#2E3A33"),Opacity=opacity};var fill=new Border{Background=B(Acid),HorizontalAlignment=HorizontalAlignment.Left};g.Children.Add(fill);g.SizeChanged+=(_,_)=>fill.Width=g.ActualWidth*Math.Clamp(1.0*c.Count/c.Target,0,1);return g;
 }
 Button QueueRow(Weapon w,Challenge c,int index,bool main,Action select)
 {
  bool active=w.Id==tracker.Data.SelectedWeapon&&c.Id==tracker.Data.SelectedChallenge;
  var grid=new Grid();grid.ColumnDefinitions.Add(new(){Width=new GridLength(25)});grid.ColumnDefinitions.Add(new(){Width=new GridLength(main?78:88)});grid.ColumnDefinitions.Add(new());grid.ColumnDefinitions.Add(new(){Width=new GridLength(54)});
  if(main){var selected=new CheckBox{IsChecked=tracker.InSession(w),VerticalAlignment=VerticalAlignment.Center,ToolTip="加入今天的计划"};selected.Click+=(_,_)=>SetToday(w,selected.IsChecked==true);Put(grid,selected);}else{var n=Label((index+1).ToString("D2"),11,active?Acid:Muted,true);n.FontFamily=new("Bahnschrift");n.VerticalAlignment=VerticalAlignment.Center;Put(grid,n);}
  Put(grid,GunThumbnail(w,main?70:80,46),1);
  var name=Label(Bilingual.WeaponZh(w),13,White,true);name.TextWrapping=TextWrapping.NoWrap;name.TextTrimming=TextTrimming.CharacterEllipsis;
  var sub=Label(ShortName(c),10,ColorFor(c));sub.TextWrapping=TextWrapping.NoWrap;sub.TextTrimming=TextTrimming.CharacterEllipsis;
  var content=new StackPanel();content.Children.Add(name);if(Bilingual.WeaponZh(w)!=Bilingual.WeaponEn(w)){var englishName=Label(Bilingual.WeaponEn(w),10,Muted);englishName.TextWrapping=TextWrapping.NoWrap;englishName.TextTrimming=TextTrimming.CharacterEllipsis;englishName.Margin=new(0,1,0,0);content.Children.Add(englishName);}content.Children.Add(new Border{Height=4});content.Children.Add(sub);content.Children.Add(new Border{Height=7});content.Children.Add(Progress(c,.8));content.Margin=new(0,0,8,0);Put(grid,content,2);
  var remaining=Label(StateFor(w,c),11,c.IsPreparation?"#C7AD80":Eligibility.Reason(tracker.Data,w,c)!=null?"#BCA4DC":active?Acid:White,true);remaining.VerticalAlignment=VerticalAlignment.Center;remaining.HorizontalAlignment=HorizontalAlignment.Right;Put(grid,remaining,3);
  var button=Command("",select);button.Tag="weapon-"+w.Id;button.Content=grid;button.HorizontalContentAlignment=HorizontalAlignment.Stretch;button.Padding=new(12,main?9:12,10,main?9:12);button.Margin=new(0,0,0,5);button.Background=active?new LinearGradientBrush(DesignTint(.18),(Color)ColorConverter.ConvertFromString("#1B2621"),0):B("#171F1C");button.ToolTip=Bilingual.WeaponZh(w)+"\n"+Bilingual.Camo(c)+"\n"+Bilingual.Rule(c);
  return button;
 }
 void RefreshMini()
 {
  if(mini==null)return;var all=OverlayRows();var rows=all.Skip(overlayPage*5).Take(5).ToList();
  if(mini.IsVisible&&expandedWeaponId.Length==0&&overlayTier=="自动推荐"&&rows.Count>0&&!rows.Any(r=>r.weapon.Id==tracker.Data.SelectedWeapon&&r.challenge.Id==tracker.Data.SelectedChallenge)){tracker.Data.SelectedWeapon=rows[0].weapon.Id;tracker.Data.SelectedChallenge=rows[0].challenge.Id;tracker.Save();}
  var panel=new StackPanel{Width=expandedWeaponId.Length>0?450:376,Margin=new(12)};
  var top=new Grid{Margin=new(2,1,0,12),Background=Brushes.Transparent,Cursor=Cursors.SizeAll};top.ColumnDefinitions.Add(new());top.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  var title=Brand(18);Put(top,title);var menu=Command("···",ShowHudMenu);menu.Padding=new(6,0,6,0);menu.Background=Brushes.Transparent;menu.Margin=new(0);Put(top,menu,1);top.MouseLeftButtonDown+=(_,e)=>{if(!overlayLocked&&e.LeftButton==MouseButtonState.Pressed){mini.DragMove();e.Handled=true;}};panel.Children.Add(top);
  if(expandedWeaponId.Length>0&&!overlayLocked){panel.Children.Add(HudJourney());}
  else
  {
  var mode=Label(overlayTier=="自动推荐"?tracker.Data.SessionConfigured?$"今日计划 / {all.Count:D2}":"推荐目标 / 05":"目标 / "+overlayTier.ToUpperInvariant(),10,Acid,true);mode.Margin=new(2,0,0,10);panel.Children.Add(mode);
  for(int i=0;i<rows.Count;i++){int index=i;panel.Children.Add(QueueRow(rows[i].weapon,rows[i].challenge,i,false,()=>OpenHudWeapon(index)));}
  if(rows.Count==0)panel.Children.Add(Label("暂无目标",13,Muted));
  if(!overlayLocked)
  {
   var controls=Row(Command("已解锁 ✓",MarkCurrentUnlocked,true),Command("局后更新",()=>FromOverlay(RecordProgress)),Command("↶",Undo));controls.Margin=new(0,8,0,0);((Button)controls.Children[0]).MinWidth=115;((Button)controls.Children[0]).IsEnabled=rows.Count>0&&!tracker.Current.Done&&Eligibility.Reason(tracker.Data,tracker.CurrentWeapon,tracker.Current)==null;((Button)controls.Children[0]).ToolTip=Eligibility.Reason(tracker.Data,tracker.CurrentWeapon,tracker.Current);((Button)controls.Children[1]).IsEnabled=rows.Count>0&&(tracker.Current.IsLevelReward||Eligibility.Reason(tracker.Data,tracker.CurrentWeapon,tracker.Current)==null);((Button)controls.Children[2]).IsEnabled=tracker.CanUndo;panel.Children.Add(controls);
   if(all.Count>5){var paging=Row(Command("‹",()=>{overlayPage--;SelectOverlayRow(0);RefreshMini();}),Label($"{overlayPage+1} / {(all.Count+4)/5}",11),Command("›",()=>{overlayPage++;SelectOverlayRow(0);RefreshMini();}));paging.Margin=new(0,8,0,0);panel.Children.Add(paging);}
  }
  }
  var hint=Label(overlayLocked?lockShortcut+" 解锁":"Ctrl+Alt+Enter 已解锁   ·   "+lockShortcut+" 锁定",9,Muted);hint.Margin=new(2,11,0,1);panel.Children.Add(hint);
  var color=(Color)ColorConverter.ConvertFromString(Ink);color.A=(byte)(Math.Clamp(tracker.Data.OverlayOpacity,.2,.95)*255);
  mini.Content=new Border{Child=new ScrollViewer{Content=panel,MaxHeight=Math.Max(360,(SystemParameters.WorkArea.Height-110)/Math.Clamp(tracker.Data.OverlayScale,.65,1.4)),VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled},Background=new SolidColorBrush(color),BorderBrush=B(Line),BorderThickness=new(1),CornerRadius=DesignRadius,LayoutTransform=new ScaleTransform(Math.Clamp(tracker.Data.OverlayScale,.65,1.4),Math.Clamp(tracker.Data.OverlayScale,.65,1.4))};
 }
 void SkipWeapon(){skippedWeapons.Add(tracker.CurrentWeapon.Id);SelectOverlayRow(0);Render();}
 void ShowWeaponMenu(){var menu=new ContextMenu();foreach(var w in tracker.Data.Weapons){var item=new MenuItem{Header=Bilingual.WeaponPair(w)};item.Click+=(_,_)=>{tracker.Data.SelectedWeapon=w.Id;tracker.Normalize();tracker.Save();Render();};menu.Items.Add(item);}menu.IsOpen=true;}
 void ShowStyleMenu()=>ShowHudMenu();
 void ShowHudMenu()
 {
  var menu=new ContextMenu();void Item(string s,Action action){var i=new MenuItem{Header=s};i.Click+=(_,_)=>action();menu.Items.Add(i);}
  Item(mini is {IsVisible:true}?"返回主界面":"打开游戏小窗",ToggleMini);Item("资料更新",ShowDataUpdates);Item("主界面设计",ShowDesignMenu);Item("全局进度",ShowGlobalProgress);Item("迷彩路线",ShowTierMenu);Item("武器等级",()=>FromOverlay(RecordLevels));Item("添加武器",()=>FromOverlay(AddWeapon));Item("添加挑战",()=>FromOverlay(()=>EditChallenge(true)));Item("编辑当前挑战",()=>FromOverlay(()=>EditChallenge(false)));Item("迷彩配图",()=>FromOverlay(ChooseArt));Item("重置本项",()=>FromOverlay(ResetCurrent));
  menu.Items.Add(new Separator());Item("恢复自动推荐",()=>{tracker.Snapshot();skippedWeapons.Clear();tracker.Data.SessionWeapons.Clear();tracker.Data.SessionConfigured=false;foreach(var w in tracker.Data.Weapons)w.FocusChallengeId="";overlayTier="自动推荐";tracker.Save();SelectOverlayRow(0);Render();});Item("导入存档",()=>FromOverlay(Import));Item("导出备份",()=>FromOverlay(Export));
  var pin=new MenuItem{Header="主窗口置顶",IsCheckable=true,IsChecked=Topmost};pin.Click+=(_,_)=>{Topmost=pin.IsChecked;tracker.Data.Topmost=Topmost;tracker.Save();};menu.Items.Add(pin);
  Item(overlayLocked?"解锁小窗":"锁定小窗 / 鼠标穿透",ToggleOverlayLock);
  var display=new MenuItem{Header="小窗透明度与大小"};var settings=new StackPanel{Width=200,Margin=new(12)};settings.Children.Add(Label("背景",12));var alpha=new Slider{Minimum=.2,Maximum=.95,Value=Math.Clamp(tracker.Data.OverlayOpacity,.2,.95),Margin=new(0,7,0,15)};alpha.ValueChanged+=(_,_)=>{tracker.Data.OverlayOpacity=alpha.Value;RefreshMini();};alpha.PreviewMouseLeftButtonUp+=(_,_)=>tracker.Save();settings.Children.Add(alpha);settings.Children.Add(Label("大小",12));var scale=new Slider{Minimum=.65,Maximum=1.4,Value=Math.Clamp(tracker.Data.OverlayScale,.65,1.4),Margin=new(0,7,0,0)};scale.ValueChanged+=(_,_)=>{tracker.Data.OverlayScale=scale.Value;RefreshMini();};scale.PreviewMouseLeftButtonUp+=(_,_)=>tracker.Save();settings.Children.Add(scale);display.Items.Add(new MenuItem{Header=settings,StaysOpenOnClick=true});menu.Items.Add(display);
  menu.Items.Add(new Separator());var fx=new MenuItem{Header="解锁动画",IsCheckable=true,IsChecked=tracker.Data.UnlockEffects};fx.Click+=(_,_)=>{tracker.Data.UnlockEffects=fx.IsChecked;if(!fx.IsChecked)DismissUnlock();tracker.Save();};menu.Items.Add(fx);var sound=new MenuItem{Header="解锁提示音",IsCheckable=true,IsChecked=tracker.Data.UnlockSound};sound.Click+=(_,_)=>{tracker.Data.UnlockSound=sound.IsChecked;tracker.Save();};menu.Items.Add(sound);Item("预览解锁特效",()=>ShowUnlockToast(tracker.CurrentWeapon,tracker.Current,true));
  Item("试听解锁音效",UnlockAudio.Play);menu.Items.Add(new Separator());Item("赞赏作者",ShowSupport);Item("使用说明",()=>FromOverlay(Help));menu.IsOpen=true;
 }
 void FromOverlay(Action action){bool compact=mini is {IsVisible:true};if(compact){mini!.Hide();Show();}action();if(compact)ToggleMini();}
 Window CreateSupportWindow()
 {
  var dialog=new Window{Title="赞赏作者",Width=470,Height=580,Owner=this,Background=B(Ink),Foreground=B(White),FontFamily=FontFamily,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize};dialog.Resources=Resources;
  var panel=new StackPanel{Margin=new(24)};panel.Children.Add(Label("Burrito · 迷彩猎手",23,White,true));panel.Children.Add(Label("如果这个工具帮到了你，欢迎赞赏支持后续维护。",11,Muted));panel.Children.Add(new Border{Height=12});panel.Children.Add(new Image{Source=new BitmapImage(new Uri("pack://application:,,,/Assets/donation.png")),Height=390,Stretch=Stretch.Uniform});var note=Label("作者 Burrito · 赞赏自愿，全部功能免费。",12,Muted);note.Margin=new(0,16,0,0);panel.Children.Add(note);dialog.Content=panel;dialog.SourceInitialized+=(_,_)=>DarkTitle(dialog);return dialog;
 }
 void ShowSupport()=>FromOverlay(()=>CreateSupportWindow().ShowDialog());
 void CaptureHudVariants(string path)
 {
  if(mini==null)return;var mainImage=LoadPng(path);mini.UpdateLayout();var miniImage=new RenderTargetBitmap((int)mini.ActualWidth,(int)mini.ActualHeight,96,96,PixelFormats.Pbgra32);miniImage.Render(mini);
  var visual=new DrawingVisual();using(var draw=visual.RenderOpen()){draw.DrawRectangle(B("#080D0B"),null,new Rect(0,0,1440,760));draw.DrawText(new FormattedText("主界面",CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface(FontFamily,FontStyles.Normal,FontWeights.Normal,FontStretches.Normal),16,B(Muted),1),new Point(24,18));draw.DrawImage(mainImage,new Rect(24,55,mainImage.PixelWidth,mainImage.PixelHeight));draw.DrawText(new FormattedText("游戏小窗",CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface(FontFamily,FontStyles.Normal,FontWeights.Normal,FontStretches.Normal),16,B(Muted),1),new Point(1000,18));draw.DrawImage(miniImage,new Rect(1000,55,miniImage.PixelWidth,miniImage.PixelHeight));}
  var board=new RenderTargetBitmap(1440,760,96,96,PixelFormats.Pbgra32);board.Render(visual);SavePng(board,System.IO.Path.ChangeExtension(path,"styles.png"));
  var support=CreateSupportWindow();support.ShowActivated=false;support.Show();support.UpdateLayout();var supportImage=new RenderTargetBitmap((int)support.ActualWidth,(int)support.ActualHeight,96,96,PixelFormats.Pbgra32);supportImage.Render(support);SavePng(supportImage,System.IO.Path.ChangeExtension(path,"support.png"));support.Close();
  var inventory=CreateInventoryWindow();inventory.ShowActivated=false;inventory.Show();inventory.UpdateLayout();var inventoryImage=new RenderTargetBitmap((int)inventory.ActualWidth,(int)inventory.ActualHeight,96,96,PixelFormats.Pbgra32);inventoryImage.Render(inventory);SavePng(inventoryImage,System.IO.Path.ChangeExtension(path,"inventory.png"));inventory.Close();
  var progress=CreateProgressWindow();progress.ShowActivated=false;progress.Show();progress.UpdateLayout();var progressImage=new RenderTargetBitmap((int)progress.ActualWidth,(int)progress.ActualHeight,96,96,PixelFormats.Pbgra32);progressImage.Render(progress);SavePng(progressImage,System.IO.Path.ChangeExtension(path,"progress.png"));progress.Close();
 }
 static BitmapSource LoadPng(string path){var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.UriSource=new Uri(path);image.EndInit();return image;}
 static void SavePng(BitmapSource bitmap,string path){var e=new PngBitmapEncoder();e.Frames.Add(BitmapFrame.Create(bitmap));using var s=File.Create(path);e.Save(s);}
}

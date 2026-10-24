using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace CamoTracker;
public partial class MainWindow
{
 static CornerRadius DesignRadius=new(7);
 static readonly (string key,string title,string accent)[] Designs=[("classic","原版 · 经典布局","#CEEF63"),("tactical","A · 战术简报","#D3ED83"),("armory","B · 军械展厅","#81CDE2"),("arena","C · 赛场看板","#F2AE72")];
 void ApplyMainDesign()
 {
  var design=Designs.FirstOrDefault(d=>d.key==tracker.Data.MainDesign);if(design.key==null){tracker.Data.MainDesign="classic";design=Designs[0];}
  Acid=design.accent;DesignRadius=new(tracker.Data.MainDesign=="tactical"?2:tracker.Data.MainDesign=="arena"?5:8);
  Resources["AccentBrush"]=B(Acid);Resources["ButtonRadius"]=DesignRadius;Resources[SystemColors.HighlightBrushKey]=B(Acid);
 }
 static Color DesignTint(double amount)
 {
  var a=(Color)ColorConverter.ConvertFromString(Acid);var b=(Color)ColorConverter.ConvertFromString(Ink);
  return Color.FromRgb((byte)(b.R+(a.R-b.R)*amount),(byte)(b.G+(a.G-b.G)*amount),(byte)(b.B+(a.B-b.B)*amount));
 }
 void ShowDesignMenu()
 {
  var menu=new ContextMenu();foreach(var design in Designs){var choice=design;var item=new MenuItem{Header=choice.title,IsCheckable=true,IsChecked=tracker.Data.MainDesign==choice.key};item.Click+=(_,_)=>{tracker.Data.MainDesign=choice.key;tracker.Save();Render();};menu.Items.Add(item);}menu.IsOpen=true;
 }
 List<(Weapon weapon,Challenge challenge)> DesignWeapons()
 {
  var suggestions=Recommendations.Next(tracker.Data,skippedWeapons);var chosen=tracker.Data.SessionConfigured?Recommendations.Next(tracker.Data,new HashSet<string>(),true):new();
  return chosen.Concat(suggestions.Where(r=>!chosen.Any(p=>p.weapon.Id==r.weapon.Id))).Take(5).ToList();
 }
 static TextBlock OneLine(string text,double size=12,string color=White,bool bold=false)
 {
  var label=Label(text,size,color,bold);label.TextWrapping=TextWrapping.NoWrap;label.TextTrimming=TextTrimming.CharacterEllipsis;return label;
 }
 static Border Frame(UIElement child,double padding=16,string color=Surface)=>new(){Child=child,Padding=new(padding),Background=B(color),BorderBrush=B(Line),BorderThickness=new(1),CornerRadius=DesignRadius};
 static TextBlock Figure(string text,double size=30,string color=White)
 {
  var label=OneLine(text,size,color,true);label.FontFamily=new("Bahnschrift, Microsoft YaHei UI");return label;
 }
 UIElement DesignHeader()
 {
  var header=new Grid{Margin=new(24,0,17,0)};header.ColumnDefinitions.Add(new());header.ColumnDefinitions.Add(new(){Width=GridLength.Auto});var brand=Brand(25);brand.SetValue(VerticalAlignmentProperty,VerticalAlignment.Center);Put(header,brand);
  var nav=Row(Command("设计 ▾",ShowDesignMenu),Command("全局进度",ShowGlobalProgress),Command("已解锁清单",ShowInventory),Command("游戏小窗 ↗",ToggleMini,true),Command("···",ShowHudMenu));nav.VerticalAlignment=VerticalAlignment.Center;Put(header,nav,1);return header;
 }
 UIElement DesignFooter()
 {
  var grid=new Grid{Margin=new(24,0,17,0)};grid.ColumnDefinitions.Add(new());grid.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
  var name=Designs.First(d=>d.key==tracker.Data.MainDesign).title;var state=Label("● "+(tracker.Notice.Contains("失败")?"保存失败":"已保存")+"     /     "+name,10,Muted);state.VerticalAlignment=VerticalAlignment.Center;Put(grid,state);
  var support=Command(Branding.Author+" · 赞赏作者",ShowSupport);support.Background=Brushes.Transparent;support.Foreground=B(Muted);support.Padding=new(0);Put(grid,support,1);return grid;
 }
 void RenderDesigned()
 {
  Shell.Children.Clear();Shell.RowDefinitions.Clear();Shell.ColumnDefinitions.Clear();Shell.Background=B(Ink);Shell.RowDefinitions.Add(new(){Height=new GridLength(70)});Shell.RowDefinitions.Add(new());Shell.RowDefinitions.Add(new(){Height=new GridLength(38)});Put(Shell,DesignHeader());
  var body=tracker.Data.MainDesign switch{"armory"=>ArmoryBody(),"arena"=>ArenaBody(),_=>TacticalBody()};body.Margin=new(24,4,24,8);Put(Shell,body,0,1);Put(Shell,DesignFooter(),0,2);
 }
 UIElement SummaryLine()
 {
  var tally=ProgressSummary.Collection(tracker.Data);var gold=ProgressSummary.Route(tracker.Data,"gold");var grid=new Grid();for(int i=0;i<3;i++)grid.ColumnDefinitions.Add(new());
  var stats=new[]{("总收集率",Pct(tally.Percent)),("黄金武器",gold.Done+" / "+gold.Total),(tracker.Data.SessionConfigured?"今日计划":"推荐目标",(tracker.Data.SessionConfigured?tracker.Data.SessionWeapons.Count:DesignWeapons().Count)+" / 5")};
  for(int i=0;i<stats.Length;i++)
  {
   var text=Row(Figure(stats[i].Item2,22,i==0?Acid:White),new Border{Width=12},Label(stats[i].Item1,10,Muted));var button=Command("",i==2?ShowInventory:ShowGlobalProgress);button.Content=text;button.Background=Brushes.Transparent;button.Margin=new(0);button.Padding=new(0);button.HorizontalContentAlignment=HorizontalAlignment.Left;Put(grid,button,i);
  }
  return grid;
 }
 UIElement WeaponStage(Weapon weapon,double height)
 {
  var stage=new Grid{Height=height,Background=B("#0A0F0E"),ClipToBounds=true};var image=WeaponArt.Image(weapon);stage.Children.Add(new Image{Source=image,Stretch=Stretch.Uniform,Margin=new(10,6,10,20)});if(image==null){var empty=Label("暂无对应枪型",11,Muted);empty.HorizontalAlignment=HorizontalAlignment.Center;empty.VerticalAlignment=VerticalAlignment.Center;stage.Children.Add(empty);}
  var caption=Label("BASE WEAPON  /  原厂枪型",8,Muted);caption.Margin=new(10,0,0,7);caption.VerticalAlignment=VerticalAlignment.Bottom;stage.Children.Add(caption);return stage;
 }
 UIElement CamoReference(Challenge c)
 {
  var key=OfficialArt.Key(c);var image=OfficialArt.Image(key);var row=new Grid();row.ColumnDefinitions.Add(new(){Width=new GridLength(64)});row.ColumnDefinitions.Add(new());
  Put(row,new Image{Source=image,Height=38,Width=54,Stretch=Stretch.UniformToFill});var text=Stack(OneLine(image==null?"迷彩图片待补充":OfficialArt.Caption(key),10,Muted),OneLine("CAMO REFERENCE  ↗",8,Muted));text.VerticalAlignment=VerticalAlignment.Center;Put(row,text,1);
  var button=Command("",()=>{if(image!=null)ShowArt(key);else ChooseArt();});button.Content=row;button.HorizontalContentAlignment=HorizontalAlignment.Stretch;button.Background=Brushes.Transparent;button.Padding=new(0);button.Margin=new(0);button.ToolTip=OfficialArt.Caption(key);return button;
 }
 UIElement TaskRules(Weapon w,Challenge c)
 {
  var stack=new StackPanel();stack.Children.Add(Label(c.Rule.Split('。')[0],12,White));var en=Bilingual.RuleEn(c);if(en.Length>0){var english=Label(en,10,Muted);english.Margin=new(0,4,0,0);stack.Children.Add(english);}var gate=Eligibility.Reason(tracker.Data,w,c);if(gate!=null){var label=Label("前置："+gate,10,"#B9A0EB");label.Margin=new(0,5,0,0);stack.Children.Add(label);}return stack;
 }
 UIElement TaskActions(bool compact=false)
 {
  var c=tracker.Current;var panel=new StackPanel();var row=Row(Command(c.Done?"已解锁 ✓":c.GameRewardOnly?"游戏里已解锁":"标记已解锁",MarkCurrentUnlocked,true),Command(c.GameRewardOnly?"前置详情":"局后更新",RecordProgress),Command("↶",Undo));((Button)row.Children[0]).IsEnabled=CanConfirm(tracker.CurrentWeapon,c);((Button)row.Children[0]).ToolTip=Eligibility.Reason(tracker.Data,tracker.CurrentWeapon,c);((Button)row.Children[1]).IsEnabled=c.GameRewardOnly||c.IsLevelReward||Eligibility.Reason(tracker.Data,tracker.CurrentWeapon,c)==null;((Button)row.Children[2]).IsEnabled=tracker.CanUndo;
  if(compact)foreach(var button in row.Children.OfType<Button>()){button.FontSize=11;button.Padding=new(10,9,10,9);button.Margin=new(0,0,5,0);}panel.Children.Add(row);
  var w=tracker.CurrentWeapon;var today=new CheckBox{Content="今天刷这把",IsChecked=tracker.InSession(w),Foreground=B(Acid),Margin=new(0,11,0,0)};today.Click+=(_,_)=>SetToday(w,today.IsChecked==true);panel.Children.Add(Row(today,Command("武器路线",()=>ShowWeaponJourney(w))));return panel;
 }
 UIElement NumericProgress(Challenge c,double size=36)
 {
  var row=new Grid();row.ColumnDefinitions.Add(new());row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});Put(row,Figure(c.Done?"已解锁":c.GameRewardOnly?"等待游戏解锁":c.ProgressKnown?c.Count.ToString("D2"):"—",c.Done?24:size));var note=Label(c.GameRewardOnly?"游戏进度奖励":c.IsLevelReward?"等级奖励":c.ProgressKnown?"/ "+c.Target:"未录进度",11,Muted);note.VerticalAlignment=VerticalAlignment.Bottom;note.Margin=new(0,0,0,5);Put(row,note,1);return row;
 }
 Grid TacticalBody()
 {
  var body=new Grid();body.RowDefinitions.Add(new(){Height=new GridLength(48)});body.RowDefinitions.Add(new());Put(body,SummaryLine());
  var content=new Grid();content.ColumnDefinitions.Add(new(){Width=new GridLength(280)});content.ColumnDefinitions.Add(new(){Width=new GridLength(18)});content.ColumnDefinitions.Add(new());
  var queue=new DockPanel();var heading=new Grid{Margin=new(0,0,0,10)};heading.ColumnDefinitions.Add(new());heading.ColumnDefinitions.Add(new(){Width=GridLength.Auto});Put(heading,Label("今日 / 推荐",14,White,true));var more=Command("挑选  +",ShowInventory);more.FontSize=10;more.Padding=new(7,4,7,4);more.Margin=new(0);Put(heading,more,1);DockPanel.SetDock(heading,Dock.Top);queue.Children.Add(heading);
  var rows=new StackPanel();int n=0;foreach(var (w,c) in DesignWeapons())rows.Children.Add(QueueRow(w,c,n++,true,()=>SelectGoal(w,c)));if(n==0)rows.Children.Add(Label("暂无目标",12,Muted));queue.Children.Add(new ScrollViewer{Content=rows,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});Put(content,queue);
  var weapon=tracker.CurrentWeapon;var current=tracker.Current;var detail=new StackPanel();var title=new Grid();title.ColumnDefinitions.Add(new());title.ColumnDefinitions.Add(new(){Width=GridLength.Auto});Put(title,Stack(Label(Bilingual.WeaponZh(weapon),25,White,true),Label(Bilingual.WeaponEn(weapon),10,Muted)));Put(title,Label(weapon.Category,10,Acid,true),1);detail.Children.Add(title);
  var stage=WeaponStage(weapon,98);stage.SetValue(MarginProperty,new Thickness(0,10,0,11));detail.Children.Add(stage);detail.Children.Add(Label(Bilingual.Camo(current),15,Acid,true));var rule=TaskRules(weapon,current);rule.SetValue(MarginProperty,new Thickness(0,7,0,8));detail.Children.Add(rule);detail.Children.Add(NumericProgress(current));var progress=Progress(current,1);progress.SetValue(MarginProperty,new Thickness(0,3,0,12));detail.Children.Add(progress);detail.Children.Add(TaskActions());
  var reference=CamoReference(current);reference.SetValue(MarginProperty,new Thickness(0,12,0,0));detail.Children.Add(reference);Put(content,Frame(new ScrollViewer{Content=detail,VerticalScrollBarVisibility=ScrollBarVisibility.Auto},16),2);Put(body,content,0,1);return body;
 }
 Button WeaponTile(Weapon w,Challenge c,bool arena)
 {
  bool active=tracker.Data.SelectedWeapon==w.Id&&tracker.Data.SelectedChallenge==c.Id;var root=new StackPanel();
  var top=new Grid();top.ColumnDefinitions.Add(new());top.ColumnDefinitions.Add(new(){Width=GridLength.Auto});var check=new CheckBox{Content=tracker.InSession(w)?"今日":"加入今日",IsChecked=tracker.InSession(w),Foreground=B(active?Acid:Muted),FontSize=9};check.Click+=(_,_)=>SetToday(w,check.IsChecked==true);Put(top,check);var value=OneLine(c.Done?"✓":ProgressSummary.Percent(c) is {} p?Pct(p):"—",10,active?Acid:Muted,true);Put(top,value,1);root.Children.Add(top);
  if(arena)
  {
   var headline=new Grid{Margin=new(0,8,0,8)};headline.ColumnDefinitions.Add(new());headline.ColumnDefinitions.Add(new(){Width=new GridLength(89)});Put(headline,Stack(OneLine(Bilingual.WeaponZh(w),13,White,true),OneLine(Bilingual.WeaponEn(w),9,Muted)));Put(headline,GunThumbnail(w,84,40),1);root.Children.Add(headline);
  }
  else
  {
   root.Children.Add(GunThumbnail(w,130,46));root.Children.Add(OneLine(Bilingual.WeaponZh(w),12,White,true));root.Children.Add(OneLine(Bilingual.WeaponEn(w),9,Muted));
  }
  root.Children.Add(OneLine(ShortName(c),10,ColorFor(c)));var bar=Progress(c,.9);bar.SetValue(MarginProperty,new Thickness(0,7,0,0));root.Children.Add(bar);
  var button=Command("",()=>SelectGoal(w,c));button.Tag="weapon-"+w.Id;button.Content=root;button.Padding=new(arena?12:10);button.Margin=new(0,0,8,arena?8:0);button.HorizontalContentAlignment=HorizontalAlignment.Stretch;button.VerticalContentAlignment=VerticalAlignment.Stretch;button.Background=active?new SolidColorBrush(DesignTint(.15)):B("#19211F");button.ToolTip=Bilingual.WeaponPair(w)+"\n"+Bilingual.Camo(c)+"\n"+Bilingual.Rule(c);return button;
 }
 Grid ArmoryBody()
 {
  var body=new Grid();body.RowDefinitions.Add(new());body.RowDefinitions.Add(new(){Height=new GridLength(14)});body.RowDefinitions.Add(new(){Height=new GridLength(164)});
  var hero=new Grid();hero.ColumnDefinitions.Add(new(){Width=new GridLength(304)});hero.ColumnDefinitions.Add(new(){Width=new GridLength(22)});hero.ColumnDefinitions.Add(new());var w=tracker.CurrentWeapon;var c=tracker.Current;var info=new StackPanel();
  info.Children.Add(Label("本局目标 / ACTIVE LOADOUT",9,Acid,true));var title=Label(Bilingual.WeaponZh(w),28,White,true);title.Margin=new(0,9,0,1);info.Children.Add(title);info.Children.Add(Label(Bilingual.WeaponEn(w)+" · "+w.Category,10,Muted));var camo=Label(Bilingual.Camo(c),14,Acid,true);camo.Margin=new(0,16,0,8);info.Children.Add(camo);info.Children.Add(TaskRules(w,c));var number=NumericProgress(c,32);number.SetValue(MarginProperty,new Thickness(0,8,0,3));info.Children.Add(number);info.Children.Add(Progress(c,1));var actions=TaskActions(true);actions.SetValue(MarginProperty,new Thickness(0,11,0,0));info.Children.Add(actions);Put(hero,new ScrollViewer{Content=info,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
  var showcase=new DockPanel();var meta=Row(Label("原厂枪型",10,Muted),new Border{Width=14},Label("BASE WEAPON",9,Muted));DockPanel.SetDock(meta,Dock.Top);showcase.Children.Add(meta);var reference=CamoReference(c);reference.SetValue(MarginProperty,new Thickness(0,6,0,0));DockPanel.SetDock(reference,Dock.Bottom);showcase.Children.Add(reference);showcase.Children.Add(new Image{Source=WeaponArt.Image(w),Stretch=Stretch.Uniform,Margin=new(0,10,0,4)});Put(hero,showcase,2);Put(body,Frame(hero,19,"#101A1E"));
  var rail=new DockPanel();var head=new Grid{Margin=new(0,0,0,7)};head.ColumnDefinitions.Add(new());head.ColumnDefinitions.Add(new(){Width=GridLength.Auto});Put(head,Label("今日 / 推荐",12,White,true));var add=Command("挑选其他武器  +",ShowInventory);add.FontSize=10;add.Padding=new(6,2,6,2);add.Margin=new(0);Put(head,add,1);DockPanel.SetDock(head,Dock.Top);rail.Children.Add(head);var tiles=new Grid();var rows=DesignWeapons();for(int i=0;i<5;i++)tiles.ColumnDefinitions.Add(new());for(int i=0;i<rows.Count;i++)Put(tiles,WeaponTile(rows[i].weapon,rows[i].challenge,false),i);if(rows.Count==0)Put(tiles,Label("暂无目标",12,Muted));rail.Children.Add(tiles);Put(body,rail,0,2);return body;
 }
 UIElement ProgressRing(Challenge c)
 {
  var grid=new Grid{Width=122,Height=122};grid.Children.Add(new System.Windows.Shapes.Ellipse{Stroke=B(Line),StrokeThickness=5,Margin=new(5)});double? percentage=ProgressSummary.Percent(c);
  if(percentage>=100)grid.Children.Add(new System.Windows.Shapes.Ellipse{Stroke=B(Acid),StrokeThickness=5,Margin=new(5)});
  else if(percentage>0)
  {
   double radians=percentage.Value/100*Math.PI*2;var geometry=new PathGeometry();var figure=new PathFigure{StartPoint=new Point(61,7)};figure.Segments.Add(new ArcSegment(new Point(61+54*Math.Sin(radians),61-54*Math.Cos(radians)),new Size(54,54),0,percentage>50,SweepDirection.Clockwise,true));geometry.Figures.Add(figure);grid.Children.Add(new System.Windows.Shapes.Path{Data=geometry,Stroke=B(Acid),StrokeThickness=5,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round});
  }
  var value=Figure(percentage is {} p?Pct(p):"—",percentage is >99?26:30,Acid);value.HorizontalAlignment=HorizontalAlignment.Center;value.VerticalAlignment=VerticalAlignment.Center;grid.Children.Add(value);return grid;
 }
 Grid ArenaBody()
 {
  var body=new Grid();body.RowDefinitions.Add(new(){Height=new GridLength(58)});body.RowDefinitions.Add(new());Put(body,SummaryLine());var content=new Grid();content.ColumnDefinitions.Add(new());content.ColumnDefinitions.Add(new(){Width=new GridLength(16)});content.ColumnDefinitions.Add(new(){Width=new GridLength(306)});
  var tiles=new Grid();tiles.ColumnDefinitions.Add(new());tiles.ColumnDefinitions.Add(new());for(int r=0;r<3;r++)tiles.RowDefinitions.Add(new());var rows=DesignWeapons();for(int i=0;i<rows.Count;i++)Put(tiles,WeaponTile(rows[i].weapon,rows[i].challenge,true),i%2,i/2);var more=Command("+  挑选其他武器",ShowInventory);more.Background=Brushes.Transparent;more.Foreground=B(Muted);more.Margin=new(0,0,8,8);Put(tiles,more,rows.Count%2,rows.Count/2);Put(content,tiles);
  var w=tracker.CurrentWeapon;var c=tracker.Current;var focus=new StackPanel();focus.Children.Add(Label("本局目标 / FOCUS",10,Acid,true));var title=Label(Bilingual.WeaponZh(w),23,White,true);title.Margin=new(0,10,0,0);focus.Children.Add(title);focus.Children.Add(Label(Bilingual.WeaponEn(w),10,Muted));var ring=ProgressRing(c);ring.SetValue(MarginProperty,new Thickness(0,10,0,10));focus.Children.Add(ring);focus.Children.Add(Label(Bilingual.Camo(c),14,Acid,true));var rules=TaskRules(w,c);rules.SetValue(MarginProperty,new Thickness(0,8,0,10));focus.Children.Add(rules);focus.Children.Add(TaskActions(true));var reference=CamoReference(c);reference.SetValue(MarginProperty,new Thickness(0,13,0,0));focus.Children.Add(reference);Put(content,Frame(new ScrollViewer{Content=focus,VerticalScrollBarVisibility=ScrollBarVisibility.Auto},15),2);Put(body,content,0,1);return body;
 }
 void CaptureMainDesigns(string path)
 {
  string original=tracker.Data.MainDesign;bool hidden=!IsVisible;bool activated=ShowActivated;ShowActivated=false;if(hidden)Show();var pictures=new List<(string title,BitmapSource image)>();
  bool HasText(DependencyObject node,string value){if(node is TextBlock text&&text.Text==value)return true;for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)if(HasText(VisualTreeHelper.GetChild(node,i),value))return true;return false;}
  Button? FindWeapon(DependencyObject node,string id){if(node is Button button&&Equals(button.Tag,"weapon-"+id))return button;for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++){var found=FindWeapon(VisualTreeHelper.GetChild(node,i),id);if(found!=null)return found;}return null;}
  foreach(var design in Designs.Where(d=>d.key!="classic"))
  {
   tracker.Data.MainDesign=design.key;Render();UpdateLayout();foreach(var text in new[]{"汉86","Han 86","全局进度","已解锁清单","标记已解锁"})if(!HasText(Shell,text))throw new Exception(design.title+" missing visible control: "+text);
   var oldWeapon=tracker.Data.SelectedWeapon;var oldChallenge=tracker.Data.SelectedChallenge;var m4=tracker.Data.Weapons.First(w=>w.Name=="M4");var choice=FindWeapon(Shell,m4.Id)??throw new Exception("Design weapon selection missing");choice.RaiseEvent(new RoutedEventArgs(Button.ClickEvent,choice));if(tracker.CurrentWeapon.Id!=m4.Id)throw new Exception("Design selection did not update active weapon");tracker.Data.SelectedWeapon=oldWeapon;tracker.Data.SelectedChallenge=oldChallenge;Render();UpdateLayout();
   var bitmap=new RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(this);SavePng(bitmap,System.IO.Path.ChangeExtension(path,"design-"+design.key+".png"));pictures.Add((design.title,bitmap));
   if(mini!=null){mini.UpdateLayout();var hud=new RenderTargetBitmap((int)mini.ActualWidth,(int)mini.ActualHeight,96,96,PixelFormats.Pbgra32);hud.Render(mini);SavePng(hud,System.IO.Path.ChangeExtension(path,"design-"+design.key+"-hud.png"));}
  }
  var visual=new DrawingVisual();using(var draw=visual.RenderOpen())
  {
   draw.DrawRectangle(B("#080D0B"),null,new Rect(0,0,1500,420));for(int i=0;i<pictures.Count;i++){draw.DrawText(new FormattedText(pictures[i].title,CultureInfo.GetCultureInfo("zh-CN"),FlowDirection.LeftToRight,new Typeface(FontFamily,FontStyles.Normal,FontWeights.SemiBold,FontStretches.Normal),17,B("#D7E4DA"),1),new Point(18+i*500,18));draw.DrawImage(pictures[i].image,new Rect(18+i*500,55,470,325));}
  }
  var comparison=new RenderTargetBitmap(1500,420,96,96,PixelFormats.Pbgra32);comparison.Render(visual);SavePng(comparison,System.IO.Path.ChangeExtension(path,"designs.png"));tracker.Data.MainDesign=original;Render();if(hidden)Hide();ShowActivated=activated;
 }
}

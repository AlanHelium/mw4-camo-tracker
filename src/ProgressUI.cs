using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace CamoTracker;
public partial class MainWindow
{
 void ShowGlobalProgress()=>FromOverlay(()=>CreateProgressWindow().ShowDialog());
 static string Pct(double value)=>value.ToString("0.#",CultureInfo.InvariantCulture)+"%";
 static string LeftPct(Challenge c)=>ProgressSummary.Percent(c) is {} p?"还差 "+Pct(100-p):"未录进度";
 Border Metric(string name,string value,string sub,string color)
 {
  var panel=Stack(Label(name,11,Muted),Label(value,24,color,true),Label(sub,10,Muted));return new Border{Child=panel,Background=B(Surface),Padding=new(16,12,16,12),CornerRadius=new(8),Margin=new(0,0,9,0)};
 }
 Window CreateProgressWindow()
 {
  var dialog=new Window{Title=Branding.Chinese+" · 全局进度 / Progress",Width=1060,Height=800,MinWidth=970,MinHeight=650,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=B(Ink),Foreground=B(White),FontFamily=FontFamily};dialog.Resources=Resources;dialog.SourceInitialized+=(_,_)=>DarkTitle(dialog);
  var root=new DockPanel{Margin=new(22)};
  var top=new Grid{Margin=new(0,0,0,15)};top.ColumnDefinitions.Add(new());top.ColumnDefinitions.Add(new(){Width=GridLength.Auto});Put(top,Stack(Label("全局进度",25,White,true),Label("PROGRESS / 本地记录",10,Muted)));Put(top,Command("完成",()=>dialog.Close(),true),1);DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);
  var overview=new StackPanel();DockPanel.SetDock(overview,Dock.Top);root.Children.Add(overview);
  var controls=new Grid{Margin=new(0,16,0,10)};controls.ColumnDefinitions.Add(new());controls.ColumnDefinitions.Add(new(){Width=new GridLength(180)});controls.ColumnDefinitions.Add(new(){Width=new GridLength(180)});
  var search=new TextBox{ToolTip="搜索中文 / 英文武器名",Margin=new(0,0,10,0),Padding=new(9,6,9,6)};var searchHost=new Grid();searchHost.Children.Add(search);var placeholder=Label("搜索武器 · 中文 / English",11,Muted);placeholder.IsHitTestVisible=false;placeholder.Margin=new(10,0,0,0);placeholder.VerticalAlignment=VerticalAlignment.Center;searchHost.Children.Add(placeholder);Put(controls,searchHost);
  var categories=new ComboBox{ItemsSource=new[]{"全部类别"}.Concat(ProgressSummary.Weapons(tracker.Data).Select(w=>w.Category).Distinct()).ToList(),SelectedIndex=0,Margin=new(0,0,10,0),Padding=new(8,5,8,5)};Put(controls,categories,1);
  var goal=new ComboBox{ItemsSource=new[]{"黄金收集 / Gold","白金收集 / Platinum","赤律收集 / Harmonic","分歧前置 / Divergence","武器巅峰 / Prestige","全部迷彩 / All"},SelectedIndex=0,Padding=new(8,5,8,5)};Put(controls,goal,2);DockPanel.SetDock(controls,Dock.Top);root.Children.Add(controls);
  var routePanel=new StackPanel();DockPanel.SetDock(routePanel,Dock.Top);root.Children.Add(routePanel);
  var matrix=new DockPanel();root.Children.Add(matrix);var list=new StackPanel();var scroll=new ScrollViewer{Content=list,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
  double[] widths=[270,68,68,58,58,58,68,1];
  Grid Columns(){var grid=new Grid();foreach(var width in widths)grid.ColumnDefinitions.Add(new(){Width=width==1?new GridLength(1,GridUnitType.Star):new GridLength(width)});return grid;}
  var headings=Columns();headings.Margin=new(8,10,8,9);string[] headers=["武器 / WEAPON","军事","特殊","黄金","白金","赤律","巅峰","最近目标"];
  for(int i=0;i<headers.Length;i++)Put(headings,Label(headers[i],10,Muted),i);DockPanel.SetDock(headings,Dock.Top);matrix.Children.Add(headings);matrix.Children.Add(scroll);
  void Follow(Weapon w,Challenge c)
  {
   try{tracker.SetSession(w,true);SelectGoal(w,c,true);dialog.Close();}catch(InvalidOperationException e){MessageBox.Show(dialog,e.Message,"今日计划");}
  }
  void Overview()
  {
   overview.Children.Clear();var stats=new Grid();for(int i=0;i<4;i++)stats.ColumnDefinitions.Add(new());var all=ProgressSummary.Collection(tracker.Data);Put(stats,Metric("已记录解锁率",Pct(all.Percent),$"{all.Done} / {all.Total} 项",Acid));int column=1;
   foreach(var (kind,label,color) in new[]{("gold","黄金武器","#E9D392"),("platinum","白金武器","#D1DEE5"),("harmonic","赤律武器","#B9A0EB")}){var tally=ProgressSummary.Route(tracker.Data,kind);Put(stats,Metric(label,$"{tally.Done} / {tally.Total}","收集率 "+Pct(tally.Percent),color),column++);}overview.Children.Add(stats);
   var title=Label("最近可完成 · 按已录进度排序",12,White,true);title.Margin=new(0,14,0,8);overview.Children.Add(title);var closest=ProgressSummary.Closest(tracker.Data);var cards=new Grid();for(int i=0;i<3;i++)cards.ColumnDefinitions.Add(new());
   for(int i=0;i<closest.Count;i++)
   {
    var (w,c)=closest[i];var name=Label(Bilingual.WeaponPair(w),12,White,true);name.TextWrapping=TextWrapping.NoWrap;name.TextTrimming=TextTrimming.CharacterEllipsis;var camo=Label(Bilingual.Camo(c),11,"#73C9BC");camo.TextWrapping=TextWrapping.NoWrap;camo.TextTrimming=TextTrimming.CharacterEllipsis;var state=Label(Pct(ProgressSummary.Percent(c)!.Value)+" · "+LeftPct(c)+$"  ({c.Count}/{c.Target})",11,Acid);state.Margin=new(0,5,0,6);var button=Command("本局冲这个",()=>Follow(w,c));button.FontSize=10;button.Padding=new(8,5,8,5);button.HorizontalAlignment=HorizontalAlignment.Left;
    var box=new Border{Child=Stack(name,camo,state,button),Background=B(Surface),Padding=new(12),CornerRadius=new(8),Margin=new(0,0,9,0)};Put(cards,box,i);
   }
   if(closest.Count==0)Put(cards,Label("录入游戏内进度后，这里显示最接近完成的目标。",11,Muted));overview.Children.Add(cards);
  }
  void Rebuild()
  {
   routePanel.Children.Clear();list.Children.Clear();string kind=new[]{"gold","platinum","harmonic","harmonic","prestige","all"}[Math.Max(0,goal.SelectedIndex)];var tally=ProgressSummary.Route(tracker.Data,kind);var route=Label($"{goal.SelectedItem}    已完成 {Pct(tally.Percent)} · 还差 {Pct(tally.Remaining)}    {tally.Done}/{tally.Total}"+(goal.SelectedIndex==3?" 把赤律":""),12,Acid,true);route.ToolTip="按本地已解锁项目数计算；不是击杀数、用时估算或账号自动同步。";routePanel.Children.Add(route);var progress=new Grid{Height=3,Background=B(Line),Margin=new(0,8,0,3)};var fill=new Border{Background=B(Acid),HorizontalAlignment=HorizontalAlignment.Left};progress.Children.Add(fill);progress.SizeChanged+=(_,_)=>fill.Width=progress.ActualWidth*tally.Percent/100;routePanel.Children.Add(progress);
   foreach(var w in ProgressSummary.Weapons(tracker.Data).Where(w=>(categories.SelectedIndex==0||w.Category==(string?)categories.SelectedItem)&&(Bilingual.WeaponPair(w)+" "+w.Name).Contains(search.Text,StringComparison.OrdinalIgnoreCase)))
   {
    var row=Columns();var weaponBlock=new Grid();weaponBlock.ColumnDefinitions.Add(new(){Width=new GridLength(66)});weaponBlock.ColumnDefinitions.Add(new());Put(weaponBlock,new Image{Source=WeaponArt.Image(w),Width=58,Height=38,Stretch=Stretch.Uniform});var name=Label(Bilingual.WeaponZh(w),12,White,true);name.TextWrapping=TextWrapping.NoWrap;name.TextTrimming=TextTrimming.CharacterEllipsis;var en=Label(Bilingual.WeaponEn(w),9,Muted);en.TextWrapping=TextWrapping.NoWrap;en.TextTrimming=TextTrimming.CharacterEllipsis;Put(weaponBlock,Stack(name,en),1);Put(row,weaponBlock);
    int col=1;foreach(var k in new[]{"military","special","gold","platinum","harmonic","prestige"}){var group=w.Challenges.Where(c=>c.Kind==k).ToList();string text=group.Count==0?"—":group.Count==1?(group[0].Done?"✓":"—"):$"{group.Count(c=>c.Done)}/{group.Count}";var value=Label(text,12,group.Count>0&&group.All(c=>c.Done)?Acid:Muted);value.VerticalAlignment=VerticalAlignment.Center;Put(row,value,col++);}
    var next=ProgressSummary.Nearest(tracker.Data,w)??Recommendations.ForWeapon(tracker.Data,w);var nextName=Label(next==null?"已全部解锁":Bilingual.Camo(next),10,next==null?Acid:"#73C9BC");nextName.TextWrapping=TextWrapping.NoWrap;nextName.TextTrimming=TextTrimming.CharacterEllipsis;var state=Label(next==null?"100%":Eligibility.Reason(tracker.Data,w,next) is {} reason?reason:ProgressSummary.Percent(next) is {} p?Pct(p)+" · "+LeftPct(next):"未录进度",9,Muted);state.TextWrapping=TextWrapping.NoWrap;state.TextTrimming=TextTrimming.CharacterEllipsis;Put(row,Stack(nextName,state),7);
    var button=Command("",()=>{tracker.Data.SelectedWeapon=w.Id;tracker.Normalize();Render();var inventory=CreateInventoryWindow();inventory.Owner=dialog;inventory.ShowDialog();Overview();Rebuild();});button.Content=row;button.HorizontalContentAlignment=HorizontalAlignment.Stretch;button.Padding=new(8,8,8,8);button.Margin=new(0,0,0,4);button.ToolTip=Bilingual.WeaponPair(w)+" · 点击查看已有解锁";list.Children.Add(button);
   }
   if(list.Children.Count==0)list.Children.Add(Label("没有匹配的武器",12,Muted));
  }
  search.TextChanged+=(_,_)=>{placeholder.Visibility=search.Text.Length==0?Visibility.Visible:Visibility.Collapsed;Rebuild();};categories.SelectionChanged+=(_,_)=>Rebuild();goal.SelectionChanged+=(_,_)=>Rebuild();Overview();Rebuild();dialog.Content=root;return dialog;
 }
}

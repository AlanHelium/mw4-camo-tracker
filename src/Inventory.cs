using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
namespace CamoTracker;
public partial class MainWindow
{
 void SelectGoal(Weapon w,Challenge c,bool focus=false){tracker.Data.SelectedWeapon=w.Id;tracker.Data.SelectedChallenge=c.Id;w.OverlayChallengeId=c.Id;if(focus)w.FocusChallengeId=c.Id;tracker.Save();Render();}
 void SetToday(Weapon w,bool selected){try{tracker.SetSession(w,selected);}catch(InvalidOperationException e){MessageBox.Show(e.Message,"今日计划");}Render();}
 void MarkCurrentUnlocked()
 {
  var w=tracker.CurrentWeapon;var c=tracker.Current;if(c.Done)return;tracker.MarkUnlocked(w,c,true);
  var next=Recommendations.ForWeapon(tracker.Data,w);if(next!=null)tracker.Data.SelectedChallenge=next.Id;tracker.Save();Render();
 }
 void RecordProgress()
 {
  var c=tracker.Current;if(c.IsLevelReward){RecordLevels();return;}
  var values=Form("局后更新",[("游戏内当前总进度",c.ProgressKnown?c.Count.ToString():"")]);if(values==null)return;
  if(!int.TryParse(values[0],out var count)||count<0||count>c.Target){MessageBox.Show($"请输入 0 到 {c.Target} 之间的进度。未知进度可不填写。");return;}
  tracker.RecordTotal(tracker.CurrentWeapon,c,count);Render();
 }
 void ShowInventory()=>FromOverlay(()=>CreateInventoryWindow().ShowDialog());
 Window CreateInventoryWindow()
 {
  var dialog=new Window{Title="武器与已有解锁",Width=940,Height=700,MinWidth=820,MinHeight=580,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=B(Ink),Foreground=B(White),FontFamily=FontFamily};dialog.Resources=Resources;dialog.SourceInitialized+=(_,_)=>DarkTitle(dialog);
  var root=new Grid{Margin=new(22)};root.ColumnDefinitions.Add(new(){Width=new GridLength(240)});root.ColumnDefinitions.Add(new(){Width=new GridLength(22)});root.ColumnDefinitions.Add(new());
  var left=new DockPanel();var search=new TextBox{ToolTip="搜索武器",Margin=new(0,5,0,12)};var searchHeader=Stack(Label("武器",12,Muted),search);DockPanel.SetDock(searchHeader,Dock.Top);left.Children.Add(searchHeader);var weapons=new StackPanel();left.Children.Add(new ScrollViewer{Content=weapons,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});Put(root,left);
  Weapon active=tracker.CurrentWeapon;var details=new StackPanel();var detailScroll=new ScrollViewer{Content=details,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Put(root,detailScroll,2);
  void ListWeapons()
  {
   weapons.Children.Clear();foreach(var w in tracker.Data.Weapons.Where(w=>w.Name.Contains(search.Text,StringComparison.OrdinalIgnoreCase)))
   {
    var line=new Grid();line.ColumnDefinitions.Add(new(){Width=new GridLength(25)});line.ColumnDefinitions.Add(new());line.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
    {var plan=new CheckBox{IsChecked=tracker.InSession(w),VerticalAlignment=VerticalAlignment.Center,ToolTip="加入今日计划"};plan.Click+=(_,_)=>{SetToday(w,plan.IsChecked==true);ListWeapons();};Put(line,plan);}
    Put(line,Label(w.Name,12,White,w==active),1);Put(line,Label(w.Challenges.Count(c=>c.Done)+"/"+w.Challenges.Count,10,Muted),2);
    var button=Command("",()=>{active=w;tracker.Data.SelectedWeapon=w.Id;tracker.Normalize();tracker.Save();ListWeapons();Detail();Render();});button.Content=line;button.HorizontalContentAlignment=HorizontalAlignment.Stretch;button.Margin=new(0,0,0,5);button.Background=B(w==active?"#29372A":Surface);weapons.Children.Add(button);
   }
  }
  void Detail()
  {
   double offset=detailScroll.VerticalOffset;details.Children.Clear();
   var title=new Grid();title.ColumnDefinitions.Add(new());title.ColumnDefinitions.Add(new(){Width=GridLength.Auto});Put(title,Label(active.Name,24,White,true));var done=Command("完成",()=>dialog.Close(),true);done.Margin=new(0);Put(title,done,1);details.Children.Add(title);
   var meta=Row(Label("已解锁 "+active.Challenges.Count(c=>c.Done)+" / "+active.Challenges.Count,11,Muted));meta.Margin=new(0,7,0,13);details.Children.Add(meta);
   {
    var plan=new CheckBox{Content=active.Category=="模式精通"?"今天追踪此路线":"今天刷这把",IsChecked=tracker.InSession(active),Foreground=B(Acid),Margin=new(0,0,0,12)};plan.Click+=(_,_)=>{SetToday(active,plan.IsChecked==true);ListWeapons();Detail();};details.Children.Add(plan);
   }
   foreach(var group in active.Challenges.GroupBy(c=>c.Stage))
   {
    var heading=Label(group.Key,12,ColorFor(group.First()),true);heading.Margin=new(0,15,0,9);details.Children.Add(heading);
    if(group.All(c=>c.Kind=="military"))
    {
     var all=Command("这些军事迷彩均已解锁",()=>{tracker.MarkUnlocked(active,group.Last(),true);ListWeapons();Detail();Render();});all.FontSize=11;all.Margin=new(0,0,0,8);details.Children.Add(all);
     var tiles=new WrapPanel();foreach(var c in group){var check=new CheckBox{Content="Lv."+c.RequiredWeaponLevel,IsChecked=c.Done,Foreground=B(White),Width=100,Margin=new(0,6,0,6),ToolTip=c.Name};check.Click+=(_,_)=>{tracker.MarkUnlocked(active,c,check.IsChecked==true);ListWeapons();Detail();Render();};tiles.Children.Add(check);}details.Children.Add(tiles);continue;
    }
    foreach(var c in group)
    {
     var card=new Grid{Margin=new(0,0,0,8)};card.ColumnDefinitions.Add(new(){Width=new GridLength(27)});card.ColumnDefinitions.Add(new());
     var check=new CheckBox{IsChecked=c.Done,VerticalAlignment=VerticalAlignment.Top,Margin=new(0,4,0,0),ToolTip="已在游戏中解锁"};check.Click+=(_,_)=>{tracker.MarkUnlocked(active,c,check.IsChecked==true);ListWeapons();Detail();Render();};Put(card,check);
     var body=new StackPanel();body.Children.Add(Label(c.Name,13,White,true));var text=Label(c.Rule,11,Muted);text.Margin=new(0,4,0,3);body.Children.Add(text);
     string? gate=Eligibility.Reason(tracker.Data,active,c);if(!c.Done&&gate!=null){var prerequisite=Label(gate,10,"#C8B187");body.Children.Add(prerequisite);}
     if(c.Kind=="special"&&!c.Done)
     {
      var opened=new CheckBox{Content="游戏中已开放此挑战",IsChecked=c.OpenConfirmed,Foreground=B(Muted),FontSize=10,Margin=new(0,6,0,0)};opened.Click+=(_,_)=>{tracker.Snapshot();c.OpenConfirmed=opened.IsChecked==true;tracker.Save();Detail();Render();};body.Children.Add(opened);
     }
     var tools=new StackPanel{Orientation=Orientation.Horizontal,Margin=new(0,6,0,0)};
     var follow=Command("本局冲这个",()=>{try{tracker.SetSession(active,true);SelectGoal(active,c,true);dialog.Close();}catch(InvalidOperationException e){MessageBox.Show(e.Message);}});follow.FontSize=10;follow.Padding=new(8,5,8,5);tools.Children.Add(follow);
     var more=Command("详情",()=>ShowChallengeDetails(active,c));more.FontSize=10;more.Padding=new(8,5,8,5);tools.Children.Add(more);body.Children.Add(tools);Put(card,body,1);details.Children.Add(new Border{Child=card,BorderBrush=B(Line),BorderThickness=new(0,0,0,1),Padding=new(0,4,0,7)});
    }
   }
   dialog.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,new Action(()=>detailScroll.ScrollToVerticalOffset(offset)));
  }
  search.TextChanged+=(_,_)=>ListWeapons();ListWeapons();Detail();dialog.Content=root;return dialog;
 }
 void ShowChallengeDetails(Weapon w,Challenge c)
 {
  var details=c.Rule+"\n\n前置："+(Eligibility.Reason(tracker.Data,w,c)??"已满足当前记录的前置条件")+(string.IsNullOrEmpty(c.DmzRule)?"":"\n\nDMZ 条件参考："+c.DmzRule)+"\n\n资料来源："+c.Source;
  MessageBox.Show(details,c.Name);
 }
}

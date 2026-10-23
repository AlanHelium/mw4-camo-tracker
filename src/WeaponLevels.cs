using System.Windows;
using System.Windows.Controls;
namespace CamoTracker;
public partial class MainWindow
{
 void RecordLevels()
 {
  var w=tracker.CurrentWeapon;
  var window=new Window{Title=w.Name+" · 武器等级",Width=450,Height=440,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize,Background=B(Ink),Foreground=B(White),FontFamily=FontFamily};window.Resources=Resources;window.SourceInitialized+=(_,_)=>DarkTitle(window);
  var panel=new StackPanel{Margin=new(24)};panel.Children.Add(Label(w.Name,23,White,true));panel.Children.Add(new Border{Height=9});panel.Children.Add(Label("这里记录武器等级，与账号巅峰分开。",11,Muted));panel.Children.Add(new Border{Height=18});
  panel.Children.Add(Label("当前阶段",12));var stage=new ComboBox{ItemsSource=new[]{"普通武器 · 还未转入武器巅峰","武器巅峰 1 · 已转生一次","武器巅峰大师 · 巅峰 1 练满后"},SelectedIndex=w.Prestige,Padding=new(8),Margin=new(0,7,0,15)};panel.Children.Add(stage);
  panel.Children.Add(Label("游戏内当前武器等级",12));var level=new TextBox{Text=w.LevelKnown?w.WeaponLevel.ToString():"",Margin=new(0,7,0,15)};panel.Children.Add(level);
  panel.Children.Add(Label("该武器的普通满级等级 · 未知可留空",12,Muted));var max=new TextBox{Text=w.MaxLevel>0?w.MaxLevel.ToString():"",Margin=new(0,7,0,18)};panel.Children.Add(max);
  var save=Command("保存",()=>
  {
   int maximum=0;if(!int.TryParse(level.Text,out var value)||value is <1 or >1000||(!string.IsNullOrWhiteSpace(max.Text)&&(!int.TryParse(max.Text,out maximum)||maximum is <1 or >250))){MessageBox.Show(window,"请核对武器等级和满级等级。");return;}
   int prestige=stage.SelectedIndex;if(prestige==0&&maximum>0&&value>maximum){MessageBox.Show(window,"普通武器等级不能超过该武器满级等级，请核对当前阶段。");return;}if(prestige==1&&maximum>0&&value>=maximum)prestige=2;
   tracker.Snapshot();w.Prestige=prestige;w.WeaponLevel=value;w.MaxLevel=maximum;w.LevelKnown=true;
   foreach(var c in w.Challenges.Where(c=>c.Kind=="military"&&!c.Unlocked)){c.ProgressKnown=true;c.Count=Math.Clamp(value,0,c.Target);if(prestige>0){c.Count=c.Target;c.Unlocked=true;}}
   foreach(var c in w.Challenges.Where(c=>c.Kind=="prestige"&&!c.Unlocked)){c.Count=0;c.ProgressKnown=false;c.Count=Eligibility.Reason(tracker.Data,w,c)==null?1:0;c.ProgressKnown=true;}
   tracker.Save();window.Close();Render();
  },true);panel.Children.Add(save);window.Content=panel;window.ShowDialog();
 }
}

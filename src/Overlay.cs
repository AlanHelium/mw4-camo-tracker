using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
namespace CamoTracker;
public partial class MainWindow
{
 bool overlayLocked;
 string overlayTier="自动推荐";
 readonly HashSet<string> skippedWeapons=[];
 string lockShortcut="Ctrl+Alt+L";
 int overlayPage;
 const string Lime="#A6EB34";
 [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]static extern IntPtr GetWindowLongPtr(IntPtr hwnd,int index);
 [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]static extern IntPtr SetWindowLongPtr(IntPtr hwnd,int index,IntPtr value);
 List<(Weapon weapon,Challenge challenge)> OverlayRows()
 {
  if(overlayTier=="自动推荐"){overlayPage=0;return Recommendations.Next(tracker.Data,skippedWeapons,true);}
  var result=new List<(Weapon,Challenge)>();
  foreach(var w in tracker.Data.Weapons.Where(w=>w.Pinned))
  {
   Challenge? c=overlayTier=="当前挑战"?w.Challenges.FirstOrDefault(c=>c.Id==(w.Id==tracker.Data.SelectedWeapon?tracker.Data.SelectedChallenge:w.OverlayChallengeId))??w.Challenges.FirstOrDefault(c=>!c.Done)??w.Challenges[0]:w.Challenges.FirstOrDefault(c=>OfficialArt.Key(c)==overlayTier);
   if(c!=null)result.Add((w,c));
  }
  overlayPage=Math.Clamp(overlayPage,0,Math.Max(0,(result.Count-1)/5));return result;
 }
 void SelectOverlayRow(int index)
 {
  var rows=OverlayRows().Skip(overlayPage*5).Take(5).ToList();if(index<0||index>=rows.Count)return;
  var (w,c)=rows[index];tracker.Data.SelectedWeapon=w.Id;tracker.Data.SelectedChallenge=c.Id;w.OverlayChallengeId=c.Id;tracker.Save();Render();
 }
 void ToggleMini()
 {
  if(mini is {IsVisible:true}){mini.Hide();Render();Show();Activate();return;}
  if(mini==null)
  {
   mini=new Window{Title="MW4 · 迷彩 HUD",SizeToContent=SizeToContent.WidthAndHeight,WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,ResizeMode=ResizeMode.NoResize,Topmost=true,ShowInTaskbar=true,FontFamily=FontFamily,Left=Math.Max(0,SystemParameters.WorkArea.Right-470),Top=100,ShowActivated=false};
   mini.Resources=Resources;mini.Icon=Icon;mini.SourceInitialized+=(_,_)=>ApplyOverlayLock();
   mini.Closing+=(_,e)=>{if(!shuttingDown){e.Cancel=true;mini.Hide();Show();Activate();}};
  }
  RefreshMini();mini.Show();Hide();
 }
 void ToggleOverlayLock(){if(!overlayLocked&&!registeredHotkeys.Contains(865)&&!preview){MessageBox.Show("Ctrl+Alt+L 被其他程序占用，暂不能开启鼠标穿透，以确保你能随时解锁。","解锁快捷键不可用");return;}overlayLocked=!overlayLocked;ApplyOverlayLock();RefreshMini();}
 void ApplyOverlayLock()
 {
  if(mini==null)return;var h=new WindowInteropHelper(mini).Handle;if(h==IntPtr.Zero)return;
  var style=GetWindowLongPtr(h,-20).ToInt64();const long transparent=0x20,noActivate=0x8000000;
  style=overlayLocked?style|transparent|noActivate:style&~(transparent|noActivate);SetWindowLongPtr(h,-20,new IntPtr(style));
 }
 void ShowTierMenu()
 {
  var menu=new ContextMenu();foreach(var (key,label) in new[]{("自动推荐","自动推荐 5 把（默认）"),("当前挑战","每把武器当前追踪的挑战"),("gold","GOLD · 黄金"),("platinum","PLATINUM · 白金"),("harmonic","HARMONIC BREAK"),("terminus","武器巅峰 · Terminus"),("scatter","巅峰大师 · Scatter"),("cascade","巅峰大师 · Cascade")})
  {var item=new MenuItem{Header=label,IsCheckable=true,IsChecked=key==overlayTier};item.Click+=(_,_)=>{overlayTier=key;overlayPage=0;SelectOverlayRow(0);RefreshMini();};menu.Items.Add(item);}menu.IsOpen=true;
 }
 Image ArtImage(string key)=>new(){Source=OfficialArt.Image(key),Stretch=Stretch.UniformToFill};
 UIElement CamoPreview(Challenge c)
 {
  var key=OfficialArt.Key(c);var content=OfficialArt.Image(key)==null?(UIElement)T("暂无配图\n点击选择",12,"#99AF89"):ArtImage(key);
  var b=Btn("",()=>{if(OfficialArt.Image(key)==null)ChooseArt();else ShowArt(key);});b.Content=content;b.Width=130;b.Height=170;b.Padding=new(0);b.Margin=new(10,0,0,0);b.ToolTip="官方迷彩外观参考 · 点击查看完整原图";return b;
 }
 void ShowArt(string key)
 {
  var dialog=new Window{Title="官方迷彩外观参考（HAN 86）",Width=1000,Height=640,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=B("#101710")};
  var dock=new DockPanel();var label=T("图片来源：Call of Duty 官方博客 · 静态外观参考，不是可交互 3D 材质。",12,"#ADBBA5");label.Margin=new(12);DockPanel.SetDock(label,Dock.Bottom);dock.Children.Add(label);dock.Children.Add(new Image{Source=OfficialArt.Image(key,true),Stretch=Stretch.Uniform});dialog.Content=dock;dialog.ShowDialog();
 }
 void ChooseArt()
 {
  var dialog=new Window{Title="选择迷彩配图",Width=630,Height=420,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=B("#111B12")};dialog.Resources=Resources;
  var wrap=new WrapPanel{Margin=new(16)};foreach(var key in new[]{"smoke","stormwave","red","gold","platinum","harmonic","pyroclast","terminus","scatter","cascade",""})
  {
   var k=key;var b=Btn("",()=>{tracker.Snapshot();tracker.Current.ArtKey=k;tracker.Current.Source=k==""?"用户自定义":tracker.Current.Source;tracker.Save();dialog.Close();Render();});b.Width=180;b.Height=155;b.Padding=new(8);b.Margin=new(0,0,8,8);
   b.Content=Stack(new Image{Source=OfficialArt.Image(k),Height=110,Stretch=Stretch.Uniform},T(k==""?"无配图":k.ToUpperInvariant(),12));wrap.Children.Add(b);
  }
  dialog.Content=new ScrollViewer{Content=wrap};dialog.ShowDialog();
 }
 class UniformGridCompat:StackPanel{public UniformGridCompat(){Orientation=Orientation.Horizontal;}}
 public void OpenOverlay()=>ToggleMini();
}

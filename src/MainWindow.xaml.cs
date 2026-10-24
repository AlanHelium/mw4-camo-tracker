using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
namespace CamoTracker;
public partial class MainWindow : Window
{
 readonly Tracker tracker;
 readonly bool preview;
 Window? mini;
 HwndSource? hook;
 readonly HashSet<int> registeredHotkeys=[];
 string hotkeyStatus="快捷键初始化中";
 bool shuttingDown;
 static Brush B(string color)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
 static TextBlock T(string text,double size=14,string color="#E6EBE7",bool bold=false)=>new(){Text=text,FontSize=size,Foreground=B(color),FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal};
 static StackPanel Stack(params UIElement[] children){var p=new StackPanel();foreach(var c in children)p.Children.Add(c);return p;}
 static StackPanel Row(params UIElement[] children){var p=Stack(children);p.Orientation=Orientation.Horizontal;return p;}
 static Border Card(UIElement content,string color="#161D19",Thickness? padding=null)=>new(){Background=B(color),CornerRadius=new(10),Padding=padding??new Thickness(20),Child=content,BorderBrush=B("#29342C"),BorderThickness=new(1)};
 Button Btn(string label,Action action,bool primary=false){var b=new Button{Content=label}; if(primary){b.Background=B("#C5F66B");b.Foreground=B("#14200C");b.FontWeight=FontWeights.Bold;} b.Click+=(_,_)=>action();return b;}
 static void Put(Grid g,UIElement el,int col=0,int row=0){Grid.SetColumn(el,col);Grid.SetRow(el,row);g.Children.Add(el);}
 public MainWindow(bool previewMode=false)
 {
  InitializeComponent();preview=previewMode;
  tracker=new(preview?null:System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CamoTracker","progress.json"));
  if(preview){tracker.CurrentWeapon.LevelKnown=true;tracker.CurrentWeapon.WeaponLevel=72;tracker.Current.OpenConfirmed=true;foreach(var c in tracker.CurrentWeapon.Challenges.Where(c=>c.Kind=="military")){c.Count=c.Target;c.ProgressKnown=true;c.Unlocked=true;}}
  Topmost=tracker.Data.Topmost;
  var icon=new DrawingGroup();icon.Children.Add(new GeometryDrawing(B("#142018"),null,new RectangleGeometry(new Rect(0,0,32,32),6,6)));icon.Children.Add(new GeometryDrawing(null,new Pen(B("#C5F66B"),3),Geometry.Parse("M16,5 L27,16 L16,27 L5,16 Z")));icon.Children.Add(new GeometryDrawing(B("#C5F66B"),null,new RectangleGeometry(new Rect(12,12,8,8))));Icon=new DrawingImage(icon);
  Render();SourceInitialized+=(_,_)=>{DarkTitle(this);RegisterKeys();};
  Closing+=(_,_)=>{shuttingDown=true;DismissUnlock();mini?.Close();UnregisterKeys();tracker.Save();};
 }
 void Count(int amount)
 {
  if(mini is {IsVisible:true}&&!HudHasTarget)return;
  if(Eligibility.Reason(tracker.Data,tracker.CurrentWeapon,tracker.Current) is {} blocked){MessageBox.Show(blocked,"尚未满足前置");return;}
  if(tracker.Current.IsLevelReward||tracker.Current.GameRewardOnly)return;tracker.Change(amount);Render();
 }

 void Undo(){DismissUnlock();tracker.Undo();Render();RefreshJourney();}
 void ResetCurrent(){if(MessageBox.Show(this,"将“"+tracker.Current.Name+"”的计数归零？可撤销。","重置本项",MessageBoxButton.YesNo)==MessageBoxResult.Yes){tracker.Change(-tracker.Current.Count);Render();}}
 void AddWeapon()
 {
  var values=Form("添加武器",[("武器名称",""),("武器类型","自定义"),("游戏模式","多人模式")]);if(values==null)return;
  if(tracker.Data.Weapons.Count>=200){MessageBox.Show(this,"最多支持 200 把武器。");return;}
  tracker.Snapshot();var w=new Weapon{Name=values[0],Category=values[1],Mode=values[2],Challenges=[new Challenge()]};tracker.Data.Weapons.Add(w);tracker.Data.SelectedWeapon=w.Id;tracker.Data.SelectedChallenge=w.Challenges[0].Id;tracker.Save();Render();
 }
 void EditWeapon()
 {
  var w=tracker.CurrentWeapon;var values=Form("编辑武器",[("武器名称",w.Name),("武器类型",w.Category),("游戏模式",w.Mode)]);if(values==null)return;tracker.Snapshot();w.Name=values[0];w.Category=values[1];w.Mode=values[2];tracker.Save();Render();
 }
 void EditChallenge(bool add)
 {
  var c=add?new Challenge():tracker.Current;
  var values=Form(add?"添加挑战":"编辑当前挑战",[("迷彩名称",c.Name),("阶段",c.Stage),("挑战要求（含模式与前置条件）",c.Rule),("目标次数",c.Target.ToString()),("当前次数",c.Count.ToString())]);if(values==null)return;
  if(!int.TryParse(values[3],out var target)||target is <1 or >1000000||!int.TryParse(values[4],out var count)||count<0||count>target){MessageBox.Show(this,"目标必须为 1–1000000，当前次数必须在 0 与目标之间。请重新编辑。");return;}
  if(add && tracker.CurrentWeapon.Challenges.Count>=200){MessageBox.Show(this,"每把武器最多支持 200 项挑战。");return;}
  tracker.Snapshot();c.ArtKey=OfficialArt.Key(c);c.Name=values[0];c.ChineseName="";c.EnglishName="";c.Stage=values[1];c.Rule=values[2];c.Target=target;c.Count=count;c.ProgressKnown=true;c.Source="用户自定义 / 编辑";c.RequirementUnverified=false;
  if(add)tracker.CurrentWeapon.Challenges.Add(c);tracker.Data.SelectedChallenge=c.Id;tracker.Save();Render();
 }
 string[]? Form(string title,(string label,string value)[] fields)
 {
  var dialog=new Window{Title=title,Width=480,SizeToContent=SizeToContent.Height,MaxHeight=730,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=B("#151E17"),Foreground=B("#E6EBE7"),ResizeMode=ResizeMode.NoResize,FontFamily=FontFamily};dialog.Resources=Resources;
  var panel=new StackPanel{Margin=new(24)};panel.Children.Add(T(title,23,"#E9F3E0",true));panel.Children.Add(T("输入兼容模式：中文可复制粘贴；英文与数字可直接输入。",11,"#8EA486"));var inputs=new List<TextBox>();
  foreach(var f in fields){panel.Children.Add(new Border{Height=13});panel.Children.Add(T(f.label,12,"#A0B49D"));var input=new TextBox{Text=f.value,Margin=new(0,5,0,0),MaxLength=f.label.Contains("要求")?2000:80};if(f.label.Contains("要求")){input.AcceptsReturn=true;input.TextWrapping=TextWrapping.Wrap;input.Height=90;input.VerticalScrollBarVisibility=ScrollBarVisibility.Auto;}inputs.Add(input);panel.Children.Add(input);}
  var buttons=Row(Btn("保存",()=>{if(inputs.Any(x=>string.IsNullOrWhiteSpace(x.Text))){MessageBox.Show(dialog,"请填写所有字段。");return;}dialog.DialogResult=true;},true),Btn("取消",()=>dialog.DialogResult=false));buttons.Margin=new(0,22,0,0);panel.Children.Add(buttons);dialog.Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
  return dialog.ShowDialog()==true?inputs.Select(i=>i.Text.Trim()).ToArray():null;
 }
 void ChangeMode()
 {
  MessageBox.Show(this,"此版本预设为 HAN 86 多人模式。\n\n需要追踪 DMZ 时，请添加单独武器条目（如 HAN 86 · DMZ），再按游戏内数值编辑目标。游戏的跨模式共享进度不会在本工具中自动换算。","模式说明");
 }
 void Export()
 {
  var d=new SaveFileDialog{Filter="迷彩进度 (*.json)|*.json",FileName="MW4-迷彩进度-"+DateTime.Now.ToString("yyyyMMdd")+".json"};
  if(d.ShowDialog(this)==true)try{File.WriteAllText(d.FileName,JsonSerializer.Serialize(tracker.Data,Tracker.JsonOptions));MessageBox.Show(this,"备份已导出。");}catch(Exception ex){MessageBox.Show(this,"导出失败："+ex.Message);}
 }
 void Import()
 {
  var d=new OpenFileDialog{Filter="迷彩进度 (*.json)|*.json"};if(d.ShowDialog(this)!=true)return;
  try{if(new FileInfo(d.FileName).Length>4_000_000)throw new Exception("文件超过 4 MB");var json=File.ReadAllText(d.FileName);Tracker.Parse(json);if(MessageBox.Show(this,"用此存档替换当前进度？可按撤销恢复。","导入存档",MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;tracker.Import(json);Render();}catch(Exception ex){MessageBox.Show(this,"导入失败："+ex.Message);}
 }
 void Help()=>MessageBox.Show(this,"1. 在「已有解锁清单」勾选已拥有的迷彩。军用迷彩按游戏实际已解锁确认，不要求填写普通等级。\n2. 勾选今天要刷的武器（最多 5 把），选中一把打开小窗；小窗只显示当前这一把，点「切枪」更换。\n3. 默认推荐最接近完成且满足已知前置的迷彩，也可点「本局冲这个」。\n4. 游戏中只看目标；局后填游戏内当前总进度，或直接标记「已解锁」。无需逐次加一。\n5. 菜单可导出备份；误操作可撤销。\n\nCtrl+Alt+Enter：确认当前迷彩已解锁；Z：撤销；1–5：切换计划目标；M：主界面/小窗；Space：隐藏/显示；L：鼠标穿透（若占用则用 F8）。\n\n武器巅峰（俗称转生）与玩家巅峰是两条路线。进入武器巅峰并不等于所有巅峰迷彩都已满足等级门槛。\n\n内置 33 把武器的社区条件，缺失或冲突项标记待核对。特殊迷彩开放等级未知时，请按游戏实况确认「已开放」。图片为可核实的官方静态外观参考，尚未覆盖所有迷彩。\n\n独立手动工具，不读取画面、游戏内存或账号。建议搭配无边框窗口；独占全屏与游戏内快捷键效果尚未实机验证。\n\n存档：%LOCALAPPDATA%\\CamoTracker\\progress.json","使用指南");
 [DllImport("user32.dll",SetLastError=true)]static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
 [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr h,int attribute,ref int value,int size);
 static void DarkTitle(Window window){int dark=1;DwmSetWindowAttribute(new WindowInteropHelper(window).Handle,20,ref dark,4);}
 [DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr h,int id);
 void RegisterKeys()
 {
  hook=HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);hook.AddHook(WndProc);var failures=new List<string>();
  uint[] keys=[0x0D,0,0x5A,0x4D,0x20,0x4C,0x31,0x32,0x33,0x34,0x35];string[] labels=["Enter","","Z","M","Space","L","1","2","3","4","5"];for(int i=0;i<keys.Length;i++){if(keys[i]==0)continue;if(RegisterHotKey(hook.Handle,860+i,0x4003,keys[i]))registeredHotkeys.Add(860+i);else failures.Add(labels[i]);}
  if(!registeredHotkeys.Contains(865)&&RegisterHotKey(hook.Handle,865,0x4003,0x77)){registeredHotkeys.Add(865);failures.Remove("L");lockShortcut="Ctrl+Alt+F8";}
  hotkeyStatus=failures.Count==0?"● 全局快捷键已启用":"⚠ Ctrl+Alt+"+string.Join(" / ",failures)+" 被占用";Render();
 }
 void UnregisterKeys(){if(hook==null)return;for(int i=0;i<11;i++)UnregisterHotKey(hook.Handle,860+i);hook.RemoveHook(WndProc);}
 IntPtr WndProc(IntPtr hwnd,int msg,IntPtr wp,IntPtr lp,ref bool handled)
 {
  if(msg!=0x312)return IntPtr.Zero;
  if(!IsEnabled||mini is {IsEnabled:false}){handled=true;return IntPtr.Zero;}
  switch(wp.ToInt32())
  {
   case 860:MarkCurrentUnlocked();break;case 862:Undo();break;case 863:ToggleMini();break;
   case 864:if(mini is {IsVisible:true})mini.Hide();else if(IsVisible)Hide();else if(mini!=null)mini.Show();else Show();break;
   case 865:ToggleOverlayLock();break;
   case >=866 and <=870:SelectOverlayRow(wp.ToInt32()-866);break;
   default:return IntPtr.Zero;
  }
  handled=true;return IntPtr.Zero;
 }
 public void CaptureAndExit(string path)
 {
  Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(async ()=>
  {
   try{
    // Exercise actual UI event handlers against an isolated, memory-only tracker.
    Count(5);if(tracker.Current.Count!=5)throw new Exception("UI increment failed");Undo();if(tracker.Current.Count!=0)throw new Exception("UI undo failed");
    var before=tracker.Current.Id;bool handled=false;WndProc(IntPtr.Zero,0x312,new IntPtr(860),IntPtr.Zero,ref handled);if(!handled||!tracker.Data.Weapons.SelectMany(w=>w.Challenges).First(c=>c.Id==before).Done)throw new Exception("unlock hotkey handler failed");Undo();
    var m4=tracker.Data.Weapons.First(w=>w.Name=="M4");m4.LevelKnown=true;m4.WeaponLevel=59;foreach(var c in m4.Challenges.Where(c=>c.Kind=="military")){c.Unlocked=true;c.Count=c.Target;c.ProgressKnown=true;}foreach(var c in m4.Challenges.Where(c=>c.Kind=="special"))c.OpenConfirmed=true;var m4Goal=m4.Challenges.First(c=>c.Kind=="special");tracker.RecordTotal(m4,m4Goal,16);tracker.SetSession(tracker.CurrentWeapon,true);tracker.SetSession(m4,true);tracker.SetSession(tracker.Data.Weapons.First(w=>w.Name=="VEL 46"),true);Render();
    UpdateLayout();var image=new RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(this);
    var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var stream=File.Create(path))encoder.Save(stream);
    ToggleMini();mini!.UpdateLayout();var miniImage=new RenderTargetBitmap((int)mini.ActualWidth,(int)mini.ActualHeight,96,96,PixelFormats.Pbgra32);miniImage.Render(mini);var miniEncoder=new PngBitmapEncoder();miniEncoder.Frames.Add(BitmapFrame.Create(miniImage));using(var stream=File.Create(System.IO.Path.ChangeExtension(path,"mini.png")))miniEncoder.Save(stream);
    ToggleOverlayLock();if((GetWindowLongPtr(new WindowInteropHelper(mini).Handle,-20).ToInt64()&0x20)==0)throw new Exception("Click-through flag failed");ToggleOverlayLock();if((GetWindowLongPtr(new WindowInteropHelper(mini).Handle,-20).ToInt64()&0x20)!=0)throw new Exception("Click-through unlock failed");
    var choices=OverlayRows();SelectOverlayRow(1);if(tracker.CurrentWeapon.Id!=choices[1].weapon.Id)throw new Exception("Overlay selection failed");
    bool HasText(DependencyObject node,string value){if(node is TextBlock text&&text.Text==value)return true;for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)if(HasText(VisualTreeHelper.GetChild(node,i),value))return true;return false;}
    SelectOverlayRow(2);UpdateLayout();mini.UpdateLayout();if(!HasText(Shell,"威尔46")||!HasText(Shell,"VEL 46")||!HasText(mini,"威尔46")||!HasText(mini,"VEL 46"))throw new Exception("Chinese and English weapon names must appear in both main and HUD");if(Title!=Branding.Title||!mini.Title.StartsWith(Branding.Title))throw new Exception("Product naming mismatch");
    overlayTier="gold";var expected=OverlayRows()[5].weapon.Id;overlayPage=1;SelectOverlayRow(0);if(tracker.CurrentWeapon.Id!=expected)throw new Exception("Overlay paging failed");overlayTier="自动推荐";SelectOverlayRow(0);
    foreach(var w in tracker.Data.Weapons.Where(w=>w.Category!="模式精通"))if(WeaponArt.Image(w) is not {IsFrozen:true,PixelWidth:>0})throw new Exception("Weapon art decode failed: "+w.Name);
    CaptureHudVariants(path);CaptureMainDesigns(path);CaptureUnlockPreview(path);ShowUnlockToast(tracker.CurrentWeapon,tracker.Current,true);var toastHandle=new WindowInteropHelper(unlockToast!).Handle;if((GetWindowLongPtr(toastHandle,-20).ToInt64()&0x8000020)!=0x8000020)throw new Exception("Unlock toast must not activate or intercept input");await System.Threading.Tasks.Task.Delay(2800);if(unlockToast!=null)throw new Exception("Unlock toast did not dismiss");
    ToggleMini();var unplanned=tracker.Data.Weapons.First(w=>!tracker.InSession(w)&&w.Category!="模式精通");SelectGoal(unplanned,Recommendations.ForWeapon(tracker.Data,unplanned)!);if(tracker.CurrentWeapon.Id!=unplanned.Id)throw new Exception("Hidden HUD changed main selection");ToggleMini();if(tracker.CurrentWeapon.Id!=unplanned.Id)throw new Exception("Opening single-gun HUD must preserve selected gun");
    var savedPlan=tracker.Data.SessionWeapons.ToList();tracker.Data.SessionWeapons.Clear();Render();var emptyCount=tracker.Current.Count;var emptyUnlocked=tracker.Current.Unlocked;MarkCurrentUnlocked();if(tracker.Current.Count!=emptyCount||tracker.Current.Unlocked!=emptyUnlocked)throw new Exception("Empty HUD changed an unrelated goal");tracker.Data.SessionWeapons=savedPlan;Render();ToggleMini();
    await CaptureJourneyVariants(path);var updates=CreateUpdateWindow();updates.ShowActivated=false;updates.Show();updates.UpdateLayout();if(!HasText(updates,"资料更新")||!HasText(updates,"检查更新")||!HasText(updates,"导入离线包"))throw new Exception("Data update controls missing");var updateImage=new RenderTargetBitmap((int)updates.ActualWidth,(int)updates.ActualHeight,96,96,PixelFormats.Pbgra32);updateImage.Render(updates);SavePng(updateImage,System.IO.Path.ChangeExtension(path,"updates.png"));updates.Close();
    File.WriteAllText(path+".checks.txt","PASS: all gun images; classic and 3 new layouts render/select weapons; bilingual controls; shared HUD themes; hidden HUD selection and empty HUD guard; global progress; hotkeys/click-through; original donation; unlock toast lifecycle; update dialog controls; actual journey node selection / locked Platinum actions / single-gun menu / game-owned confirmation without level / same-gun next target\n"+hotkeyStatus);
   }catch(Exception ex){File.WriteAllText(path+".checks.txt","FAIL: "+ex);}
   Application.Current.Shutdown();
  }));
 }
}

using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
namespace CamoTracker;
public partial class MainWindow
{
 void ShowDataUpdates()=>FromOverlay(()=>CreateUpdateWindow().ShowDialog());
 Window CreateUpdateWindow()
 {
  var dialog=new Window{Title="资料更新",Width=640,Height=540,MinHeight=490,MinWidth=580,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=B(Ink),Foreground=B(White),FontFamily=FontFamily};dialog.Resources=Resources;dialog.SourceInitialized+=(_,_)=>DarkTitle(dialog);
  var root=new Grid{Margin=new(26)};root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new());root.RowDefinitions.Add(new(){Height=GridLength.Auto});
  Put(root,Stack(Label("资料更新",25,White,true),Label("CATALOG UPDATE",10,Acid),new Border{Height=18}));
  var current=Label($"当前资料  {DataCatalog.Active.Revision}   /   {DataCatalog.Active.Updated}",12,Muted);Put(root,current,0,1);
  var status=Label(DataCatalog.StartupNotice.Length>0?DataCatalog.StartupNotice:"武器、迷彩、配图与解锁条件，一起更新。",15,White,true);status.TextWrapping=TextWrapping.Wrap;
  var details=Label("点击检查获取已核实的资料包；也可导入作者发布的离线 ZIP。\n更新前自动备份，保留已解锁、计数与今日计划。",12,Muted);details.TextWrapping=TextWrapping.Wrap;details.Margin=new(0,14,0,0);
  var d=DataCatalog.Active;var numbers=Row(Label((d.Weapons.Count(w=>w.Category!="模式精通"))+" 把武器",12,Acid,true),new Border{Width=24},Label(d.Weapons.Sum(w=>w.Challenges.Count)+" 项挑战",12,White),new Border{Width=24},Label(d.CamoImages.Count+" 项迷彩配图",12,Muted));
  var info=Frame(numbers,14);info.Margin=new(0,0,0,22);var content=Stack(info,status,details);content.Margin=new(0,24,0,14);Put(root,new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto},0,2);
  var cts=new CancellationTokenSource();bool closed=false,busy=false;PreparedPack? candidate=null;
  var apply=Command("安装资料",()=>{},true);apply.IsEnabled=false;
  Button check=null!,offline=null!;
  void Busy(bool value){busy=value;check.IsEnabled=!value;offline.IsEnabled=!value;apply.IsEnabled=!value&&candidate!=null;}
  void Candidate(PreparedPack pack)
  {
   candidate?.Dispose();candidate=pack;
   if(!DataUpdates.NeedsInstall(pack.Definition.Revision,tracker)){candidate.Dispose();candidate=null;status.Text="当前已是此版本或更新版本";details.Text="无需重复安装。";apply.IsEnabled=false;return;}
   status.Text=$"可更新至 {pack.Definition.Revision} · {pack.Definition.Updated}";details.Text=DataUpdates.Preview(pack.Definition)+"\n\n已完成文件和图片校验。点击安装即可生效。";apply.IsEnabled=true;
  }
  check=Command("检查更新",()=>{});check.Click+=async(_,_)=>
  {
   Busy(true);candidate?.Dispose();candidate=null;status.Text="正在检查…";details.Text="连接项目公开的 GitHub 资料源。";
   try
   {
    var manifest=await DataUpdates.Check(cts.Token);if(closed)return;
    if(!DataUpdates.NeedsInstall(manifest.Revision,tracker)){status.Text="资料已是最新";details.Text=$"最新版本 {manifest.Revision} · {manifest.Updated}\n\n{manifest.Notes}";return;}
    status.Text="正在下载并校验资料包…";details.Text=manifest.Notes;
    var pack=await DataUpdates.Download(manifest,cts.Token);if(closed){pack.Dispose();return;}Candidate(pack);
   }
   catch(OperationCanceledException){if(!closed){status.Text="连接超时，请重试";details.Text="也可下载离线资料包后导入。";}}
   catch(Exception ex){if(!closed){status.Text="暂时无法检查更新";details.Text=ex.Message+"\n\n当前资料可继续离线使用；也可下载离线资料包后导入。";}}
   finally{if(!closed)Busy(false);}
  };
  offline=Command("导入离线包",()=>{});offline.Click+=async(_,_)=>
  {
   var file=new OpenFileDialog{Filter="资料包 (*.zip)|*.zip",Title="选择作者发布的迷彩资料包"};if(file.ShowDialog(dialog)!=true)return;
   Busy(true);status.Text="正在校验离线资料…";
   try{if(new FileInfo(file.FileName).Length>24_000_000)throw new InvalidDataException("资料包超过 24 MB。");var bytes=await File.ReadAllBytesAsync(file.FileName,cts.Token);var pack=await Task.Run(()=>DataUpdates.Prepare(bytes));if(closed){pack.Dispose();return;}Candidate(pack);}
   catch(Exception ex){if(!closed){status.Text="无法导入此资料包";details.Text=ex.Message;}}
   finally{if(!closed)Busy(false);}
  };
  apply.Click+=(_,_)=>
  {
   if(busy||candidate==null)return;
   try{var backup=DataUpdates.Apply(tracker,candidate);candidate.Dispose();candidate=null;apply.IsEnabled=false;status.Text="更新完成";details.Text="主界面、小窗和全局统计已同步。\n\n更新前存档：\n"+backup;current.Text=$"当前资料  {DataCatalog.Active.Revision}   /   {DataCatalog.Active.Updated}";Render();}
   catch(Exception ex){status.Text="未完成更新";details.Text=ex.Message;}
  };
  var sources=Command("资料来源",()=>Process.Start(new ProcessStartInfo("https://github.com/AlanHelium/mw4-camo-tracker/blob/main/docs/DATA.md"){UseShellExecute=true}));sources.Padding=new(0,9,0,0);sources.Background=System.Windows.Media.Brushes.Transparent;sources.Foreground=B(Muted);
  var footer=Stack(Row(check,offline,apply),sources);Put(root,footer,0,3);dialog.Content=root;
  dialog.Closed+=(_,_)=>{closed=true;cts.Cancel();candidate?.Dispose();};return dialog;
 }
 public async void CaptureUpdateCheck(string path)
 {
  var dialog=CreateUpdateWindow();dialog.ShowActivated=false;dialog.Show();
  IEnumerable<T> Nodes<T>(DependencyObject node) where T:DependencyObject
  {
   if(node is T match)yield return match;for(int i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(node);i++)foreach(var x in Nodes<T>(System.Windows.Media.VisualTreeHelper.GetChild(node,i)))yield return x;
  }
  try
  {
   dialog.UpdateLayout();var button=Nodes<Button>(dialog).First(b=>b.Content as string=="检查更新");button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
   var deadline=DateTime.UtcNow.AddSeconds(110);while(!button.IsEnabled&&DateTime.UtcNow<deadline)await Task.Delay(200);
   dialog.UpdateLayout();var words=string.Join("\n",Nodes<TextBlock>(dialog).Select(x=>x.Text));
   if(!button.IsEnabled||!words.Contains("资料已是最新")&&!words.Contains("可更新至"))throw new Exception(words);
   var image=new System.Windows.Media.Imaging.RenderTargetBitmap((int)dialog.ActualWidth,(int)dialog.ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);image.Render(dialog);SavePng(image,path);File.WriteAllText(path+".checks.txt","PASS: actual check button, public channel response, UI result\n"+words);
  }
  catch(Exception ex){File.WriteAllText(path+".checks.txt","FAIL: "+ex);}
  finally{dialog.Close();Application.Current.Shutdown();}
 }
}

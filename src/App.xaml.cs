using System.Windows;
namespace CamoTracker;
public partial class App : Application
{
 System.Threading.Mutex? instance;
 protected override void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);
  DispatcherUnhandledException+=(_,args)=>{System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"runtime-error.txt"),args.Exception.ToString());args.Handled=true;Shutdown(1);};
  if(e.Args.Contains("--self-test")) { Environment.Exit(ModelTests.Run()); return; }
  var export=Array.IndexOf(e.Args,"--export-data");if(export>=0&&e.Args.Length>export+1){System.IO.File.WriteAllText(e.Args[export+1],System.Text.Json.JsonSerializer.Serialize(DataCatalog.Builtin(),Tracker.JsonOptions));Shutdown();return;}
  var validate=Array.IndexOf(e.Args,"--validate-data");if(validate>=0&&e.Args.Length>validate+1){try{using var pack=DataUpdates.Prepare(System.IO.File.ReadAllBytes(e.Args[validate+1]));System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"data-results.txt"),"PASS: "+pack.Definition.Revision+"\n"+DataUpdates.Preview(pack.Definition));Shutdown();}catch(Exception ex){System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"data-results.txt"),"FAIL: "+ex);Shutdown(1);}return;}
  if(e.Args.Contains("--verify-feed")){VerifyFeed();return;}
  var updateShot=Array.IndexOf(e.Args,"--update-screenshot");var shot = Array.IndexOf(e.Args, "--screenshot");if(updateShot>=0)shot=updateShot;
  if(shot<0)
  {
   instance=new System.Threading.Mutex(true,"Local\\CamoTracker.Desktop.v1",out var fresh);
   if(!fresh){MessageBox.Show(Branding.Chinese+"已在运行。按 Ctrl+Alt+Space 可显示或隐藏窗口。",Branding.Title);Shutdown();return;}
   Exit+=(_,_)=>instance.ReleaseMutex();
   try{DataUpdates.Recover();}catch(Exception ex){MessageBox.Show(ex.Message,"资料恢复失败");Shutdown(1);return;}
   DataCatalog.Initialize();
  }
  // This PC's TSF service can deadlock WPF at startup. Disable TSF inside this
  // process only; no registry or system input settings are changed. ASCII and
  // Unicode paste remain available. --system-ime opts into normal WPF IME.
  if(!e.Args.Contains("--system-ime"))
   foreach(var assembly in new[]{typeof(System.Windows.Input.InputMethod).Assembly,typeof(Window).Assembly})
   {
    var field=assembly.GetType("MS.Internal.TextServicesLoader")?.GetField("s_servicesInstalled",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
    if(field!=null)field.SetValue(null,Enum.Parse(field.FieldType,"NotInstalled"));
   }
  System.Windows.Media.RenderOptions.ProcessRenderMode=System.Windows.Interop.RenderMode.SoftwareOnly;
  var window = new MainWindow(shot >= 0);
  MainWindow = window;
  window.Show();
  if(e.Args.Contains("--overlay"))window.OpenOverlay();
  if(shot >= 0 && e.Args.Length > shot+1){if(updateShot>=0)window.CaptureUpdateCheck(e.Args[shot+1]);else window.CaptureAndExit(e.Args[shot+1]);}
 }
 async void VerifyFeed()
 {
  try{var m=await DataUpdates.Check(CancellationToken.None);using var pack=await DataUpdates.Download(m,CancellationToken.None);System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"feed-results.txt"),"PASS: public manifest, SHA256 verified download, full image validation; revision "+m.Revision+"\n"+DataUpdates.Preview(pack.Definition));Shutdown();}
  catch(Exception ex){System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory,"feed-results.txt"),"FAIL: "+ex);Shutdown(1);}
 }
}

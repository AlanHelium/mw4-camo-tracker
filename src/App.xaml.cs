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
  var shot = Array.IndexOf(e.Args, "--screenshot");
  if(shot<0)
  {
   instance=new System.Threading.Mutex(true,"Local\\CamoTracker.Desktop.v1",out var fresh);
   if(!fresh){MessageBox.Show("迷彩行动台已在运行。按 Ctrl+Alt+Space 可显示或隐藏窗口。","CAMO");Shutdown();return;}
   Exit+=(_,_)=>instance.ReleaseMutex();
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
  if(shot >= 0 && e.Args.Length > shot+1) window.CaptureAndExit(e.Args[shot+1]);
 }
}

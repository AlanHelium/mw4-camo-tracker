using System.IO;
using System.Media;
using System.Reflection;
namespace CamoTracker;
static class UnlockAudio
{
 static SoundPlayer? player;
 public static Stream Open()=>Assembly.GetExecutingAssembly().GetManifestResourceStream("CamoTracker.unlock.wav")!;
 public static void Play()
 {
  // Load once on first use. A short static sound, not a running audio engine.
  try{if(player==null){player=new SoundPlayer(Open());player.Load();}player.Play();}catch{}
 }
}

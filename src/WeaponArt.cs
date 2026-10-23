using System.Reflection;
using System.Text.Json;
using System.Windows.Media.Imaging;
namespace CamoTracker;
static class WeaponArt
{
 public sealed class Entry
 {
  public string CatalogKey {get;set;}="";
  public string EnglishName {get;set;}="";
  public string ChineseName {get;set;}="";
  public string Image {get;set;}="";
  public int MaxLevel {get;set;}
 }
 static readonly Dictionary<string,Entry> entries=Read();
 static readonly Dictionary<string,BitmapSource> cache=[];
 static Dictionary<string,Entry> Read()
 {
  using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("CamoTracker.weapons.json")!;
  return JsonSerializer.Deserialize<List<Entry>>(stream)!.ToDictionary(e=>e.CatalogKey);
 }
 public static Entry? Info(Weapon w)=>entries.GetValueOrDefault(w.CatalogKey);
 public static BitmapSource? Image(Weapon w)
 {
  var info=Info(w);if(info==null)return null;
  if(cache.TryGetValue(info.Image,out var found))return found;
  var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.DecodePixelWidth=720;image.UriSource=new Uri("pack://application:,,,/Assets/Weapons/"+info.Image);image.EndInit();image.Freeze();
  // Bound decoded gun art instead of retaining all 33 large images.
  if(cache.Count>=8)cache.Clear();cache[info.Image]=image;return image;
 }
}

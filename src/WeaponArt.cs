using System.Windows.Media.Imaging;
namespace CamoTracker;
public static class WeaponArt
{
 public sealed class Entry
 {
  public string CatalogKey {get;set;}="";
  public string EnglishName {get;set;}="";
  public string ChineseName {get;set;}="";
  public string Image {get;set;}="";
  public int MaxLevel {get;set;}
  public string Source {get;set;}="";
  public string ChineseSource {get;set;}="";
 }
 static readonly Dictionary<string,BitmapSource> cache=[];
 public static void Clear()=>cache.Clear();
 public static Entry? Info(Weapon w)=>DataCatalog.Active.WeaponImages.FirstOrDefault(e=>e.CatalogKey==w.CatalogKey);
 public static BitmapSource? Image(Weapon w)
 {
  var info=Info(w);if(info==null)return null;
  if(cache.TryGetValue(info.Image,out var found))return found;
  var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.DecodePixelWidth=720;image.UriSource=DataCatalog.Root==null?new Uri("pack://application:,,,/Assets/Weapons/"+info.Image):new Uri(DataCatalog.Asset("weapons/"+info.Image));image.EndInit();image.Freeze();
  // Bound decoded gun art instead of retaining all 33 large images.
  if(cache.Count>=8)cache.Clear();cache[info.Image]=image;return image;
 }
}

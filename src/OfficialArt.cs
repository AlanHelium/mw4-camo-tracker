using System.Windows;
using System.Windows.Media.Imaging;
namespace CamoTracker;
static class OfficialArt
{
 static readonly Dictionary<string,BitmapSource> cache=[];
 static readonly Dictionary<string,BitmapSource> sources=[];
 public static string Key(Challenge c)
 {
  if(!string.IsNullOrEmpty(c.ArtKey))return c.ArtKey;
  if(c.Kind=="military"&&c.CatalogKey.StartsWith("military-"))return c.CatalogKey;
  if(c.Source!=Tracker.Official)return "";
  return c.Name.Split('·')[0].Trim().ToLowerInvariant() switch {"smoke"=>"smoke","stormwave"=>"stormwave","red"=>"red","gold"=>"gold","platinum"=>"platinum","harmonic break"=>"harmonic",_=>""};
 }
 public static BitmapSource? Image(string key,bool detail=false)
 {
  var asset=DataCatalog.Active.CamoImages.FirstOrDefault(x=>x.Key==key);if(asset==null)return null;
  var cacheKey=key+(detail?"-detail":"");if(cache.TryGetValue(cacheKey,out var found))return found;
  var sourceKey=asset.Image+(detail?"-detail":"");
  if(!sources.TryGetValue(sourceKey,out var decoded))
  {
   var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.DecodePixelWidth=detail?1280:1920;
   bitmap.UriSource=DataCatalog.Root==null?new Uri("pack://application:,,,/Assets/"+System.IO.Path.GetFileName(asset.Image)):new Uri(DataCatalog.Asset(asset.Image));bitmap.EndInit();bitmap.Freeze();decoded=bitmap;if(sources.Count>=4)sources.Clear();sources[sourceKey]=decoded;
  }
  BitmapSource result=decoded;
  if(!detail&&asset.Crop.Length==4){double scale=decoded.PixelWidth/1920.0;var r=asset.Crop;result=new CroppedBitmap(decoded,new Int32Rect((int)(r[0]*scale),(int)(r[1]*scale),Math.Max(1,(int)(r[2]*scale)),Math.Max(1,(int)(r[3]*scale))));}
  result.Freeze();if(cache.Count>=8)cache.Clear();cache[cacheKey]=result;return result;
 }
 public static string Caption(string key)=>DataCatalog.Active.CamoImages.FirstOrDefault(x=>x.Key==key)?.Caption??"迷彩外观参考";
 public static string Source(string key)=>DataCatalog.Active.CamoImages.FirstOrDefault(x=>x.Key==key)?.Source??"";
 public static void Clear(){cache.Clear();sources.Clear();}
}

using System.Windows;
using System.Windows.Media.Imaging;
namespace CamoTracker;
static class OfficialArt
{
 static readonly Dictionary<string,BitmapSource> cache=[];
 public static string Key(Challenge c)
 {
  if(c.Kind=="military"&&c.CatalogKey.StartsWith("military-"))return c.CatalogKey;
  if(!string.IsNullOrEmpty(c.ArtKey))return c.ArtKey;
  if(c.Source!=Tracker.Official)return "";
  return c.Name.Split('·')[0].Trim().ToLowerInvariant() switch {"smoke"=>"smoke","stormwave"=>"stormwave","red"=>"red","gold"=>"gold","platinum"=>"platinum","harmonic break"=>"harmonic",_=>""};
 }
 public static BitmapSource? Image(string key,bool detail=false)
 {
  bool military=key.StartsWith("military-")&&int.TryParse(key.Split('-').Last(),out var militaryNumber)&&militaryNumber is >=1 and <=8;
  if(!military&&!new[]{"smoke","stormwave","red","gold","platinum","harmonic","pyroclast","terminus","scatter","cascade"}.Contains(key))return null;
  var cacheKey=key;if(!detail&&cache.TryGetValue(cacheKey,out var existing))return existing;
  var file=military?"military":key is "smoke" or "stormwave" or "red"?"special":key;
  var sourceKey="source-"+file;BitmapSource bitmap;
  if(!detail&&cache.TryGetValue(sourceKey,out var shared))bitmap=shared;
  else{var decoded=new BitmapImage();decoded.BeginInit();decoded.CacheOption=BitmapCacheOption.OnLoad;decoded.DecodePixelWidth=detail?1280:file is "special" or "military"?1920:960;decoded.UriSource=new Uri("pack://application:,,,/Assets/"+file+".png");decoded.EndInit();decoded.Freeze();bitmap=decoded;if(!detail)cache[sourceKey]=bitmap;}
  BitmapSource result=bitmap;
  if(!detail)
  {
   var crop=key switch {"smoke"=>new Int32Rect(288,108,425,400),"stormwave"=>new Int32Rect(745,108,425,400),"red"=>new Int32Rect(1203,108,425,400),_=>new Int32Rect(73,110,880,395)};
   if(military){int n=int.Parse(key.Split('-').Last())-1;crop=new Int32Rect(72+(n%4)*458,n<4?108:598,423,400);}
   double scale=bitmap.PixelWidth/1920.0;crop=new Int32Rect((int)(crop.X*scale),(int)(crop.Y*scale),(int)(crop.Width*scale),(int)(crop.Height*scale));result=new CroppedBitmap(bitmap,crop);
  }
  result.Freeze();if(!detail)cache[cacheKey]=result;return result;
 }
}

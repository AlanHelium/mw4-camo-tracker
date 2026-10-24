using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
namespace CamoTracker;

sealed class UpdateManifest
{
 public List<UpdateFile> Files {get;set;}=[];
 public int Schema {get;set;}=1;
 public long Revision {get;set;}
 public string Updated {get;set;}="";
 public string MinimumApp {get;set;}="0.7.0";
 public string Notes {get;set;}="";
 public string Blob {get;set;}="";
 public string Sha256 {get;set;}="";
 public long Bytes {get;set;}
}
sealed class UpdateFile {public string Path {get;set;}="";public string Sha256 {get;set;}="";public int Bytes {get;set;}}
sealed class PreparedPack : IDisposable
{
 public string Root {get;}
 public CatalogDefinition Definition {get;}
 public string Hash {get;}
 public PreparedPack(string root,CatalogDefinition definition,string hash){Root=root;Definition=definition;Hash=hash;}
 public void Dispose(){if(Directory.Exists(Root))Directory.Delete(Root,true);}
}
static class DataUpdates
{
 const string Api="https://api.github.com/repos/AlanHelium/mw4-camo-tracker/";
 const string Cdn="https://cdn.jsdelivr.net/gh/AlanHelium/mw4-camo-tracker@";
 const int MaxZip=24_000_000,MaxDownload=36_000_000;
 static readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(90)};
 static DataUpdates(){http.DefaultRequestHeaders.UserAgent.ParseAdd("MW4-Camo-Tracker/0.7.0");}
 public static void Compatible(int schema,string minimum)
 {
  if(schema!=1||!Version.TryParse(minimum,out var version)||version>typeof(DataUpdates).Assembly.GetName().Version)throw new InvalidDataException("此资料包需要新版 APP，请先更新程序。");
 }
 public static void Validate(CatalogDefinition d)
 {
  Compatible(d.Schema,d.MinimumApp);
  if(d.Revision is <2026102400 or >99999999999999||d.Updated.Length>40||d.Notes.Length>6000||d.Weapons.Count is <1 or >190||d.WeaponImages.Count>190||d.CamoImages.Count>1000)throw new InvalidDataException("资料包内容或版本无效。");
  // Use the same bounds as user saves, then validate stable keys and declarative rules.
  Tracker.Parse(JsonSerializer.Serialize(new TrackerData{Weapons=d.Weapons}));
  bool Key(string key)=>key!=null&&Regex.IsMatch(key,@"^[a-z0-9][a-z0-9-]{0,99}$");
  if(d.Weapons.Any(w=>!Key(w.CatalogKey))||d.Weapons.Select(w=>w.CatalogKey).Distinct().Count()!=d.Weapons.Count)throw new InvalidDataException("武器标识重复或无效。");
  var kinds=new[]{"military","special","gold","platinum","harmonic","divergence","prestige","mastery-mp","mastery-dmz","mastery-wz","custom"};
  foreach(var w in d.Weapons)
  {
   if(w.Category.Length>80||w.Mode.Length>80||w.ChineseName.Length>100||w.EnglishName.Length>100||w.MaxLevel is <0 or >1000||w.Challenges.Select(x=>x.CatalogKey).Distinct().Count()!=w.Challenges.Count)throw new InvalidDataException("武器目录无效。");
   foreach(var c in w.Challenges)
   {
    if(!Key(c.CatalogKey)||!kinds.Contains(c.Kind)||c.EnglishName.Length>150||c.ChineseName.Length>150||c.SourceRule.Length>2000||c.DmzRule.Length>2000||c.DmzSourceRule.Length>2000||c.Stage.Length>100||c.Unit.Length>30||c.ReferenceRule.Length>2000||c.GameRewardOnly&&c.Kind!="military"||c.ProgressRevision is <1 or >10000||c.RequiredPrestige is <0 or >2||c.RequiredLevel is <0 or >1000||c.RequiredWeaponLevel is <0 or >1000||c.Gates.Count>20||c.Gates.Any(g=>!new[]{"self","category","all"}.Contains(g.Scope)||g.Count is <0 or >10000||g.Label.Length>100||(g.ChallengeKey.Length==0?!kinds.Contains(g.Kind):!Key(g.ChallengeKey))))throw new InvalidDataException("挑战条件需要新版程序或内容无效。");
    if(c.Count!=0||c.ProgressKnown||c.Unlocked||c.OpenConfirmed)throw new InvalidDataException("资料包不能包含玩家进度。");
   }
  }
  foreach(var a in d.WeaponImages)if(!d.Weapons.Any(w=>w.CatalogKey==a.CatalogKey)||a.ChineseName.Length is <1 or >100||a.EnglishName.Length is <1 or >100||a.MaxLevel is <1 or >1000||!SafeAsset("weapons/"+a.Image))throw new InvalidDataException("武器图片资料无效。");
  if(d.WeaponImages.Select(x=>x.CatalogKey).Distinct().Count()!=d.WeaponImages.Count||d.CamoImages.Select(x=>x.Key).Distinct().Count()!=d.CamoImages.Count)throw new InvalidDataException("图片标识重复。");
  foreach(var a in d.CamoImages)if(!Key(a.Key)||!SafeAsset(a.Image)||a.Caption.Length>150||a.Source.Length>2000||a.Crop.Length!=0&&(a.Crop.Length!=4||a.Crop[0]<0||a.Crop[1]<0||a.Crop[2]<=0||a.Crop[3]<=0||a.Crop.Any(x=>x>4096)))throw new InvalidDataException("迷彩图片资料无效。");
 }
 static bool SafeAsset(string path)=>Regex.IsMatch(path??"",@"^(weapons|camos)/[a-z0-9][a-z0-9-]*\.(png|jpg)$");
 static async Task<byte[]> Get(string url,int max,CancellationToken token)
 {
  using var response=await http.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,token);response.EnsureSuccessStatusCode();
  if(response.Content.Headers.ContentLength>max)throw new InvalidDataException("下载超过大小限制。");
  await using var stream=await response.Content.ReadAsStreamAsync(token);using var output=new MemoryStream();var buffer=new byte[65536];int n;
  while((n=await stream.ReadAsync(buffer,token))>0){if(output.Length+n>max)throw new InvalidDataException("下载超过大小限制。");output.Write(buffer,0,n);}return output.ToArray();
 }
 public static async Task<UpdateManifest> Check(CancellationToken token)
 {
  byte[] content;
  try{var bytes=await Get(Api+"contents/data/channel.json?ref=main",64000,token);using var json=JsonDocument.Parse(bytes);content=Convert.FromBase64String(json.RootElement.GetProperty("content").GetString()!);}
  catch(Exception ex) when(!token.IsCancellationRequested&&ex is HttpRequestException or TaskCanceledException){content=await Get(Cdn+"main/data/channel.json",64000,token);}
  var m=JsonSerializer.Deserialize<UpdateManifest>(content)!;
  // An edge cache may still have the original manifest for this exact pack.
  // Only fill its file list when both the revision and ZIP digest match the app's baseline.
  if(m.Files.Count==0){using var embedded=typeof(DataUpdates).Assembly.GetManifestResourceStream("CamoTracker.channel.json")!;var local=JsonSerializer.Deserialize<UpdateManifest>(embedded)!;if(local.Revision==m.Revision&&local.Sha256==m.Sha256&&local.Blob==m.Blob)m=local;}
  Compatible(m.Schema,m.MinimumApp);
  if(!Regex.IsMatch(m.Blob??"",@"^[a-f0-9]{40}$")||!Regex.IsMatch(m.Sha256??"",@"^[a-f0-9]{64}$")||m.Bytes is <1 or >MaxZip||m.Revision<2026102400||m.Notes.Length>6000||m.Files.Count>1200||m.Files.Sum(x=>(long)x.Bytes)>64_000_000||m.Files.Select(x=>x.Path).Distinct().Count()!=m.Files.Count||m.Files.Any(x=>(x.Path!="catalog.json"&&!SafeAsset(x.Path))||x.Bytes is <1 or >8_000_000||!Regex.IsMatch(x.Sha256??"",@"^[a-f0-9]{64}$")))throw new InvalidDataException("更新清单无效。");return m;
 }
 public static async Task<PreparedPack> Download(UpdateManifest m,CancellationToken token)
 {
  byte[] zip;
  try{var bytes=await Get(Api+"git/blobs/"+m.Blob,MaxDownload,token);zip=await Task.Run(()=>{using var json=JsonDocument.Parse(bytes);return Convert.FromBase64String(json.RootElement.GetProperty("content").GetString()!);},token);}
  catch(Exception ex) when(!token.IsCancellationRequested&&ex is HttpRequestException or TaskCanceledException){return await DownloadMirror(m,token);}
  var result=await Task.Run(()=>{if(zip.Length!=m.Bytes||!Convert.ToHexStringLower(SHA256.HashData(zip)).Equals(m.Sha256,StringComparison.Ordinal))throw new InvalidDataException("下载校验失败，请重新检查更新。");return Prepare(zip);},token);
  if(result.Definition.Revision!=m.Revision){result.Dispose();throw new InvalidDataException("资料包与更新清单不一致。");}return result;
 }
 static async Task<PreparedPack> DownloadMirror(UpdateManifest m,CancellationToken token)
 {
  if(m.Files.Count<2)throw new InvalidDataException("镜像清单暂未同步，请稍后重试或导入离线包。");
  using var cancel=CancellationTokenSource.CreateLinkedTokenSource(token);using var slots=new SemaphoreSlim(3);
  var tasks=m.Files.Select(async file=>
  {
   await slots.WaitAsync(cancel.Token);try
   {
    string repoPath=file.Path=="catalog.json"?"data/catalog.json":file.Path.StartsWith("weapons/")?"src/Assets/Weapons/"+System.IO.Path.GetFileName(file.Path):"src/Assets/"+System.IO.Path.GetFileName(file.Path);
    var bytes=await Get(Cdn+"data-"+m.Revision+"/"+repoPath,file.Bytes,cancel.Token);
    if(bytes.Length!=file.Bytes||Convert.ToHexStringLower(SHA256.HashData(bytes))!=file.Sha256)throw new InvalidDataException("镜像文件校验失败，请重新检查更新。");return (file.Path,bytes);
   }
   catch{cancel.Cancel();throw;}finally{slots.Release();}
  }).ToList();
  var files=await Task.WhenAll(tasks);
  var result=await Task.Run(()=>
  {
   using var memory=new MemoryStream();using(var zip=new ZipArchive(memory,ZipArchiveMode.Create,true))foreach(var (path,bytes) in files){using var entry=zip.CreateEntry(path,CompressionLevel.Optimal).Open();entry.Write(bytes);}
   return Prepare(memory.ToArray());
  },token);
  if(result.Definition.Revision!=m.Revision){result.Dispose();throw new InvalidDataException("镜像资料版本不一致。");}return result;
 }
 public static PreparedPack Prepare(byte[] zip)
 {
  if(zip.Length>MaxZip)throw new InvalidDataException("资料包超过 24 MB。");
  var root=Path.Combine(Path.GetTempPath(),"camo-data-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  try
  {
   using var archive=new ZipArchive(new MemoryStream(zip),ZipArchiveMode.Read);if(archive.Entries.Count is <2 or >1200)throw new InvalidDataException("资料包文件数量无效。");
   long total=0;var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach(var e in archive.Entries)
   {
    if(!names.Add(e.FullName)||(e.FullName!="catalog.json"&&!SafeAsset(e.FullName))||e.Length>8_000_000||(total+=e.Length)>64_000_000)throw new InvalidDataException("资料包含有不支持的文件或路径。");
    var path=Path.Combine(root,e.FullName);Directory.CreateDirectory(Path.GetDirectoryName(path)!);using var source=e.Open();using var file=File.Create(path);var buffer=new byte[65536];long written=0;int read;while((read=source.Read(buffer))>0){written+=read;if(written>e.Length||written>8_000_000)throw new InvalidDataException("解压大小异常。");file.Write(buffer,0,read);}if(written!=e.Length)throw new InvalidDataException("资料包文件不完整。");
   }
   var json=Path.Combine(root,"catalog.json");if(!File.Exists(json)||new FileInfo(json).Length>4_000_000)throw new InvalidDataException("缺少资料目录。");
   var d=JsonSerializer.Deserialize<CatalogDefinition>(File.ReadAllText(json))!;Validate(d);
   var files=d.WeaponImages.Select(x=>"weapons/"+x.Image).Concat(d.CamoImages.Select(x=>x.Image)).Distinct().ToList();
   if(names.Count!=files.Count+1)throw new InvalidDataException("资料包包含未登记文件。");
   foreach(var file in files)
   {
    using var stream=File.OpenRead(Path.Combine(root,file));var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);var frame=decoder.Frames[0];
    if(decoder.Frames.Count!=1||frame.PixelWidth is <1 or >4096||frame.PixelHeight is <1 or >4096)throw new InvalidDataException("图片尺寸无效。");
    foreach(var art in d.CamoImages.Where(x=>x.Image==file&&x.Crop.Length==4)){double scale=frame.PixelWidth/1920.0;if((art.Crop[0]+art.Crop[2])*scale>frame.PixelWidth||(art.Crop[1]+art.Crop[3])*scale>frame.PixelHeight)throw new InvalidDataException("迷彩裁切范围超出图片。");}
   }
   return new(root,d,Convert.ToHexStringLower(SHA256.HashData(zip)));
  }
  catch{Directory.Delete(root,true);throw;}
 }
 public static string Preview(CatalogDefinition d)
 {
  var before=DataCatalog.Active;int guns=d.Weapons.Count(w=>!before.Weapons.Any(x=>x.CatalogKey==w.CatalogKey));
  int goals=d.Weapons.Sum(w=>w.Challenges.Count(c=>!before.Weapons.Any(x=>x.CatalogKey==w.CatalogKey&&x.Challenges.Any(y=>y.CatalogKey==c.CatalogKey))));
  int changes=d.Weapons.Sum(w=>w.Challenges.Count(c=>before.Weapons.FirstOrDefault(x=>x.CatalogKey==w.CatalogKey)?.Challenges.FirstOrDefault(y=>y.CatalogKey==c.CatalogKey) is {} old&&(old.Rule!=c.Rule||old.Target!=c.Target||old.SourceRule!=c.SourceRule||JsonSerializer.Serialize(old.Gates)!=JsonSerializer.Serialize(c.Gates)||old.RequiredWeaponLevel!=c.RequiredWeaponLevel||old.RequiredPrestige!=c.RequiredPrestige||old.RequiredLevel!=c.RequiredLevel||old.RequiresPrestigeMax!=c.RequiresPrestigeMax||old.ProgressRevision!=c.ProgressRevision)));
  return $"新增 {guns} 把武器 · 新增 {goals} 项挑战 · 调整 {changes} 项条件\n包含 {d.WeaponImages.Count} 张枪图、{d.CamoImages.Count} 项迷彩配图\n\n{d.Notes}";
 }
 public static bool NeedsInstall(long revision,Tracker tracker)=>revision>=tracker.Data.DataRevision&&(revision>DataCatalog.Active.Revision||revision==DataCatalog.Active.Revision&&DataCatalog.StartupNotice.Length>0);
 static void Atomic(string path,string text){File.WriteAllText(path+".tmp",text);File.Move(path+".tmp",path,true);}
 public static string Apply(Tracker tracker,PreparedPack pack)
 {
  if(tracker.Path==null)throw new InvalidOperationException("预览模式不写入资料。");
  if(!NeedsInstall(pack.Definition.Revision,tracker))throw new InvalidDataException("当前资料已是此版本或更新版本，无需重复安装。");
  var next=DataCatalog.Clone(tracker.Data);Catalog.Merge(next,pack.Definition);Tracker.Parse(JsonSerializer.Serialize(next));
  Directory.CreateDirectory(DataCatalog.Home);var pointer=Path.Combine(DataCatalog.Home,"current.json");var journal=Path.Combine(DataCatalog.Home,"transaction.json");
  var backup=tracker.Path+".before-data-"+DateTime.Now.ToString("yyyyMMddHHmmssfff");
  File.WriteAllText(backup,JsonSerializer.Serialize(tracker.Data,Tracker.JsonOptions));
  string folder=pack.Definition.Revision+"-"+pack.Hash[..12]+"-"+Guid.NewGuid().ToString("N")[..8],root=Path.Combine(DataCatalog.Home,folder);
  Directory.Move(pack.Root,root);
  var transaction=new UpdateTransaction{Progress=tracker.Path,Backup=backup,PreviousPointer=File.Exists(pointer)?File.ReadAllText(pointer):null};
  Atomic(journal,JsonSerializer.Serialize(transaction));
  try
  {
   Atomic(tracker.Path,JsonSerializer.Serialize(next,Tracker.JsonOptions));Atomic(pointer,JsonSerializer.Serialize(folder));File.Delete(journal);
  }
  catch{Recover();throw;}
  tracker.AcceptUpdate(next);DataCatalog.Set(pack.Definition,root);return backup;
 }
 sealed class UpdateTransaction{public string Progress {get;set;}="";public string Backup {get;set;}="";public string? PreviousPointer {get;set;}}
 public static void Recover()
 {
  var journal=Path.Combine(DataCatalog.Home,"transaction.json");if(!File.Exists(journal))return;
  var t=JsonSerializer.Deserialize<UpdateTransaction>(File.ReadAllText(journal))!;
  var progress=Path.Combine(Path.GetDirectoryName(DataCatalog.Home)!,"progress.json");
  if(t.Progress!=progress||!t.Backup.StartsWith(progress+".before-data-",StringComparison.Ordinal)||!File.Exists(t.Backup))throw new InvalidDataException("更新恢复记录无效，请保留存档并重新打开。");
  Atomic(progress,File.ReadAllText(t.Backup));var pointer=Path.Combine(DataCatalog.Home,"current.json");if(t.PreviousPointer==null)File.Delete(pointer);else Atomic(pointer,t.PreviousPointer);File.Delete(journal);
 }
}

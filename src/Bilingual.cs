namespace CamoTracker;
static class Bilingual
{
 // Reference translations, not a claim of official Simplified Chinese naming.
 static readonly Dictionary<string,string> camos="""
Splinter Rock|碎裂岩石
Sunset Tropic|热带落日
Cyan|青色
Smoke|烟雾
Stormwave|风暴波纹
Red|红色
Myriad|万象
Night Ops|夜间行动
Green|绿色
Ponderosa|黄松
Red Rain|红雨
Kitty Kaos|混乱猫咪
Samnim|森林
Raster Aux|辅助光栅
Yellow|黄色
Dune Sea|沙丘之海
Scorch|焦灼
Disperse|散布
Dark Moss|暗苔
Shale|页岩
Hot Rod|热力改装
Oni|鬼面
Urban Smoke|都市烟雾
Optical Reverb|光学回响
Autumn|秋日
Snakeskin|蛇皮
Space Time|时空
Off Grid|离网
Urban Flamingo|都市火烈鸟
Smokescreen|烟幕
Frosted|霜覆
Splatter Ops|泼彩行动
Red Tiger|红虎
Miami|迈阿密
Wallpaper|壁纸
Garland|花环
Stellation|星芒
Rattlesnake|响尾蛇
Dark Tan|深棕
Asphalt|沥青
Black Topo|黑色等高线
Brown|棕色
Blackwell|黑井
Iron|铁色
Currents|涌流
Sniper Veil|狙击面纱
Carbonated|碳酸气泡
Lime|青柠
Jungle Shadow|丛林暗影
Ghast|幽魂
Gunmetal|枪铁灰
Struck Oil|石油涌现
Brushstroke|笔触
White Pine|白松
Redshift|红移
Corrupted Clay|侵蚀黏土
Magma Ire|熔岩之怒
Desert Tactics|沙漠战术
Green Tiger|绿虎
Thunderhead|雷云
Ash Pine|灰松
Outcrop|岩层露头
Foxtrail|狐踪
Phantom Flux|幻影流变
Mineral|矿物
Tiger Swipe|虎爪
Mangrove|红树林
Fungal Grid|菌丝网格
White|白色
Hibiscus|木槿
Fire Topo|烈焰等高线
Deep Space|深空
Ice Sheer|冰幕
Fractal Snow|分形雪花
Sticker Shock|贴纸冲击
Treeline|林线
Spidersilk|蛛丝
Tension|张力
Anaconda|森蚺
Ripplewave|涟漪波
Sunburn|日灼
Dry Brush|干刷
Backcountry|荒野
Dread Plague|恐惧瘟疫
Frontier|边境
Sandwash|沙洗
Digital Fall|数码秋色
Digi Bark|数码树皮
Stone Bits|碎石
Bubblegum Ops|泡泡糖行动
Thicket|灌木丛
Tectonic|地壳运动
Blue Tiger|蓝虎
Night Snow|夜雪
Recursive|递归
Urban Standard|都市制式
Raster Lite|轻量光栅
Stockpile|储备
Deathbed|终末
""".Split('\n',StringSplitOptions.RemoveEmptyEntries).Select(line=>line.Trim().Split('|')).ToDictionary(parts=>parts[0],parts=>parts[1]);
 static readonly Dictionary<string,string> globalNames=new(){["divergence"]="Divergence",["mp-1"]="Mercurial Drift",["mp-2"]="Polyatomic Reforged",["mp-3"]="Orion Reforged",["dmz-1"]="Chiral",["dmz-2"]="Ripple Tide",["dmz-3"]="Helio",["wz-1"]="Parallax",["wz-2"]="Damascus Reforged",["wz-3"]="Empyros"};
 public static string WeaponEn(Weapon w)=>w.EnglishName.Length>0?w.EnglishName:WeaponArt.Info(w)?.EnglishName??w.Name;
 public static string WeaponZh(Weapon w)=>w.ChineseName.Length>0?w.ChineseName:WeaponArt.Info(w)?.ChineseName??w.Name;
 public static string WeaponPair(Weapon w){var cn=WeaponZh(w);var en=WeaponEn(w);return cn==en?cn:cn+" · "+en;}
 public static string CamoEn(Challenge c)=>c.EnglishName.Length>0?c.EnglishName:c.Kind switch
 {
  "military"=>"Military Camo "+c.CatalogKey.Split('-').Last(),
  "gold"=>"Gold","platinum"=>"Platinum","harmonic"=>"Harmonic Break",
  "prestige"=>c.ArtKey switch {"pyroclast"=>"Pyroclast","terminus"=>"Pyroclast Terminus","scatter"=>"Pyroclast Scatter","cascade"=>"Pyroclast Cascade",_=>c.Name},
  _=>globalNames.GetValueOrDefault(c.CatalogKey,c.Name)
 };
 public static string CamoZh(Challenge c)=>c.ChineseName.Length>0?c.ChineseName:camos.GetValueOrDefault(c.Name,c.Name);
 public static string Camo(Challenge c){var zh=CamoZh(c);var en=CamoEn(c);return zh==en?zh:zh+" · "+en;}
 public static string Stage(string stage)=>stage switch {"军事迷彩"=>"军事迷彩 / Military","特殊迷彩"=>"特殊迷彩 / Special","完美主义者"=>"完成主义 / Completionist","武器巅峰"=>"武器巅峰 / Weapon Prestige","模式精通"=>"模式精通 / Mode Mastery",_=>stage};
 public static string RuleEn(Challenge c)
 {
  if(c.Source.StartsWith("用户"))return "";
  if(!string.IsNullOrWhiteSpace(c.SourceRule))return c.SourceRule;
  if(c.Kind=="military")return $"Reach weapon level {c.RequiredWeaponLevel}.";
  if(c.Kind=="prestige")return c.RequiresPrestigeMax?"Reach Weapon Prestige 1 Max Level.":$"Reach {(c.RequiredPrestige==1?"Weapon Prestige 1":"Weapon Prestige Master")} Level {c.RequiredLevel}.";
  return c.CatalogKey switch
  {
   "divergence"=>"Complete 33 Harmonic Break camo challenges.",
   "mp-1"=>"Get 10 kills in a Multiplayer match, 250 times.",
   "mp-2"=>"Get 3 kills without taking damage in Multiplayer, 300 times.",
   "mp-3"=>"Get 5 kills without dying in Multiplayer, 500 times.",
   "dmz-1"=>"Get 2,500 kills against Tier 3 or higher enemies in DMZ.",
   "dmz-2"=>"Extract 2 Lieutenant dog tags in one DMZ deployment, 50 times.",
   "dmz-3"=>"Get 25,000 enemy kills with a Legendary weapon as a max-level Operator in DMZ.",
   "wz-1" or "wz-2" or "wz-3"=>"Warzone challenge requirements have not been confirmed.",_=>""
  };
 }
 public static string Rule(Challenge c)=>c.Rule+(RuleEn(c) is {Length:>0} en?"\n"+en:"");
}

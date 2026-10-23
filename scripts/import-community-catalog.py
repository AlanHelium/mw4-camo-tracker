"""Extract public challenge facts, never interface code or third-party artwork.
Requires beautifulsoup4. Source attribution is retained on every challenge.
"""
import json, re, sys, urllib.request
from pathlib import Path
try:
    from bs4 import BeautifulSoup
except ImportError:
    sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools/python-libs'))
    from bs4 import BeautifulSoup

ROOT = Path(__file__).resolve().parents[1]
SOURCE = 'https://upblazed.com/tools/mw4-camo-tracker'
OFFICIAL = 'https://www.callofduty.com/ca/en/blog/2026/10/call-of-duty-modern-warfare-4-ready-for-launch-progression-prestige'
local = ROOT / 'tools/upblazed-source.html'
html = local.read_text(encoding='utf-8') if local.exists() else urllib.request.urlopen(SOURCE).read().decode('utf-8')
soup = BeautifulSoup(html, 'html.parser')
categories = {'Assault Rifles':'突击步枪','Pistols':'手枪','SMGs':'冲锋枪','Shotguns':'霰弹枪','LMGs':'轻机枪','Marksman Rifles':'射手步枪','Sniper Rifles':'狙击步枪','Launchers':'发射器','Melee':'近战武器'}

def slug(name): return re.sub(r'[^a-z0-9]+','-',name.lower()).strip('-')
def translate(raw):
    if not raw or 'not shown' in raw.lower(): return 1, '条件尚未公开，请按游戏内文字核对。', True
    text = raw.lower()
    times = re.search(r'(\d+)\s+times', text)
    nums = re.findall(r'\d+', text)
    target = int(times.group(1)) if times else int(nums[0]) if nums else 1
    condition = next((cn for en,cn in [
      ('without reloading','不换弹连续击杀'),('without taking damage','无伤连续击杀'),('without dying','不死亡连续击杀'),
      ('within 5 seconds','5 秒内连续击杀'),('miri 40mm','使用 Miri 40MM 下挂榴弹击杀'),('ledge hanging','悬挂时击杀'),
      ('direct shot','直接命中击杀'),('killstreaks or vehicles','摧毁连杀奖励或载具'),('quickscope','快速开镜击杀'),
      ('longshot','远距离击杀'),('point blank','贴身击杀'),('mounted','架枪击杀'),('silencer','装消音器击杀'),
      ('double kills','达成双杀'),('headshot','爆头击杀'),('hipfire','腰射击杀'),('from behind','背身击杀'),
      ('prone','趴下击杀'),('crouched','蹲下击杀'),('jumping','跳跃时击杀'),('moving','移动时击杀'),
      ('full auto','使用全自动模式击杀'),('aiming down sights','开镜瞄准击杀'),('while ads','开镜瞄准击杀'),
      ('akimbo','双持击杀'),('single match','单局击杀'),('one shot','一击毙命'),('without releasing','不松开扳机连续击杀')
    ] if en in text), None)
    if condition is None: return target, raw, False
    if times and nums: return target, f'{condition} {nums[0]} 人，完成 {target} 次。', False
    return target, f'{condition} {target} 次。', False

weapons=[]
for row in soup.select('.camo-row'):
    name_cell=row.select_one('[style="grid-area:name"]')
    parts=list(name_cell.stripped_strings)
    name=parts[0]; category=categories.get(parts[1],parts[1])
    if name.lower()=='han 86': name='HAN 86'
    w={'Id':slug(name),'Name':name,'Category':category,'Mode':'多人模式','CatalogKey':slug(name),'Challenges':[]}
    seen=set(); special_index=0
    for hover in row.select('.camo-hov'):
        b=hover.select_one('button');tip=hover.select_one('[role="tooltip"]')
        if not b or not tip:continue
        label=b.get('aria-label','')
        camo=label.split(': ',1)[-1].rsplit(', ',1)[0]
        if camo in seen:continue
        seen.add(camo)
        text=tip.get_text(' ',strip=True)
        military=re.search(r'Military camo (\d+) \(weapon level (\d+)\)',camo)
        if military:
            index,level=map(int,military.groups())
            w['Challenges'].append({'Id':slug(name)+':military-'+str(index),'CatalogKey':'military-'+str(index),'Name':f'军事迷彩 {index}','Stage':'军事迷彩','Kind':'military','Target':level,'RequiredWeaponLevel':level,'Rule':f'武器达到 {level} 级。','Source':OFFICIAL if name=='HAN 86' else SOURCE})
            continue
        if camo in ['Gold','Platinum','Harmonic Break']:kind={'Gold':'gold','Platinum':'platinum','Harmonic Break':'harmonic'}[camo]
        else:
            special_index+=1;kind='special'
        match=re.search(r'MP\s*:\s*(.*?)\s*DMZ\s*:\s*(.*?)(?:Opens once|Open\.|Locked until|$)',text)
        if not match:raise ValueError((name,camo,text))
        mp,dmz=[x.strip() for x in match.groups()]
        target,rule,unknown=translate(mp);dmz_target,dmz_rule,dmz_unknown=translate(dmz)
        unlock=re.search(r'from level (\d+)',text)
        key=f'special-{special_index}' if kind=='special' else kind
        art={'Smoke':'smoke','Stormwave':'stormwave','Red':'red','Gold':'gold','Platinum':'platinum','Harmonic Break':'harmonic'}.get(camo,'')
        title={'Gold':'黄金','Platinum':'白金','Harmonic Break':'赤律'}.get(camo,camo)
        conflict=name=='HAN 86' and camo=='Red'
        if conflict:rule+=' 社区列为 15 次，但官方与小黑盒正文缺失次数，需在游戏内核对。'
        w['Challenges'].append({'Id':slug(name)+':'+key,'CatalogKey':key,'Name':title,'Stage':'特殊迷彩' if kind=='special' else '完美主义者','Kind':kind,'Target':target,'Rule':rule,'Source':SOURCE,'SourceRule':mp,'DmzRule':dmz_rule,'DmzSourceRule':dmz,'DmzTarget':dmz_target,'ArtKey':art,'RequiredWeaponLevel':int(unlock.group(1)) if unlock else 0,'LevelGateUnknown':kind=='special' and not unlock,'RequirementUnverified':unknown or conflict,'IsSingleMatch':kind=='gold'})
    if len(w['Challenges'])!=14:raise ValueError((name,len(w['Challenges'])))
    weapons.append(w)
if len(weapons)!=33:raise ValueError(len(weapons))
(ROOT/'src/Data/catalog.json').write_text(json.dumps(weapons,ensure_ascii=False,indent=2),encoding='utf-8')
print(f'Extracted {len(weapons)} weapons / {sum(len(w["Challenges"]) for w in weapons)} weapon challenges. Data only; no UI copied.')

"""Compile attributed, matching base-weapon crops from the official launch cards.
Requires beautifulsoup4 and Pillow. No recoloring or simulated camo application.
"""
import json, re, sys, urllib.request
from pathlib import Path
try:
    from bs4 import BeautifulSoup
    from PIL import Image
except ImportError:
    sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools/python-libs'))
    from bs4 import BeautifulSoup
    from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SLUG = '2026/10/call-of-duty-modern-warfare-4-launch-multiplayer-operators-weapons-equipment-create-a-class'
SOURCE = 'https://www.callofduty.com/uk/en/blog/' + SLUG
ZH_SOURCE = 'https://www.callofduty.com/cn/zh/blog/' + SLUG
def page(url, cache):
    path = ROOT / 'tools' / cache
    path.parent.mkdir(exist_ok=True)
    if not path.exists(): path.write_bytes(urllib.request.urlopen(url).read())
    return BeautifulSoup(path.read_bytes(), 'html.parser')
en = page(SOURCE, 'official-weapons.html')
zh = page(ZH_SOURCE, 'official-weapons-cn.html')
heads = [h for h in en.find_all('h4') if h.find_next('p') and h.find_next('p').get_text(' ', strip=True).startswith('Levels:')]
zheads = [h for h in zh.find_all('h4') if h.find_next('p') and re.match(r'等级[：:]\s*\d+$', h.find_next('p').get_text(' ', strip=True))][:33]
assert len(heads) == len(zheads) == 33
catalog = json.loads((ROOT / 'src/Data/catalog.json').read_text(encoding='utf-8'))
normalize = lambda name: re.sub('[^a-z0-9]', '', name.lower())
by_name = {normalize(w['Name']): w for w in catalog}
records = []
dest = ROOT / 'src/Assets/Weapons'
dest.mkdir(parents=True, exist_ok=True)
originals = ROOT / 'tools/weapon-originals'
originals.mkdir(parents=True, exist_ok=True)
for h, z in zip(heads, zheads):
    name = h.get_text(' ', strip=True)
    weapon = by_name[normalize(name)]
    image = None
    for node in h.next_elements:
        if getattr(node, 'name', None) in ['h3', 'h4']: break
        if getattr(node, 'name', None) == 'img': image = node; break
    assert image is not None, name
    url = image['data-src']
    assert url.startswith('https://imgs.callofduty.com/')
    original = originals / (weapon['CatalogKey'] + '.webp')
    if not original.exists(): urllib.request.urlretrieve(url, original)
    bitmap = Image.open(original).convert('RGB')
    assert bitmap.size == (1920, 1080), (name, bitmap.size)
    crop = (60, 110, 1870, 680)
    if weapon['Category'] == '手枪': crop = (470, 110, 1450, 680)
    if weapon['Category'] == '近战武器': crop = (420, 110, 1500, 680)
    thumb = bitmap.crop(crop)
    thumb.thumbnail((720, 300), Image.Resampling.LANCZOS)
    thumb.save(dest / (weapon['CatalogKey'] + '.jpg'), quality=93, optimize=True)
    records.append(dict(CatalogKey=weapon['CatalogKey'], EnglishName=name,
        ChineseName=z.get_text(' ', strip=True), ChineseOfficialName=z.get_text(' ', strip=True),
        MaxLevel=int(re.search(r'\d+', h.find_next('p').get_text()).group()),
        Image=weapon['CatalogKey'] + '.jpg', Source=SOURCE, ChineseSource=ZH_SOURCE,
        OriginalImage=url, Crop=list(crop)))
(ROOT / 'src/Data/weapons.json').write_text(json.dumps(records, ensure_ascii=False, indent=2), encoding='utf-8')
print(f'Compiled {len(records)} exact weapon mappings and base-image crops.')

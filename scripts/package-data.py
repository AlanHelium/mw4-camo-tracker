"""Build a deterministic, data-only update ZIP and its public GitHub channel.

Edit data/catalog.json and the referenced src/Assets images first.
Then: python scripts/package-data.py
Validate with: CamoTracker.exe --validate-data data/catalog-pack.zip
Publish catalog.json, catalog-pack.zip and channel.json in the same commit.
"""
import hashlib
import json
import re
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / 'data'
definition = json.loads((DATA / 'catalog.json').read_text(encoding='utf-8-sig'))
assert definition['Schema'] == 1
assert definition['Revision'] >= 2026102401
names = set()
for weapon in definition['Weapons']:
    key = weapon['CatalogKey']
    assert re.fullmatch(r'[a-z0-9][a-z0-9-]{0,99}', key) and key not in names
    names.add(key)
    weapon['Id'] = key
    for challenge in weapon['Challenges']:
        challenge['Id'] = key + ':' + challenge['CatalogKey']
        assert not challenge.get('Count') and not challenge.get('Unlocked') and not challenge.get('ProgressKnown') and not challenge.get('OpenConfirmed')

payload = json.dumps(definition, ensure_ascii=False, indent=2).encode('utf-8')
paths = {'weapons/' + item['Image'] for item in definition['WeaponImages']}
paths |= {item['Image'] for item in definition['CamoImages']}
archive_path = DATA / 'catalog-pack.zip'
with zipfile.ZipFile(archive_path, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    def add(name, content):
        entry = zipfile.ZipInfo(name, (2026, 10, 24, 0, 0, 0))
        entry.compress_type = zipfile.ZIP_DEFLATED
        archive.writestr(entry, content)
    add('catalog.json', payload)
    for path in sorted(paths):
        assert re.fullmatch(r'(weapons|camos)/[a-z0-9][a-z0-9-]*\.(png|jpg)', path)
        source = ROOT / 'src/Assets' / ('Weapons' if path.startswith('weapons/') else '') / Path(path).name
        add(path, source.read_bytes())

content = archive_path.read_bytes()
assert len(content) <= 24_000_000
manifest = {key: definition[key] for key in ['Schema', 'Revision', 'Updated', 'MinimumApp', 'Notes']}
manifest.update(Blob=hashlib.sha1(b'blob ' + str(len(content)).encode() + b'\0' + content).hexdigest(),
                Sha256=hashlib.sha256(content).hexdigest(), Bytes=len(content))
files = [{'Path': 'catalog.json', 'Sha256': hashlib.sha256(payload + b'\n').hexdigest(), 'Bytes': len(payload) + 1}]
for path in sorted(paths):
    source = ROOT / 'src/Assets' / ('Weapons' if path.startswith('weapons/') else '') / Path(path).name
    item = source.read_bytes()
    files.append({'Path': path, 'Sha256': hashlib.sha256(item).hexdigest(), 'Bytes': len(item)})
manifest['Files'] = files
(DATA / 'channel.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
(DATA / 'catalog.json').write_bytes(payload + b'\n')
print(f"Data {manifest['Revision']}: {len(names) - 1} weapons; {len(content):,} ZIP bytes; SHA256 {manifest['Sha256']}")

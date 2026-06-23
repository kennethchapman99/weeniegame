#!/usr/bin/env python3
"""
Backyard Map-Based Greybox Planner importer.

Does three things:
  1. Prepares MAP assets for Unity:
       - copies  "Backyard - Aerial.png"  -> Reference/BackyardCapture/Map/Backyard_Aerial.png
       - copies  "export_map.jpg"         -> Reference/BackyardCapture/Map/export_map.png
         (HEIC fallback: converts via sips if a .heic path is supplied instead)
  2. Indexes the numbered reference photos as REFERENCE LINKS — photos are NOT copied
     into the Unity project. Grouped by leading filename number into capture points.
  3. Emits structured data the Unity editor tool turns into an editable greybox scene:
       Reference/BackyardCapture/backyard_manifest.json / .md
       Reference/BackyardCapture/backyard_capture_points.json   (positions preserved on rerun)
       MAPS - template/contact_sheets/*.html                    (external; relative paths work in browser)
"""
from pathlib import Path
import argparse
import datetime
import html
import json
import os
import re
import shutil
import subprocess
import sys

PHOTO_EXTS = {'.jpg', '.jpeg', '.png'}
LEADING_NUMBER = re.compile(r'^(\d+)')

DIRECTION_KEYWORDS = {
    'forward': 'forward', 'forwards': 'forward', 'front': 'forward', 'ahead': 'forward',
    'backward': 'backward', 'backwards': 'backward', 'back': 'backward', 'rear': 'backward',
    'left': 'left', 'right': 'right',
}
OVERVIEW_KEYWORDS = {'high', 'overview', 'overhead', 'top', 'wide', 'aerial'}
DOGVIEW_KEYWORDS = {'dogview', 'dog', 'low', 'route', 'runlane'}
GAMEPLAY_KEYWORDS = {
    'tunnel': 'tunnel', 'crawl': 'tunnel', 'squeeze': 'tunnel', 'under': 'tunnel',
    'hide': 'hide', 'hiding': 'hide',
    'cover': 'cover', 'shadow': 'cover',
    'shrub': 'shrub', 'bush': 'shrub', 'hedge': 'shrub', 'ivy': 'shrub', 'garden': 'shrub',
    'fence': 'fence', 'gate': 'gate', 'latch': 'gate', 'gap': 'gate',
    'dig': 'dig', 'dirt': 'dig', 'soil': 'dig', 'mulch': 'dig',
    'patio': 'patio', 'paver': 'patio', 'stone': 'patio',
    'deck': 'deck', 'stairs': 'deck', 'step': 'deck', 'railing': 'deck',
    'tree': 'tree', 'trunk': 'tree', 'branch': 'tree', 'magnolia': 'tree',
}
INTERACTIVE_TAGS = {'tunnel', 'hide', 'cover', 'fence', 'gate', 'dig', 'shrub'}

# Initial map positions read from the hand-drawn export_map.
# Normalized (0..1, origin bottom-left), matching Backyard - Aerial.png orientation.
# These are used ONLY when a point has never been placed (placed=False).
# Edit these or drag markers in Unity then run Save Capture Point Positions.
INITIAL_POSITIONS = {
    'point_001': (0.83, 0.82),   # house door, top-right
    'point_002': (0.89, 0.53),   # right side, pool stairs area
    'point_003': (0.80, 0.69),   # hot tub area
    'point_004': (0.52, 0.66),   # center, above rock
    'point_005': (0.60, 0.50),   # center, left of pool
    'point_006': (0.70, 0.49),   # right of center, pool stair
    'point_007': (0.89, 0.37),   # pool house
    'point_008': (0.89, 0.12),   # bottom-right corner
    'point_009': (0.53, 0.13),   # bottom-center
    'point_010': (0.55, 0.37),   # center, above bottom fence
    'point_011': (0.57, 0.58),   # center
    'point_012': (0.54, 0.71),   # center, upper
    'point_013': (0.42, 0.64),   # rock area
    'point_014': (0.34, 0.71),   # upper-center-left
    'point_015': (0.15, 0.71),   # left gate
    'point_016': (0.08, 0.82),   # far left
    'point_017': (0.14, 0.88),   # bunker door, top-left
    'point_018': (0.27, 0.79),   # stairs, top-center
    'point_019': (0.10, 0.63),   # left side
    'point_020': (0.12, 0.54),   # left side, below 19
    'point_021': (0.31, 0.52),   # center-left
    'point_022': (0.37, 0.50),   # tree
    'point_023': (0.25, 0.40),   # garden
    'point_024': (0.40, 0.35),   # garden, right of 23
    'point_025': (0.22, 0.32),   # garden
    'point_026': (0.30, 0.27),   # lower garden
    'point_027': (0.22, 0.20),   # lower garden, left
    'point_028': (0.06, 0.22),   # bottom-left
    'point_029': (0.08, 0.43),   # left side
    'point_030': (0.10, 0.49),   # left side, above 29
    'point_031': (0.09, 0.60),   # left side, between 19 and 20
    'point_032': (0.26, 0.88),   # top stairs
}


def tokens(stem):
    return [t for t in re.split(r'[^a-zA-Z0-9]+', stem.lower()) if t]

def natural_key(p):
    return [int(x) if x.isdigit() else x for x in re.split(r'(\d+)', p.name.lower())]

def point_id_for(name):
    m = LEADING_NUMBER.match(name)
    return f'point_{int(m.group(1)):03d}' if m else None

def classify(tk):
    ss = set(tk)
    direction = next((DIRECTION_KEYWORDS[t] for t in tk if t in DIRECTION_KEYWORDS), '')
    if ss & OVERVIEW_KEYWORDS:
        view_type = 'overview_high'
    elif ss & DOGVIEW_KEYWORDS:
        view_type = 'dogview_route'
    else:
        view_type = 'unknown'
    gameplay = sorted({GAMEPLAY_KEYWORDS[t] for t in tk if t in GAMEPLAY_KEYWORDS})
    tags = set(gameplay)
    if view_type == 'overview_high':
        tags.update({'overview', 'layout'})
    if view_type == 'dogview_route':
        tags.add('dogview')
    return view_type, direction, sorted(tags or {'reference'}), gameplay

def scan_photos(src):
    return sorted(
        (p for p in src.iterdir() if p.is_file() and p.suffix.lower() in PHOTO_EXTS),
        key=natural_key,
    )

def prepare_annotated_map(src_path, dest_png):
    """Copy (JPG/PNG) or convert (HEIC) the annotated map. Returns (ok, error_or_None)."""
    if not src_path.exists():
        return False, f'not found: {src_path}'
    suffix = src_path.suffix.lower()
    dest_png.parent.mkdir(parents=True, exist_ok=True)
    if suffix in {'.jpg', '.jpeg', '.png'}:
        shutil.copy2(src_path, dest_png)
        return True, None
    elif suffix == '.heic':
        if shutil.which('sips') is None:
            return False, 'sips not available — on Windows, export the HEIC to PNG manually'
        try:
            subprocess.run(['sips', '-s', 'format', 'png', str(src_path), '--out', str(dest_png)],
                           check=True, capture_output=True, text=True)
            return dest_png.exists(), None if dest_png.exists() else 'sips ran but no output file'
        except subprocess.CalledProcessError as e:
            return False, (e.stderr or e.stdout or str(e)).strip()
    return False, f'unsupported format: {suffix}'

def image_pixel_size(path):
    if shutil.which('sips') is None or not path.exists():
        return None
    try:
        out = subprocess.run(['sips', '-g', 'pixelWidth', '-g', 'pixelHeight', str(path)],
                             check=True, capture_output=True, text=True).stdout
        w = re.search(r'pixelWidth:\s*(\d+)', out)
        h = re.search(r'pixelHeight:\s*(\d+)', out)
        if w and h:
            return int(w.group(1)), int(h.group(1))
    except subprocess.CalledProcessError:
        pass
    return None

def write_manifest_md(path, manifest):
    lines = [
        '# Backyard Capture Manifest', '',
        f'Generated: `{manifest["generated_at"]}`', '',
        f'Source photos: `{manifest["source_root"]}`', '',
        f'Images indexed: **{len(manifest["images"])}**  |  Capture points: **{len(manifest["points"])}**', '',
        '> Photos are referenced in place — they are NOT copied into the Unity project.', '',
        '| Point | File | View | Direction | Tags |',
        '|---|---|---|---|---|',
    ]
    for img in manifest['images']:
        lines.append(f'| {img["point_id"]} | {img["filename"]} | {img["type"]} | '
                     f'{img["direction"] or "-"} | {", ".join(img["tags"])} |')
    path.write_text('\n'.join(lines) + '\n', encoding='utf-8')

def write_contact_sheet(path, title, images):
    """External browser contact sheet. Relative src paths work because the sheet is in the
    MAPS - template folder, sibling to 01_exports_flat."""
    figures = []
    for img in images:
        rel = os.path.relpath(img['original_file'], path.parent).replace(os.sep, '/')
        caption = (
            f'<b>{html.escape(img["point_id"] or "ungrouped")}</b><br>'
            f'{html.escape(img["filename"])}<br>'
            f'<small>{html.escape(img["type"])}'
            f'{" / " + html.escape(img["direction"]) if img["direction"] else ""}</small><br>'
            f'<small>{html.escape(", ".join(img["tags"]))}</small>'
        )
        figures.append(
            f'<figure><a href="{html.escape(rel)}" target="_blank">'
            f'<img src="{html.escape(rel)}" loading="lazy"></a>'
            f'<figcaption>{caption}</figcaption></figure>'
        )
    style = (
        'body{font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif;margin:24px}'
        '.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(220px,1fr));gap:16px}'
        'figure{margin:0;border:1px solid #ccc;padding:8px;border-radius:8px;background:#fafafa}'
        'img{width:100%;height:150px;object-fit:cover;border-radius:4px}'
        'figcaption{font-size:12px;line-height:1.35;margin-top:6px;overflow-wrap:anywhere}'
    )
    path.write_text(
        f'<!doctype html><meta charset="utf-8"><title>{html.escape(title)}</title>'
        f'<style>{style}</style><h1>{html.escape(title)}</h1>'
        f'<p>{len(images)} images</p>'
        f'<div class="grid">{"".join(figures)}</div>',
        encoding='utf-8',
    )

def build_points(images, existing_points):
    """Aggregate images into capture points. Merge order: existing placed > INITIAL_POSITIONS > staging."""
    prior = {p['id']: p for p in existing_points}
    by_point = {}
    for img in images:
        if not img['point_id']:
            continue
        by_point.setdefault(img['point_id'], []).append(img)

    points = []
    for staging_index, pid in enumerate(sorted(by_point)):
        entries = by_point[pid]
        tags, candidates, types, directions = set(), set(), set(), set()
        for e in entries:
            tags.update(e['tags'])
            candidates.update(e['gameplay_candidates'])
            types.add(e['type'])
            if e['direction']:
                directions.add(e['direction'])
        prev = prior.get(pid)
        # Preserve explicitly placed position; fall back to hand-drawn map initial positions.
        if prev and prev.get('map_position', {}).get('placed'):
            map_position = prev['map_position']
        elif pid in INITIAL_POSITIONS:
            ix, iy = INITIAL_POSITIONS[pid]
            map_position = {'x': ix, 'y': iy, 'placed': True}
        else:
            map_position = {'x': 0.0, 'y': 0.0, 'placed': False}
        points.append({
            'id': pid,
            'images': sorted(e['filename'] for e in entries),
            'types': sorted(types),
            'directions': sorted(directions),
            'tags': sorted(tags),
            'gameplay_candidates': sorted(candidates),
            'interactive': bool(candidates & INTERACTIVE_TAGS),
            'staging_index': prev.get('staging_index', staging_index) if prev else staging_index,
            'map_position': map_position,
        })
    return points


def main():
    ap = argparse.ArgumentParser(description='Backyard map-based greybox planner importer')
    ap.add_argument('--source', default='MAPS - template/01_exports_flat')
    ap.add_argument('--aerial', default='MAPS - template/Backyard - Aerial.png')
    ap.add_argument('--annotated', default='MAPS - template/export_map.jpg',
                    help='annotated capture-point map (JPG/PNG preferred; HEIC converted via sips)')
    ap.add_argument('--unity', default='unity/CheddarAndCocoa')
    args = ap.parse_args()

    src = Path(args.source).expanduser().resolve()
    aerial_src = Path(args.aerial).expanduser().resolve()
    annotated_src = Path(args.annotated).expanduser().resolve()
    unity = Path(args.unity).expanduser().resolve()

    if not src.is_dir():
        print('ERROR: source photo folder not found:', src, file=sys.stderr); return 1
    if not unity.is_dir():
        print('ERROR: Unity project root not found:', unity, file=sys.stderr); return 1

    out = unity / 'Assets/Reference/BackyardCapture'
    map_dir = out / 'Map'
    # Contact sheets live next to the source photos so relative img src paths work in browsers.
    sheets_dir = src.parent / 'contact_sheets'
    out.mkdir(parents=True, exist_ok=True)
    map_dir.mkdir(parents=True, exist_ok=True)
    sheets_dir.mkdir(parents=True, exist_ok=True)

    # --- 1. Map assets -------------------------------------------------------
    aerial_ok = False
    aerial_dest = map_dir / 'Backyard_Aerial.png'
    if aerial_src.exists():
        shutil.copy2(aerial_src, aerial_dest)
        aerial_ok = True
    else:
        print('WARN: aerial map not found:', aerial_src, file=sys.stderr)

    annotated_dest = map_dir / 'export_map.png'
    annotated_ok, annotated_err = prepare_annotated_map(annotated_src, annotated_dest)
    if not annotated_ok:
        print('WARN: annotated map not prepared:', annotated_err, file=sys.stderr)

    aerial_size = image_pixel_size(aerial_dest) if aerial_ok else None

    # --- 2. Index reference photos -------------------------------------------
    photos = scan_photos(src)
    if not photos:
        print('ERROR: no JPG/PNG photos found in', src, file=sys.stderr); return 1

    images = []
    ungrouped = 0
    for p in photos:
        tk = tokens(p.stem)
        view_type, direction, tags, gameplay = classify(tk)
        pid = point_id_for(p.name)
        if pid is None:
            ungrouped += 1
        images.append({
            'filename': p.name,
            'original_file': str(p),
            'point_id': pid,
            'type': view_type,
            'direction': direction,
            'tags': tags,
            'gameplay_candidates': gameplay,
        })

    # --- 3. Emit data --------------------------------------------------------
    points_path = out / 'backyard_capture_points.json'
    existing_points = []
    if points_path.exists():
        try:
            existing_points = json.loads(points_path.read_text(encoding='utf-8')).get('points', [])
        except (ValueError, OSError):
            existing_points = []
    points = build_points(images, existing_points)

    generated_at = datetime.datetime.now().isoformat(timespec='seconds')
    manifest = {
        'area': 'backyard_main',
        'generated_at': generated_at,
        'source_root': str(src),
        'unity_root': str(unity),
        'output_root': str(out),
        'aerial_map_asset': 'Assets/Reference/BackyardCapture/Map/Backyard_Aerial.png' if aerial_ok else '',
        'annotated_map_asset': 'Assets/Reference/BackyardCapture/Map/export_map.png' if annotated_ok else '',
        'contact_sheets_dir': str(sheets_dir),
        'images': images,
        'points': points,
    }
    (out / 'backyard_manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    write_manifest_md(out / 'backyard_manifest.md', manifest)

    capture_points = {
        'area': 'backyard_main',
        'generated_at': generated_at,
        'aerial_map_asset': manifest['aerial_map_asset'],
        'annotated_map_asset': manifest['annotated_map_asset'],
        'aerial_pixel_size': ({'width': aerial_size[0], 'height': aerial_size[1]} if aerial_size else None),
        'position_space': 'normalized_0_1_origin_bottom_left',
        'points': points,
    }
    points_path.write_text(json.dumps(capture_points, indent=2), encoding='utf-8')

    write_contact_sheet(sheets_dir / 'contact_sheet_all.html',
                        'Backyard Reference — All', images)
    write_contact_sheet(sheets_dir / 'contact_sheet_overview.html',
                        'Backyard Reference — Overview / Layout',
                        [i for i in images if i['type'] == 'overview_high'])
    write_contact_sheet(sheets_dir / 'contact_sheet_dogview.html',
                        'Backyard Reference — Dog View Routes',
                        [i for i in images if i['type'] == 'dogview_route'])
    write_contact_sheet(sheets_dir / 'contact_sheet_interactive.html',
                        'Backyard Reference — Interactive Candidates',
                        [i for i in images if set(i['gameplay_candidates']) & INTERACTIVE_TAGS])

    # --- Ensure the Unity editor tool is present (never clobber edits) ------
    editor_dir = unity / 'Assets/Editor'
    editor_dir.mkdir(parents=True, exist_ok=True)
    builder_dest = editor_dir / 'BackyardGreyboxPlannerBuilder.cs'
    builder_src = Path(__file__).resolve().parents[2] / \
        'unity/CheddarAndCocoa/Assets/Editor/BackyardGreyboxPlannerBuilder.cs'
    if not builder_dest.exists() and builder_src.exists():
        shutil.copy2(builder_src, builder_dest)
        print('Installed editor tool:', builder_dest)

    # --- Report -------------------------------------------------------------
    placed = sum(1 for p in points if p['map_position'].get('placed'))
    print('Images indexed:      ', len(images), f'({ungrouped} ungrouped)')
    print('Capture points:      ', len(points), f'({placed} placed on map)')
    print('Aerial map copied:   ', 'yes' if aerial_ok else 'NO')
    print('Annotated map:       ', 'yes' if annotated_ok else f'NO — {annotated_err}')
    print('Contact sheets:      ', sheets_dir)
    print('Output root:         ', out)
    print('Next: open Unity and run  Cheddar & Cocoa > Backyard > Build Map-Based Greybox Planner')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())

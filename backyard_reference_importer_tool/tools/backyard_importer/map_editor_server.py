#!/usr/bin/env python3
"""
Backyard Map Editor — local web server.

Run via:  open_backyard_map_editor_mac.command   (double-click in Finder)
Or:       python3 map_editor_server.py

Opens http://localhost:5500 in your browser.
Drag the numbered circles onto the aerial map, click Save.
Positions are written directly to backyard_capture_points.json.
Press Ctrl-C in the terminal to stop the server.
"""
import http.server
import json
import socketserver
import sys
import threading
import traceback
import webbrowser
from pathlib import Path

PORT = 5500
REPO = Path(__file__).resolve().parents[3]
DATA_FILE = REPO / 'unity/CheddarAndCocoa/Assets/Reference/BackyardCapture/backyard_capture_points.json'
AERIAL_FILE = REPO / 'MAPS - template/Backyard_Aerial_Editor.png'  # copy made at startup
AERIAL_SRC  = REPO / 'MAPS - template/Backyard - Aerial.png'

HTML = r"""<!doctype html>
<html>
<head>
<meta charset="utf-8">
<title>Backyard Map Editor</title>
<style>
* { box-sizing: border-box; margin: 0; padding: 0; }
body { background: #111827; color: #e5e7eb; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; height: 100vh; display: flex; flex-direction: column; }
#toolbar { padding: 8px 16px; background: #1f2937; display: flex; align-items: center; gap: 12px; border-bottom: 1px solid #374151; flex-shrink: 0; }
#toolbar h1 { font-size: 14px; font-weight: 700; color: #f9fafb; white-space: nowrap; }
#toolbar p  { font-size: 12px; color: #9ca3af; }
#save-btn   { background: #16a34a; color: #fff; border: none; padding: 7px 20px; border-radius: 6px; cursor: pointer; font-size: 13px; font-weight: 600; white-space: nowrap; }
#save-btn:hover { background: #15803d; }
#copy-btn   { background: #374151; color: #d1d5db; border: none; padding: 7px 14px; border-radius: 6px; cursor: pointer; font-size: 12px; white-space: nowrap; }
#copy-btn:hover { background: #4b5563; }
#status { font-size: 12px; color: #6ee7b7; margin-left: auto; white-space: nowrap; }
#scroll { flex: 1; overflow: auto; padding: 16px; }
#map-wrap { position: relative; display: inline-block; }
#aerial { display: block; max-width: none; user-select: none; pointer-events: none; }
.pt {
  position: absolute; width: 28px; height: 28px; border-radius: 50%;
  background: rgba(59,130,246,0.9); border: 2px solid #fff;
  transform: translate(-50%,-50%); cursor: grab;
  display: flex; align-items: center; justify-content: center;
  font-size: 9px; font-weight: 800; color: #fff; user-select: none;
  box-shadow: 0 2px 8px rgba(0,0,0,0.6); transition: background .12s;
}
.pt:hover  { background: rgba(251,191,36,0.95); z-index: 10; }
.pt.active { cursor: grabbing; background: rgba(239,68,68,0.95); z-index: 20; }
.pt .tip   { display: none; position: absolute; bottom: 32px; left: 50%; transform: translateX(-50%);
             background: rgba(0,0,0,0.85); color: #fff; font-size: 10px; padding: 3px 7px;
             border-radius: 4px; white-space: nowrap; pointer-events: none; }
.pt:hover .tip { display: block; }
</style>
</head>
<body>
<div id="toolbar">
  <h1>Backyard Map Editor</h1>
  <p>Drag numbers onto the map, then Save.</p>
  <button id="save-btn" onclick="save()">Save Positions</button>
  <button id="copy-btn" onclick="copyJSON()" title="Fallback: copy JSON to clipboard then paste into backyard_capture_points.json">Copy JSON</button>
  <span id="status">loading…</span>
</div>
<div id="scroll">
  <div id="map-wrap">
    <img id="aerial" src="/aerial" draggable="false">
  </div>
</div>

<script>
const wrap   = document.getElementById('map-wrap');
const imgEl  = document.getElementById('aerial');
const status = document.getElementById('status');
let points   = [];
let drag = null, ox = 0, oy = 0;
const LS_KEY = 'backyard_map_positions';

imgEl.onload = () => {
  fetch('/positions')
    .then(r => r.json())
    .then(d => {
      // Restore from localStorage if it has more recent changes
      const saved = localStorage.getItem(LS_KEY);
      if (saved) {
        try {
          const ls = JSON.parse(saved);
          // Use localStorage version if it's newer (has any placed=true that server doesn't)
          points = ls.points || d.points || [];
          status.textContent = points.length + ' points (restored from last session) — drag to place, then Save';
        } catch(e) { points = d.points || []; status.textContent = points.length + ' points — drag to place, then Save'; }
      } else {
        points = d.points || [];
        status.textContent = points.length + ' points — drag to place, then Save';
      }
      render();
    })
    .catch(() => { status.textContent = 'ERROR: could not load positions'; });
};

function render() {
  wrap.querySelectorAll('.pt').forEach(e => e.remove());
  const iw = imgEl.offsetWidth, ih = imgEl.offsetHeight;
  points.forEach((p, i) => {
    const mp = p.map_position || { x: 0.5, y: 0.5 };
    const el = document.createElement('div');
    el.className = 'pt';
    el.dataset.i = i;
    el.innerHTML = `${+p.id.split('_')[1]}<span class="tip">${p.id}<br>${(p.images||[]).slice(0,3).join('<br>')}</span>`;
    el.style.left = (mp.x * iw) + 'px';
    el.style.top  = ((1 - mp.y) * ih) + 'px';
    el.addEventListener('mousedown', startDrag);
    wrap.appendChild(el);
  });
}

function startDrag(e) {
  e.preventDefault();
  drag = e.currentTarget;
  drag.classList.add('active');
  const wr = wrap.getBoundingClientRect();
  ox = e.clientX - (parseFloat(drag.style.left) + wr.left);
  oy = e.clientY - (parseFloat(drag.style.top)  + wr.top);
}

document.addEventListener('mousemove', e => {
  if (!drag) return;
  const wr = wrap.getBoundingClientRect();
  const iw = imgEl.offsetWidth, ih = imgEl.offsetHeight;
  const px = Math.max(0, Math.min(iw, e.clientX - wr.left - ox));
  const py = Math.max(0, Math.min(ih, e.clientY - wr.top  - oy));
  drag.style.left = px + 'px';
  drag.style.top  = py + 'px';
  const idx = +drag.dataset.i;
  points[idx].map_position = { x: px / iw, y: 1 - py / ih, placed: true };
  localStorage.setItem(LS_KEY, JSON.stringify({ points }));
});

document.addEventListener('mouseup', () => {
  if (drag) { drag.classList.remove('active'); drag = null; }
});

window.addEventListener('resize', render);

function save() {
  status.textContent = 'Saving…';
  localStorage.setItem(LS_KEY, JSON.stringify({ points }));
  fetch('/save', { method: 'POST', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ points }) })
  .then(r => r.json())
  .then(d => {
    if (d.ok) { localStorage.removeItem(LS_KEY); status.textContent = '✓ Saved ' + d.updated + ' points at ' + new Date().toLocaleTimeString(); }
    else { status.textContent = 'ERROR: ' + d.error; }
  })
  .catch(err => { status.textContent = 'ERROR: ' + err + ' — use Copy JSON as fallback'; });
}

function copyJSON() {
  const out = JSON.stringify({ points }, null, 2);
  navigator.clipboard.writeText(out).then(() => {
    status.textContent = '✓ JSON copied to clipboard — paste into backyard_capture_points.json replacing the "points" array';
  }).catch(() => {
    // fallback: show in a textarea
    const ta = document.createElement('textarea');
    ta.value = out; ta.style.cssText = 'position:fixed;top:10px;left:10px;width:80vw;height:80vh;z-index:999;font-size:11px;';
    document.body.appendChild(ta); ta.select();
    status.textContent = 'Select all + copy from the text box that appeared';
  });
}
</script>
</body>
</html>
"""


class Handler(http.server.BaseHTTPRequestHandler):
    def log_message(self, fmt, *args): pass

    def do_OPTIONS(self):
        self.send_response(200)
        self.send_header('Allow', 'GET, POST, OPTIONS')
        self.end_headers()

    def do_GET(self):
        if self.path == '/':
            self._send(200, 'text/html', HTML.encode())
        elif self.path == '/aerial':
            f = AERIAL_FILE if AERIAL_FILE.exists() else AERIAL_SRC
            if f.exists():
                self._send(200, 'image/png', f.read_bytes())
            else:
                self._send(404, 'text/plain', b'aerial not found')
        elif self.path == '/positions':
            if DATA_FILE.exists():
                self._send(200, 'application/json', DATA_FILE.read_bytes())
            else:
                self._send(404, 'text/plain', b'data file not found - run the importer first')
        else:
            self._send(404, 'text/plain', b'not found')

    def do_POST(self):
        if self.path == '/save':
            try:
                length = int(self.headers.get('Content-Length') or 0)
                body = self.rfile.read(length)
                print(f'[save] received {len(body)} bytes', flush=True)
                if not body:
                    raise ValueError('Empty request body — Content-Length was ' + str(length))
                payload = json.loads(body)
                new_points = payload.get('points', [])
                print(f'[save] {len(new_points)} points in payload', flush=True)
                existing = json.loads(DATA_FILE.read_text(encoding='utf-8'))
                by_id = {p['id']: p for p in existing.get('points', [])}
                updated = 0
                for p in new_points:
                    if p.get('id') in by_id and p.get('map_position'):
                        by_id[p['id']]['map_position'] = p['map_position']
                        updated += 1
                existing['points'] = list(by_id.values())
                DATA_FILE.write_text(json.dumps(existing, indent=2), encoding='utf-8')
                print(f'[save] wrote {updated} positions to {DATA_FILE}', flush=True)
                self._send(200, 'application/json', json.dumps({'ok': True, 'updated': updated}).encode())
            except Exception as ex:
                traceback.print_exc()
                self._send(500, 'application/json', json.dumps({'ok': False, 'error': str(ex)}).encode())
        else:
            self._send(404, 'text/plain', b'not found')

    def _send(self, code, ctype, body):
        self.send_response(code)
        self.send_header('Content-Type', ctype)
        self.send_header('Content-Length', len(body))
        self.end_headers()
        self.wfile.write(body)


class ThreadedHTTPServer(socketserver.ThreadingMixIn, http.server.HTTPServer):
    daemon_threads = True
    allow_reuse_address = True


def main():
    if not DATA_FILE.exists():
        print(f'ERROR: run the importer first — data file not found:\n  {DATA_FILE}')
        sys.exit(1)
    url = f'http://localhost:{PORT}'
    print(f'\nBackyard Map Editor')
    print(f'  {url}')
    print(f'  Drag numbers onto the map, click Save.')
    print(f'  Data: {DATA_FILE}')
    print(f'\nCtrl-C to stop.\n')
    server = ThreadedHTTPServer(('localhost', PORT), Handler)
    threading.Timer(0.8, lambda: webbrowser.open(url)).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print('\nStopped.')


if __name__ == '__main__':
    main()

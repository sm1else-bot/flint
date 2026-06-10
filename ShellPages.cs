using System.Globalization;
using System.Net;

namespace Flint;

public static class ShellPages
{
    public static string Home(SearchEngine searchEngine) => """
    <!doctype html>
    <html lang="en">
    <head>
      <meta charset="utf-8"/>
      <meta name="viewport" content="width=device-width,initial-scale=1"/>
      <title>Home</title>
      <style>
        *,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
        :root{color-scheme:dark}
        html,body{width:100%;height:100%;overflow:hidden;background:transparent}
        #canvas{
          position:relative;width:100%;height:100%;overflow:auto;
          background-color:transparent;
          background-image:radial-gradient(circle,rgba(255,255,255,.055) 1px,transparent 1px);
          background-size:32px 32px;
        }
        #hint{
          position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);
          font-family:'Segoe Script','Caveat','Comic Sans MS',cursive;
          font-size:22px;color:rgba(255,255,255,.18);pointer-events:none;
          user-select:none;white-space:nowrap;transition:opacity 400ms;
        }
        #hint.gone{opacity:0}
        /* Toolbox */
        #toolbox{
          position:fixed;display:flex;align-items:center;gap:1px;padding:5px;
          background:rgba(255,255,255,.08);backdrop-filter:blur(12px);
          -webkit-backdrop-filter:blur(12px);border:1px solid rgba(255,255,255,.12);
          border-radius:999px;z-index:9999;
        }
        #toolbox.hidden{display:none}
        .tb-btn{
          width:46px;height:46px;display:flex;flex-direction:column;
          align-items:center;justify-content:center;gap:3px;
          background:transparent;border:none;border-radius:8px;
          cursor:pointer;color:rgba(255,255,255,.65);
          transition:background 120ms,color 120ms;padding:4px;
        }
        .tb-btn:hover{background:rgba(255,255,255,.08);color:rgba(255,255,255,.9)}
        .tb-btn svg{width:18px;height:18px;stroke:currentColor;fill:none;
          stroke-width:1.5;stroke-linecap:round;stroke-linejoin:round}
        .tb-btn span{font-size:8px;letter-spacing:.06em;text-transform:uppercase;
          opacity:.4;font-family:system-ui}
        /* Tile base */
        .tile{
          position:absolute;background:rgba(255,255,255,.055);
          border:1px solid rgba(255,255,255,.10);border-radius:12px;
          backdrop-filter:blur(5px);-webkit-backdrop-filter:blur(5px);
          box-sizing:border-box;overflow:hidden;
        }
        .tile.dragging{
          opacity:.85;box-shadow:0 16px 40px rgba(0,0,0,.5);
          z-index:500;overflow:visible;
        }
        .tile-x{
          position:absolute;top:5px;right:7px;width:18px;height:18px;
          background:transparent;border:none;color:rgba(255,255,255,.28);
          cursor:pointer;font-size:14px;line-height:18px;text-align:center;
          border-radius:4px;z-index:20;padding:0;font-family:system-ui;
          transition:color 120ms,background 120ms;
        }
        .tile-x:hover{color:rgba(255,255,255,.8);background:rgba(255,255,255,.08)}
        /* Note */
        .note-tile{cursor:grab}
        .note-body{
          position:absolute;top:26px;left:0;right:0;bottom:16px;
          padding:2px 12px;font-family:'Courier New',monospace;font-size:13px;
          color:rgba(255,255,255,.78);background:transparent;border:none;
          outline:none;overflow:auto;line-height:1.65;cursor:text;
          white-space:pre-wrap;word-break:break-word;
        }
        .note-body:empty::before{content:attr(data-ph);color:rgba(255,255,255,.15);pointer-events:none}
        .note-body input[type=checkbox]{
          margin:0 5px 0 0;vertical-align:middle;cursor:pointer;
          accent-color:rgba(255,255,255,.55);width:13px;height:13px;
        }
        .rh{
          position:absolute;bottom:3px;right:3px;width:14px;height:14px;
          cursor:se-resize;opacity:.22;z-index:10;
          display:flex;align-items:center;justify-content:center;
        }
        .rh:hover{opacity:.6}
        /* Shortcut */
        .sc-tile{
          display:flex;flex-direction:column;align-items:center;
          justify-content:center;gap:5px;cursor:pointer;transition:background 120ms;
        }
        .sc-tile:hover{background:rgba(255,255,255,.08)}
        .sc-tile svg{width:26px;height:26px;stroke:rgba(255,255,255,.55);fill:none;
          stroke-width:1.5;stroke-linecap:round;stroke-linejoin:round}
        .sc-tile img{width:26px;height:26px;border-radius:4px;object-fit:contain}
        .sc-lbl{
          font-size:9px;letter-spacing:.10em;text-transform:uppercase;
          color:rgba(255,255,255,.32);max-width:80px;overflow:hidden;
          text-overflow:ellipsis;white-space:nowrap;padding:0 6px;
        }
        .sc-form{
          position:absolute;inset:28px 0 0;padding:0 12px 12px;
          display:flex;flex-direction:column;gap:6px;overflow-y:auto;cursor:default;
        }
        .sc-form input[type='text']{
          width:100%;background:rgba(255,255,255,.06);
          border:1px solid rgba(255,255,255,.12);border-radius:6px;
          color:rgba(255,255,255,.8);font-size:11px;font-family:system-ui;
          padding:5px 8px;outline:none;
        }
        .sc-form input[type='text']:focus{border-color:rgba(255,255,255,.28)}
        .sc-form-lbl{
          font-size:9px;letter-spacing:.10em;text-transform:uppercase;
          color:rgba(255,255,255,.25);font-family:system-ui;margin-top:2px;
        }
        .ic-grid{display:grid;grid-template-columns:repeat(6,1fr);gap:3px}
        .ic-opt{
          height:32px;display:flex;align-items:center;justify-content:center;
          border-radius:5px;cursor:pointer;border:1px solid transparent;
          transition:background 100ms;
        }
        .ic-opt:hover{background:rgba(255,255,255,.08)}
        .ic-opt.on{border-color:rgba(255,255,255,.28);background:rgba(255,255,255,.10)}
        .ic-opt svg{width:14px;height:14px;stroke:rgba(255,255,255,.5);fill:none;
          stroke-width:1.5;stroke-linecap:round;stroke-linejoin:round}
        .fav-row{
          display:flex;align-items:center;gap:5px;font-size:10px;
          color:rgba(255,255,255,.32);cursor:pointer;user-select:none;
        }
        .sc-ok{
          width:100%;padding:5px 0;background:rgba(255,255,255,.07);
          border:1px solid rgba(255,255,255,.12);border-radius:6px;
          color:rgba(255,255,255,.55);font-size:11px;font-family:system-ui;
          cursor:pointer;transition:background 120ms;margin-top:2px;
        }
        .sc-ok:hover{background:rgba(255,255,255,.12)}
        /* Clock */
        .cl-tile{display:flex;flex-direction:column;align-items:center;
          justify-content:center;gap:2px;cursor:grab}
        .cl-time{font-family:'Courier New',monospace;font-size:28px;
          letter-spacing:.05em;color:rgba(255,255,255,.82)}
        .cl-date{font-size:10px;letter-spacing:.10em;text-transform:uppercase;
          color:rgba(255,255,255,.28);font-family:system-ui}
        /* Label */
        .label-tile{
          background:transparent!important;border-color:transparent!important;
          backdrop-filter:none!important;-webkit-backdrop-filter:none!important;
          overflow:visible!important;
        }
        .label-tile:hover{border-color:rgba(255,255,255,.07)!important}
        .label-tile .tile-x{opacity:0;transition:opacity 120ms,color 120ms,background 120ms}
        .label-tile:hover .tile-x{opacity:1}
        .label-handle{
          position:absolute;left:3px;top:50%;transform:translateY(-50%);
          cursor:grab;opacity:0;transition:opacity 120ms;z-index:5;padding:4px 3px;
          display:flex;flex-direction:column;align-items:center;gap:3px;
        }
        .label-tile:hover .label-handle{opacity:1}
        .label-body{
          position:absolute;left:20px;right:8px;top:0;bottom:0;
          display:flex;align-items:center;
          font-size:18px;font-weight:600;color:rgba(255,255,255,.72);
          outline:none;overflow:hidden;white-space:nowrap;cursor:text;
          font-family:system-ui;
        }
        .label-body:empty::before{content:'Label...';color:rgba(255,255,255,.2);pointer-events:none}
        /* Line */
        .line-tile{
          background:transparent!important;border-color:transparent!important;
          backdrop-filter:none!important;-webkit-backdrop-filter:none!important;
          overflow:visible!important;
        }
        .line-tile:hover{border-color:rgba(255,255,255,.07)!important}
        .line-tile .tile-x{opacity:0;transition:opacity 120ms,color 120ms,background 120ms}
        .line-tile:hover .tile-x{opacity:1}
        .ln{position:absolute;background:rgba(255,255,255,.35);border-radius:1px;
          cursor:pointer;transition:background 120ms;}
        .ln:hover{background:rgba(255,255,255,.55)}
        .ln-rot{
          position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);
          background:rgba(20,20,20,.7);border:1px solid rgba(255,255,255,.15);
          color:rgba(255,255,255,.6);font-size:11px;width:22px;height:22px;
          border-radius:4px;cursor:pointer;display:none;align-items:center;
          justify-content:center;z-index:10;line-height:1;
        }
        .line-tile:hover .ln-rot{display:flex}
        /* Timer */
        .tmr-tile{
          display:flex;flex-direction:column;align-items:center;
          justify-content:center;gap:6px;cursor:grab;padding:6px 4px;
        }
        .tmr-mode{display:flex;gap:2px}
        .tmr-mode-btn{
          padding:3px 10px;font-size:9px;letter-spacing:.08em;text-transform:uppercase;
          border-radius:999px;cursor:pointer;background:transparent;
          border:1px solid rgba(255,255,255,.12);color:rgba(255,255,255,.3);
          font-family:system-ui;transition:all 120ms;
        }
        .tmr-mode-btn.active{
          background:rgba(255,255,255,.08);color:rgba(255,255,255,.75);
          border-color:rgba(255,255,255,.2);
        }
        .tmr-display{
          font-family:'Courier New',monospace;font-size:22px;
          letter-spacing:.04em;color:rgba(255,255,255,.85);user-select:none;
        }
        @keyframes tmrflash{0%,100%{color:rgba(255,255,255,.85)}50%{color:rgba(255,100,100,.9)}}
        .tmr-display.done{animation:tmrflash 600ms ease 3}
        .tmr-dur{display:flex;align-items:center;gap:3px}
        .tmr-dur.hidden{display:none}
        .tmr-dur-sep{font-family:'Courier New',monospace;font-size:18px;color:rgba(255,255,255,.4)}
        .tmr-dur-inp{
          width:34px;background:rgba(255,255,255,.06);border:1px solid rgba(255,255,255,.12);
          border-radius:4px;color:rgba(255,255,255,.75);font-size:16px;
          font-family:'Courier New',monospace;text-align:center;padding:2px 0;outline:none;
        }
        .tmr-dur-inp:focus{border-color:rgba(255,255,255,.28)}
        .tmr-btns{display:flex;gap:4px}
        .tmr-btn{
          padding:4px 12px;font-size:9px;letter-spacing:.08em;text-transform:uppercase;
          background:rgba(255,255,255,.06);border:1px solid rgba(255,255,255,.12);
          border-radius:6px;color:rgba(255,255,255,.5);font-family:system-ui;
          cursor:pointer;transition:background 120ms;
        }
        .tmr-btn:hover{background:rgba(255,255,255,.10);color:rgba(255,255,255,.8)}
        /* Recent */
        .recent-tile{display:flex;flex-direction:column;cursor:default}
        .recent-hdr{
          display:flex;align-items:center;justify-content:space-between;
          padding:7px 10px 4px;flex-shrink:0;
        }
        .recent-title{
          font-size:9px;letter-spacing:.12em;text-transform:uppercase;
          color:rgba(255,255,255,.28);font-family:system-ui;
        }
        .recent-refresh{
          background:transparent;border:none;color:rgba(255,255,255,.22);
          font-size:14px;cursor:pointer;padding:0 2px;transition:color 120ms;
          line-height:1;
        }
        .recent-refresh:hover{color:rgba(255,255,255,.6)}
        .recent-list{flex:1;overflow-y:auto;padding:0 6px 6px}
        .recent-list::-webkit-scrollbar{width:3px}
        .recent-list::-webkit-scrollbar-thumb{background:rgba(255,255,255,.12);border-radius:2px}
        .recent-row{
          display:flex;align-items:center;gap:8px;padding:5px 5px;
          border-radius:6px;cursor:pointer;transition:background 100ms;
        }
        .recent-row:hover{background:rgba(255,255,255,.06)}
        .recent-dot{
          width:5px;height:5px;border-radius:50%;
          background:rgba(255,255,255,.2);flex-shrink:0;
        }
        .recent-text{min-width:0;flex:1}
        .recent-item-title{
          font-size:12px;color:rgba(255,255,255,.72);white-space:nowrap;
          overflow:hidden;text-overflow:ellipsis;
        }
        .recent-item-domain{
          font-size:10px;color:rgba(255,255,255,.28);white-space:nowrap;
          overflow:hidden;text-overflow:ellipsis;
        }
        .recent-empty,.recent-loading{
          padding:20px 8px;text-align:center;font-size:11px;
          color:rgba(255,255,255,.2);font-family:system-ui;
        }
        /* Photo Frame */
        .photo-tile{
          background:#fff!important;border:none!important;
          backdrop-filter:none!important;-webkit-backdrop-filter:none!important;
          box-shadow:0 6px 24px rgba(0,0,0,.5)!important;
        }
        .photo-inner{
          position:absolute;top:8px;left:8px;right:8px;bottom:30px;
          overflow:hidden;background:rgba(0,0,0,0.3);border-radius:2px;
        }
        .photo-img{
          width:100%;height:100%;object-fit:contain;display:block;
        }
        .photo-cap{
          position:absolute;bottom:0;left:0;right:0;height:30px;
          display:flex;align-items:center;justify-content:center;
          font-size:11px;color:rgba(0,0,0,.42);font-style:italic;
          font-family:system-ui;outline:none;padding:0 8px;
          white-space:nowrap;overflow:hidden;
        }
        /* Ghost */
        .ghost{
          position:absolute;border:1px dashed rgba(255,255,255,.3);
          border-radius:12px;background:rgba(255,255,255,.02);
          pointer-events:none;z-index:100;transition:border-color 80ms,background 80ms;
        }
        .ghost.no{border-color:rgba(255,80,80,.5);background:rgba(255,60,60,.04)}
        body.placing{cursor:crosshair!important}
        body.placing .tile,body.placing .tile *{cursor:crosshair!important}
      </style>
    </head>
    <body>
      <div id="canvas"><div id="hint">right click to peg stuff!</div></div>
      <div id="toolbox" class="hidden">
        <button class="tb-btn" id="tb-note">
          <svg viewBox="0 0 24 24"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
          <span>note</span>
        </button>
        <button class="tb-btn" id="tb-sc">
          <svg viewBox="0 0 24 24"><path d="M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"/><path d="M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"/></svg>
          <span>shortcut</span>
        </button>
        <button class="tb-btn" id="tb-cl">
          <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
          <span>clock</span>
        </button>
        <button class="tb-btn" id="tb-lbl">
          <svg viewBox="0 0 24 24"><path d="M4 7V4h16v3"/><path d="M9 20h6"/><path d="M12 4v16"/></svg>
          <span>label</span>
        </button>
        <button class="tb-btn" id="tb-ln">
          <svg viewBox="0 0 24 24"><line x1="2" y1="12" x2="22" y2="12"/><circle cx="2" cy="12" r="1.5" fill="currentColor" stroke="none"/><circle cx="22" cy="12" r="1.5" fill="currentColor" stroke="none"/></svg>
          <span>line</span>
        </button>
        <button class="tb-btn" id="tb-tmr">
          <svg viewBox="0 0 24 24"><circle cx="12" cy="13" r="8"/><polyline points="12 9 12 13 14.5 15.5"/><path d="M9.5 3h5"/><line x1="12" y1="3" x2="12" y2="5"/></svg>
          <span>timer</span>
        </button>
        <button class="tb-btn" id="tb-rec">
          <svg viewBox="0 0 24 24"><polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 .49-4.95"/></svg>
          <span>recent</span>
        </button>
        <button class="tb-btn" id="tb-ph">
          <svg viewBox="0 0 24 24"><rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/></svg>
          <span>photo</span>
        </button>
      </div>
      <script>
        const post = p => window.chrome?.webview?.postMessage(JSON.stringify(p));
        const G = 32;
        const canvas = document.getElementById('canvas');
        const toolbox = document.getElementById('toolbox');
        const hint = document.getElementById('hint');
        const p2g = v => Math.max(0, Math.round(v / G));
        const g2p = g => g * G;
        const K = (x,y) => x + ',' + y;

        const DEF = {
          note:{w:6,h:5}, shortcut:{w:3,h:3}, clock:{w:4,h:2},
          label:{w:6,h:1}, line:{w:8,h:1}, timer:{w:5,h:4},
          recent:{w:6,h:6}, photo:{w:5,h:6}
        };
        const SETUP = { shortcut:{w:8,h:10}, photo:{w:5,h:4} };

        let tiles = [], occ = new Set(), placing = null, ghost = null, saveTm;

        function claim(t) {
          for (let x=t.gridX; x<t.gridX+t.gridW; x++)
            for (let y=t.gridY; y<t.gridY+t.gridH; y++) occ.add(K(x,y));
        }
        function release(t) {
          for (let x=t.gridX; x<t.gridX+t.gridW; x++)
            for (let y=t.gridY; y<t.gridY+t.gridH; y++) occ.delete(K(x,y));
        }
        function free(gx,gy,gw,gh) {
          for (let x=gx; x<gx+gw; x++)
            for (let y=gy; y<gy+gh; y++) if (occ.has(K(x,y))) return false;
          return true;
        }
        function syncHint() { hint.classList.toggle('gone', tiles.length > 0); }
        function sched() {
          clearTimeout(saveTm);
          saveTm = setTimeout(() => {
            const out = tiles.filter(t =>
              (t.type !== 'shortcut' || t.content?.url) &&
              (t.type !== 'photo'    || t.content?.url)
            );
            post({ type:'savePegboard', json: JSON.stringify(out) });
          }, 500);
        }

        const IC = {
          globe:         '<circle cx="12" cy="12" r="10"/><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/><line x1="2" y1="12" x2="22" y2="12"/>',
          mail:          '<path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/><polyline points="22,6 12,13 2,6"/>',
          code:          '<polyline points="16 18 22 12 16 6"/><polyline points="8 6 2 12 8 18"/>',
          music:         '<path d="M9 18V5l12-2v13"/><circle cx="6" cy="18" r="3"/><circle cx="18" cy="16" r="3"/>',
          film:          '<rect x="2" y="2" width="20" height="20" rx="2"/><line x1="7" y1="2" x2="7" y2="22"/><line x1="17" y1="2" x2="17" y2="22"/><line x1="2" y1="12" x2="22" y2="12"/>',
          'shopping-bag':'<path d="M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"/><line x1="3" y1="6" x2="21" y2="6"/><path d="M16 10a4 4 0 0 1-8 0"/>',
          chat:          '<path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>',
          github:        '<path d="M9 19c-5 1.5-5-2.5-7-3m14 6v-3.87a3.37 3.37 0 0 0-.94-2.61c3.14-.35 6.44-1.54 6.44-7A5.44 5.44 0 0 0 20 4.77 5.07 5.07 0 0 0 19.91 1S18.73.65 16 2.48a13.38 13.38 0 0 0-7 0C6.27.65 5.09 1 5.09 1A5.07 5.07 0 0 0 5 4.77a5.44 5.44 0 0 0-1.5 3.78c0 5.42 3.3 6.61 6.44 7A3.37 3.37 0 0 0 9 18.13V22"/>',
          twitter:       '<path d="M23 3a10.9 10.9 0 0 1-3.14 1.53 4.48 4.48 0 0 0-7.86 3v1A10.66 10.66 0 0 1 3 4s-4 9 5 13a11.64 11.64 0 0 1-7 2c9 5 20 0 20-11.5a4.5 4.5 0 0 0-.08-.83A7.72 7.72 0 0 0 23 3z"/>',
          youtube:       '<path d="M22.54 6.42a2.78 2.78 0 0 0-1.95-1.96C18.88 4 12 4 12 4s-6.88 0-8.59.46a2.78 2.78 0 0 0-1.95 1.96A29 29 0 0 0 1 12a29 29 0 0 0 .46 5.58A2.78 2.78 0 0 0 3.41 19.6C5.12 20 12 20 12 20s6.88 0 8.59-.46a2.78 2.78 0 0 0 1.95-1.95A29 29 0 0 0 23 12a29 29 0 0 0-.46-5.58z"/><polygon points="9.75 15.02 15.5 12 9.75 8.98 9.75 15.02"/>',
          instagram:     '<rect x="2" y="2" width="20" height="20" rx="5" ry="5"/><path d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z"/><line x1="17.5" y1="6.5" x2="17.51" y2="6.5"/>',
          linkedin:      '<path d="M16 8a6 6 0 0 1 6 6v7h-4v-7a2 2 0 0 0-2-2 2 2 0 0 0-2 2v7h-4v-7a6 6 0 0 1 6-6z"/><rect x="2" y="9" width="4" height="12"/><circle cx="4" cy="4" r="2"/>',
          camera:        '<path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z"/><circle cx="12" cy="13" r="4"/>',
          book:          '<path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"/><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"/>',
          coffee:        '<path d="M18 8h1a4 4 0 0 1 0 8h-1"/><path d="M2 8h16v9a4 4 0 0 1-4 4H6a4 4 0 0 1-4-4V8z"/><line x1="6" y1="1" x2="6" y2="4"/><line x1="10" y1="1" x2="10" y2="4"/><line x1="14" y1="1" x2="14" y2="4"/>',
          gamepad:       '<line x1="6" y1="12" x2="10" y2="12"/><line x1="8" y1="10" x2="8" y2="14"/><line x1="15" y1="13" x2="15.01" y2="13"/><line x1="18" y1="11" x2="18.01" y2="11"/><rect x="2" y="6" width="20" height="12" rx="2"/>',
          terminal:      '<polyline points="4 17 10 11 4 5"/><line x1="12" y1="19" x2="20" y2="19"/>',
          cloud:         '<path d="M18 10h-1.26A8 8 0 1 0 9 20h9a5 5 0 0 0 0-10z"/>',
          lock:          '<rect x="3" y="11" width="18" height="11" rx="2" ry="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/>',
          search:        '<circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>',
          star:          '<polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>',
          heart:         '<path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/>',
          map:           '<polygon points="1 6 1 22 8 18 16 22 23 18 23 2 16 6 8 2 1 6"/><line x1="8" y1="2" x2="8" y2="18"/><line x1="16" y1="6" x2="16" y2="22"/>',
          zap:           '<polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"/>'
        };
        const ICN = Object.keys(IC);

        function mkSvg(body, sz) {
          return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="' + sz + '" height="' + sz + '" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">' + body + '</svg>';
        }

        // ── Build tile ──────────────────────────────────────────────
        function mkTile(t) {
          const el = document.createElement('div');
          el.className = 'tile';
          el.dataset.id = t.id;
          el.style.left   = g2p(t.gridX) + 'px';
          el.style.top    = g2p(t.gridY) + 'px';
          el.style.width  = g2p(t.gridW) + 'px';
          el.style.height = g2p(t.gridH) + 'px';
          const xb = document.createElement('button');
          xb.className = 'tile-x';
          xb.textContent = '\xd7';
          xb.addEventListener('click', e => { e.stopPropagation(); rmTile(t.id); });
          el.appendChild(xb);
          if (t.type === 'note')     mkNote(el, t);
          if (t.type === 'shortcut') mkSc(el, t);
          if (t.type === 'clock')    mkCl(el, t);
          if (t.type === 'label')    mkLabel(el, t);
          if (t.type === 'line')     mkLine(el, t);
          if (t.type === 'timer')    mkTimer(el, t);
          if (t.type === 'recent')   mkRecent(el, t);
          if (t.type === 'photo')    mkPhoto(el, t);
          setupDrag(el, t);
          canvas.appendChild(el);
        }

        // ── Note ────────────────────────────────────────────────────
        function mkNote(el, t) {
          el.classList.add('note-tile');
          const body = document.createElement('div');
          body.className = 'note-body';
          body.contentEditable = 'true';
          body.spellcheck = false;
          body.setAttribute('data-ph', 'note...');
          if (t.content?.html) body.innerHTML = t.content.html;
          else if (t.content?.text) body.textContent = t.content.text;

          body.addEventListener('input', () => { t.content = { html: body.innerHTML }; sched(); });
          body.addEventListener('mousedown', e => e.stopPropagation());

          // /c + space → insert checkbox
          body.addEventListener('keydown', e => {
            if (e.key !== ' ') return;
            const sel = window.getSelection();
            if (!sel || !sel.rangeCount) return;
            const range = sel.getRangeAt(0);
            const node = range.startContainer;
            if (node.nodeType !== Node.TEXT_NODE) return;
            const before = node.textContent.slice(0, range.startOffset);
            if (!before.endsWith('/c')) return;
            e.preventDefault();
            // delete the /c
            const delRange = document.createRange();
            delRange.setStart(node, range.startOffset - 2);
            delRange.setEnd(node, range.startOffset);
            delRange.deleteContents();
            // insert checkbox
            const cb = document.createElement('input');
            cb.type = 'checkbox';
            cb.addEventListener('change', () => { t.content = { html: body.innerHTML }; sched(); });
            cb.addEventListener('mousedown', e => e.stopPropagation());
            const r2 = document.createRange();
            r2.setStart(node, range.startOffset - 2);
            r2.collapse(true);
            r2.insertNode(cb);
            // move caret after checkbox
            const r3 = document.createRange();
            r3.setStartAfter(cb);
            r3.collapse(true);
            sel.removeAllRanges();
            sel.addRange(r3);
          });

          el.appendChild(body);
          const rh = document.createElement('div');
          rh.className = 'rh';
          rh.innerHTML = '<svg viewBox="0 0 10 10" width="10" height="10" fill="none" stroke="white" stroke-width="1.8" stroke-linecap="round"><line x1="2" y1="10" x2="10" y2="2"/><line x1="6" y1="10" x2="10" y2="6"/></svg>';
          setupResize(rh, el, t);
          el.appendChild(rh);
        }

        // ── Shortcut ────────────────────────────────────────────────
        function mkSc(el, t) {
          if (!t.content?.url) { mkScForm(el, t); return; }
          el.classList.add('sc-tile');
          let icon;
          if (t.content.favicon) {
            icon = document.createElement('img');
            icon.src = t.content.favicon;
          } else {
            icon = document.createElement('span');
            icon.innerHTML = mkSvg(IC[t.content.icon] || IC.globe, 26);
          }
          el.appendChild(icon);
          const lbl = document.createElement('div');
          lbl.className = 'sc-lbl';
          try { lbl.textContent = new URL(t.content.url).hostname.replace(/^www\./, ''); }
          catch { lbl.textContent = t.content.url; }
          el.appendChild(lbl);
        }

        function mkScForm(el, t) {
          el.style.cursor = 'default';
          const form = document.createElement('div');
          form.className = 'sc-form';
          const urlInp = document.createElement('input');
          urlInp.type = 'text'; urlInp.placeholder = 'https://...';
          urlInp.addEventListener('mousedown', e => e.stopPropagation());
          urlInp.addEventListener('click', e => e.stopPropagation());
          form.appendChild(urlInp);
          const icLbl = document.createElement('div');
          icLbl.className = 'sc-form-lbl'; icLbl.textContent = 'icon';
          form.appendChild(icLbl);
          const grid = document.createElement('div');
          grid.className = 'ic-grid';
          let selIcon = 'globe';
          ICN.forEach(name => {
            const opt = document.createElement('div');
            opt.className = 'ic-opt' + (name === selIcon ? ' on' : '');
            opt.innerHTML = mkSvg(IC[name], 14);
            opt.addEventListener('click', e => {
              e.stopPropagation();
              grid.querySelectorAll('.on').forEach(o => o.classList.remove('on'));
              opt.classList.add('on'); selIcon = name;
            });
            grid.appendChild(opt);
          });
          form.appendChild(grid);
          const favRow = document.createElement('label');
          favRow.className = 'fav-row';
          const favChk = document.createElement('input');
          favChk.type = 'checkbox';
          favChk.addEventListener('mousedown', e => e.stopPropagation());
          favRow.appendChild(favChk);
          favRow.appendChild(document.createTextNode('Use favicon instead'));
          form.appendChild(favRow);
          const okBtn = document.createElement('button');
          okBtn.className = 'sc-ok'; okBtn.textContent = 'Add shortcut';
          okBtn.addEventListener('click', e => {
            e.stopPropagation();
            let url = urlInp.value.trim();
            if (!url) return;
            if (!/^https?:\/\//i.test(url)) url = 'https://' + url;
            let favicon = null;
            if (favChk.checked) {
              try { favicon = 'https://www.google.com/s2/favicons?domain=' + new URL(url).hostname + '&sz=32'; }
              catch {}
            }
            const old = canvas.querySelector('[data-id="' + t.id + '"]');
            release(t);
            t.content = { url, icon: selIcon, favicon };
            t.gridW = DEF.shortcut.w; t.gridH = DEF.shortcut.h;
            claim(t);
            if (old) old.remove();
            mkTile(t); sched();
          });
          form.appendChild(okBtn);
          el.appendChild(form);
        }

        // ── Clock ────────────────────────────────────────────────────
        function mkCl(el, t) {
          el.classList.add('cl-tile');
          const te = document.createElement('div'); te.className = 'cl-time';
          const de = document.createElement('div'); de.className = 'cl-date';
          function tick() {
            const n = new Date();
            te.textContent = String(n.getHours()).padStart(2,'0') + ':' + String(n.getMinutes()).padStart(2,'0');
            de.textContent = n.toLocaleDateString('en-US', { weekday:'short', month:'short', day:'numeric' });
          }
          tick();
          el._iv = setInterval(tick, 1000);
          el.appendChild(te); el.appendChild(de);
        }

        // ── Label ────────────────────────────────────────────────────
        function mkLabel(el, t) {
          el.classList.add('label-tile');
          const handle = document.createElement('div');
          handle.className = 'label-handle';
          handle.innerHTML = '<svg width="6" height="14" viewBox="0 0 6 14" fill="rgba(255,255,255,.35)"><circle cx="3" cy="2" r="1.5"/><circle cx="3" cy="7" r="1.5"/><circle cx="3" cy="12" r="1.5"/></svg>';
          el.appendChild(handle);
          const body = document.createElement('div');
          body.className = 'label-body';
          body.contentEditable = 'true';
          body.spellcheck = false;
          body.textContent = t.content?.text || '';
          body.addEventListener('input', () => { t.content = { text: body.textContent }; sched(); });
          body.addEventListener('mousedown', e => e.stopPropagation());
          el.appendChild(body);
        }

        // ── Line ────────────────────────────────────────────────────
        function mkLine(el, t) {
          el.classList.add('line-tile');
          if (!t.content) t.content = {};
          t.content.orientation = t.content.orientation || 'h';
          t.content.thickness   = t.content.thickness   || 1;

          const ln = document.createElement('div');
          ln.className = 'ln';
          applyLn(ln, t.content.orientation, t.content.thickness);
          ln.addEventListener('click', e => {
            e.stopPropagation();
            t.content.thickness = (t.content.thickness % 3) + 1;
            applyLn(ln, t.content.orientation, t.content.thickness);
            sched();
          });
          el.appendChild(ln);

          const rb = document.createElement('button');
          rb.className = 'ln-rot'; rb.textContent = '\u21f3';
          rb.title = 'Rotate';
          rb.addEventListener('click', e => {
            e.stopPropagation();
            const newOri = t.content.orientation === 'h' ? 'v' : 'h';
            release(t);
            const nw = t.gridH, nh = t.gridW;
            if (free(t.gridX, t.gridY, nw, nh)) {
              t.gridW = nw; t.gridH = nh;
              t.content.orientation = newOri;
              el.style.width  = g2p(t.gridW) + 'px';
              el.style.height = g2p(t.gridH) + 'px';
              applyLn(ln, newOri, t.content.thickness);
              claim(t); sched();
            } else {
              claim(t);
              el.style.borderColor = 'rgba(255,80,80,.6)';
              el.style.borderStyle = 'solid';
              setTimeout(() => { el.style.borderColor = ''; el.style.borderStyle = ''; }, 600);
            }
          });
          el.appendChild(rb);
        }

        function applyLn(ln, ori, th) {
          if (ori === 'h') {
            ln.style.cssText = 'width:100%;height:' + th + 'px;position:absolute;top:50%;transform:translateY(-50%);background:rgba(255,255,255,.35);border-radius:1px;';
          } else {
            ln.style.cssText = 'height:100%;width:' + th + 'px;position:absolute;left:50%;transform:translateX(-50%);background:rgba(255,255,255,.35);border-radius:1px;';
          }
        }

        // ── Timer / Stopwatch ────────────────────────────────────────
        function mkTimer(el, t) {
          el.classList.add('tmr-tile');
          let mode = 'stopwatch', running = false, elapsed = 0, remaining = 0;
          let setDuration = 5 * 60 * 1000, iv = null;
          const pad2 = n => String(n).padStart(2,'0');

          function fmtMs(ms, cs) {
            const total = Math.max(0, Math.floor(ms / 10));
            const c = total % 100, s = Math.floor(total/100)%60, m = Math.floor(total/6000);
            return cs ? pad2(m)+':'+pad2(s)+'.'+pad2(c) : pad2(m)+':'+pad2(s);
          }

          // Mode pill
          const pill = document.createElement('div'); pill.className = 'tmr-mode';
          const swBtn = document.createElement('button'); swBtn.className = 'tmr-mode-btn active'; swBtn.textContent = 'Stopwatch';
          const tmBtn = document.createElement('button'); tmBtn.className = 'tmr-mode-btn'; tmBtn.textContent = 'Timer';
          pill.appendChild(swBtn); pill.appendChild(tmBtn);
          pill.addEventListener('mousedown', e => e.stopPropagation());
          el.appendChild(pill);

          // Duration inputs (timer mode only)
          const durRow = document.createElement('div'); durRow.className = 'tmr-dur hidden';
          const mmI = document.createElement('input'); mmI.className = 'tmr-dur-inp'; mmI.value = '05'; mmI.maxLength = 2;
          const sep = document.createElement('span'); sep.className = 'tmr-dur-sep'; sep.textContent = ':';
          const ssI = document.createElement('input'); ssI.className = 'tmr-dur-inp'; ssI.value = '00'; ssI.maxLength = 2;
          durRow.appendChild(mmI); durRow.appendChild(sep); durRow.appendChild(ssI);
          durRow.addEventListener('mousedown', e => e.stopPropagation());
          el.appendChild(durRow);

          // Display
          const disp = document.createElement('div'); disp.className = 'tmr-display';
          disp.textContent = '00:00.00';
          el.appendChild(disp);

          // Buttons
          const btns = document.createElement('div'); btns.className = 'tmr-btns';
          const startBtn = document.createElement('button'); startBtn.className = 'tmr-btn'; startBtn.textContent = 'Start';
          const resetBtn = document.createElement('button'); resetBtn.className = 'tmr-btn'; resetBtn.textContent = 'Reset';
          btns.appendChild(startBtn); btns.appendChild(resetBtn);
          btns.addEventListener('mousedown', e => e.stopPropagation());
          el.appendChild(btns);

          function tick() {
            if (mode === 'stopwatch') {
              elapsed += 10; disp.textContent = fmtMs(elapsed, true);
            } else {
              remaining -= 10;
              if (remaining <= 0) {
                remaining = 0; disp.textContent = fmtMs(0, false);
                disp.classList.add('done');
                setTimeout(() => disp.classList.remove('done'), 1800);
                doStop(); return;
              }
              disp.textContent = fmtMs(remaining, false);
            }
          }
          function doStart() {
            if (mode === 'timer' && remaining === 0) {
              const m = Math.max(0,Math.min(99,parseInt(mmI.value)||0));
              const s = Math.max(0,Math.min(59,parseInt(ssI.value)||0));
              setDuration = (m*60+s)*1000; remaining = setDuration;
              if (!remaining) return;
            }
            running = true; startBtn.textContent = 'Pause';
            iv = setInterval(tick, 10);
          }
          function doStop() {
            running = false; startBtn.textContent = 'Start';
            clearInterval(iv); iv = null;
          }
          function doReset() {
            doStop();
            if (mode === 'stopwatch') { elapsed = 0; disp.textContent = '00:00.00'; }
            else { remaining = 0; disp.textContent = fmtMs(setDuration, false); }
          }
          function setMode(m) {
            doStop(); mode = m; elapsed = 0; remaining = 0;
            if (m === 'stopwatch') {
              swBtn.classList.add('active'); tmBtn.classList.remove('active');
              durRow.classList.add('hidden'); disp.textContent = '00:00.00';
            } else {
              tmBtn.classList.add('active'); swBtn.classList.remove('active');
              durRow.classList.remove('hidden');
              const mm = Math.max(0,Math.min(99,parseInt(mmI.value)||5));
              const ss = Math.max(0,Math.min(59,parseInt(ssI.value)||0));
              setDuration = (mm*60+ss)*1000;
              disp.textContent = fmtMs(setDuration, false);
            }
          }

          swBtn.addEventListener('click', e => { e.stopPropagation(); setMode('stopwatch'); });
          tmBtn.addEventListener('click', e => { e.stopPropagation(); setMode('timer'); });
          startBtn.addEventListener('click', e => { e.stopPropagation(); if (running) doStop(); else doStart(); });
          resetBtn.addEventListener('click', e => { e.stopPropagation(); doReset(); });

          el._cleanup = () => { clearInterval(iv); };
        }

        // ── Recent ───────────────────────────────────────────────────
        function mkRecent(el, t) {
          el.classList.add('recent-tile');
          const hdr = document.createElement('div'); hdr.className = 'recent-hdr';
          const title = document.createElement('span'); title.className = 'recent-title'; title.textContent = 'Recent';
          const refBtn = document.createElement('button'); refBtn.className = 'recent-refresh'; refBtn.textContent = '\u21bb';
          hdr.appendChild(title); hdr.appendChild(refBtn);
          el.appendChild(hdr);
          const list = document.createElement('div'); list.className = 'recent-list';
          el.appendChild(list);

          function load() {
            list.innerHTML = '<div class="recent-loading">\u2026</div>';
            post({ type:'loadRecent', tileId: t.id });
          }
          function onMsg(e) {
            try {
              const msg = JSON.parse(e.data);
              if (msg.type !== 'recentData' || msg.tileId !== t.id) return;
              list.innerHTML = '';
              if (!msg.items?.length) {
                list.innerHTML = '<div class="recent-empty">No history yet.</div>'; return;
              }
              msg.items.forEach(item => {
                const row = document.createElement('div'); row.className = 'recent-row';
                const dot = document.createElement('div'); dot.className = 'recent-dot';
                const txt = document.createElement('div'); txt.className = 'recent-text';
                const tEl = document.createElement('div'); tEl.className = 'recent-item-title';
                tEl.textContent = item.title || item.url;
                const dEl = document.createElement('div'); dEl.className = 'recent-item-domain';
                try { dEl.textContent = new URL(item.url).hostname.replace(/^www\./, ''); }
                catch { dEl.textContent = ''; }
                txt.appendChild(tEl); txt.appendChild(dEl);
                row.appendChild(dot); row.appendChild(txt);
                row.addEventListener('click', () => post({ type:'openUrl', url:item.url }));
                list.appendChild(row);
              });
            } catch {}
          }
          window.chrome?.webview?.addEventListener('message', onMsg);
          el._cleanup = () => window.chrome?.webview?.removeEventListener('message', onMsg);

          refBtn.addEventListener('click', e => { e.stopPropagation(); load(); });
          refBtn.addEventListener('mousedown', e => e.stopPropagation());
          load();
        }

        // ── Photo ────────────────────────────────────────────────────
        function mkPhoto(el, t) {
          if (!t.content?.url) { mkPhotoForm(el, t); return; }
          el.classList.add('photo-tile');
          const rot = t.content.rotate || 0;
          el.style.transform = 'rotate(' + rot + 'deg)';
          const inner = document.createElement('div');
          inner.className = 'photo-inner';
          const img = document.createElement('img');
          img.className = 'photo-img'; img.src = t.content.url; img.draggable = false;
          inner.appendChild(img);
          el.appendChild(inner);
          const cap = document.createElement('div');
          cap.className = 'photo-cap'; cap.contentEditable = 'true';
          cap.textContent = t.content.caption || '';
          cap.addEventListener('input', () => { t.content.caption = cap.textContent; sched(); });
          cap.addEventListener('mousedown', e => e.stopPropagation());
          el.appendChild(cap);
          // dark × for white background
          setTimeout(() => {
            const xb = el.querySelector('.tile-x');
            if (xb) xb.style.color = 'rgba(0,0,0,.35)';
          }, 0);
        }

        function mkPhotoForm(el, t) {
          el.style.cursor = 'default';
          const form = document.createElement('div'); form.className = 'sc-form';
          const urlInp = document.createElement('input');
          urlInp.type = 'text'; urlInp.placeholder = 'Direct image URL (.jpg, .png\u2026)';
          urlInp.addEventListener('mousedown', e => e.stopPropagation());
          urlInp.addEventListener('click', e => e.stopPropagation());
          form.appendChild(urlInp);
          const okBtn = document.createElement('button');
          okBtn.className = 'sc-ok'; okBtn.textContent = 'Add photo';
          okBtn.addEventListener('click', e => {
            e.stopPropagation();
            let url = urlInp.value.trim();
            if (!url) return;
            if (!/^https?:\/\//i.test(url)) url = 'https://' + url;
            const rotate = parseFloat((Math.random()*6-3).toFixed(2));
            let caption = '';
            try { caption = new URL(url).hostname.replace(/^www\./, ''); } catch {}
            const old = canvas.querySelector('[data-id="' + t.id + '"]');
            release(t);
            t.content = { url, caption, rotate };
            t.gridW = DEF.photo.w; t.gridH = DEF.photo.h;
            claim(t);
            if (old) old.remove();
            mkTile(t); sched();
          });
          form.appendChild(okBtn);
          el.appendChild(form);
        }

        // ── Drag ─────────────────────────────────────────────────────
        function setupDrag(el, t) {
          let wasDragged = false;
          el.addEventListener('mousedown', e => {
            if (e.target.closest('.tile-x,.note-body,.rh,.sc-form,.label-body,.photo-cap,.tmr-mode,.tmr-btns,.tmr-dur,.recent-hdr,.recent-list,.ln,.ln-rot')) return;
            e.preventDefault();
            const sx = e.clientX, sy = e.clientY;
            const sl = parseInt(el.style.left)||0, st = parseInt(el.style.top)||0;
            let moved = false; wasDragged = false;
            const mm = ev => {
              const dx = ev.clientX-sx, dy = ev.clientY-sy;
              if (!moved && Math.hypot(dx,dy) > 5) {
                moved = true; wasDragged = true;
                el.classList.add('dragging'); release(t);
              }
              if (moved) {
                el.style.left = Math.max(0, sl+dx) + 'px';
                el.style.top  = Math.max(0, st+dy) + 'px';
              }
            };
            const mu = () => {
              document.removeEventListener('mousemove', mm);
              document.removeEventListener('mouseup', mu);
              if (!moved) return;
              el.classList.remove('dragging');
              const ngx = p2g(parseInt(el.style.left)||0);
              const ngy = p2g(parseInt(el.style.top)||0);
              if (free(ngx, ngy, t.gridW, t.gridH)) { t.gridX = ngx; t.gridY = ngy; }
              el.style.left = g2p(t.gridX) + 'px';
              el.style.top  = g2p(t.gridY) + 'px';
              claim(t); sched();
            };
            document.addEventListener('mousemove', mm);
            document.addEventListener('mouseup', mu);
          });
          if (t.type === 'shortcut' && t.content?.url) {
            el.addEventListener('click', e => {
              if (wasDragged) return;
              if (!e.target.closest('.tile-x')) post({ type:'openUrl', url:t.content.url });
            });
          }
        }

        // ── Resize ───────────────────────────────────────────────────
        function setupResize(handle, el, t) {
          handle.addEventListener('mousedown', e => {
            e.preventDefault(); e.stopPropagation();
            const sx = e.clientX, sy = e.clientY;
            const sw = t.gridW, sh = t.gridH;
            release(t);
            const mm = ev => {
              el.style.width  = g2p(Math.max(3, sw + Math.round((ev.clientX-sx)/G))) + 'px';
              el.style.height = g2p(Math.max(2, sh + Math.round((ev.clientY-sy)/G))) + 'px';
            };
            const mu = () => {
              document.removeEventListener('mousemove', mm);
              document.removeEventListener('mouseup', mu);
              const nw = Math.max(3, p2g(parseInt(el.style.width)));
              const nh = Math.max(2, p2g(parseInt(el.style.height)));
              if (free(t.gridX, t.gridY, nw, nh)) { t.gridW = nw; t.gridH = nh; }
              el.style.width  = g2p(t.gridW) + 'px';
              el.style.height = g2p(t.gridH) + 'px';
              claim(t); sched();
            };
            document.addEventListener('mousemove', mm);
            document.addEventListener('mouseup', mu);
          });
        }

        // ── Add / Remove ─────────────────────────────────────────────
        function uid() { return Date.now().toString(36) + Math.random().toString(36).slice(2); }

        function addTile(type, gx, gy) {
          const sz = SETUP[type] || DEF[type];
          const gw = sz.w, gh = sz.h;
          if (!free(gx, gy, gw, gh)) return false;
          const t = { id:uid(), type, gridX:gx, gridY:gy, gridW:gw, gridH:gh, content:{} };
          if (type === 'line') t.content = { orientation:'h', thickness:1 };
          tiles.push(t); claim(t); mkTile(t); sched(); syncHint();
          return true;
        }

        function rmTile(id) {
          const i = tiles.findIndex(t => t.id === id);
          if (i < 0) return;
          release(tiles[i]); tiles.splice(i, 1);
          const el = canvas.querySelector('[data-id="' + id + '"]');
          if (el) { clearInterval(el._iv); el._cleanup?.(); el.remove(); }
          sched(); syncHint();
        }

        // ── Toolbox ──────────────────────────────────────────────────
        function showBox(x, y) {
          toolbox.style.left = Math.min(x, innerWidth-420) + 'px';
          toolbox.style.top  = Math.min(y, innerHeight-80) + 'px';
          toolbox.classList.remove('hidden');
        }
        function hideBox() { toolbox.classList.add('hidden'); }

        document.getElementById('tb-note').addEventListener('click', () => startPlace('note'));
        document.getElementById('tb-sc').addEventListener('click',   () => startPlace('shortcut'));
        document.getElementById('tb-cl').addEventListener('click',   () => startPlace('clock'));
        document.getElementById('tb-lbl').addEventListener('click',  () => startPlace('label'));
        document.getElementById('tb-ln').addEventListener('click',   () => startPlace('line'));
        document.getElementById('tb-tmr').addEventListener('click',  () => startPlace('timer'));
        document.getElementById('tb-rec').addEventListener('click',  () => startPlace('recent'));
        document.getElementById('tb-ph').addEventListener('click',   () => startPlace('photo'));

        // ── Placement mode ───────────────────────────────────────────
        function startPlace(type) {
          hideBox(); placing = type; document.body.classList.add('placing');
          const sz = SETUP[type] || DEF[type];
          ghost = document.createElement('div');
          ghost.className = 'ghost';
          ghost.style.width  = g2p(sz.w) + 'px';
          ghost.style.height = g2p(sz.h) + 'px';
          canvas.appendChild(ghost);
        }

        canvas.addEventListener('mousemove', e => {
          if (!placing || !ghost) return;
          const r = canvas.getBoundingClientRect();
          const gx = p2g(e.clientX - r.left), gy = p2g(e.clientY - r.top);
          const sz = SETUP[placing] || DEF[placing];
          ghost.style.left = g2p(gx) + 'px';
          ghost.style.top  = g2p(gy) + 'px';
          ghost.classList.toggle('no', !free(gx, gy, sz.w, sz.h));
        });

        canvas.addEventListener('click', e => {
          if (!placing) return;
          if (e.target.closest('.tile')) return;
          const r = canvas.getBoundingClientRect();
          const gx = p2g(e.clientX - r.left), gy = p2g(e.clientY - r.top);
          if (ghost) { ghost.remove(); ghost = null; }
          document.body.classList.remove('placing');
          addTile(placing, gx, gy);
          placing = null;
        });

        canvas.addEventListener('contextmenu', e => {
          if (e.target.closest('.tile')) return;
          e.preventDefault();
          showBox(e.clientX, e.clientY);
        });

        document.addEventListener('mousedown', e => {
          if (!toolbox.classList.contains('hidden') && !toolbox.contains(e.target)) hideBox();
        });

        document.addEventListener('keydown', e => {
          if (e.key === 'Escape') {
            hideBox();
            if (placing) {
              placing = null;
              if (ghost) { ghost.remove(); ghost = null; }
              document.body.classList.remove('placing');
            }
          }
        });

        // ── WebMessage ───────────────────────────────────────────────
        window.chrome?.webview?.addEventListener('message', e => {
          try {
            const msg = JSON.parse(e.data);
            if (msg.type !== 'pegboardData') return;
            tiles = msg.tiles || [];
            occ.clear();
            canvas.querySelectorAll('.tile').forEach(el => { clearInterval(el._iv); el._cleanup?.(); el.remove(); });
            tiles.forEach(t => { claim(t); mkTile(t); }); syncHint();
          } catch {}
        });

        post({ type: 'loadPegboard' });
      </script>
    </body>
    </html>
    """;

    public static string History(IReadOnlyList<HistoryItem> history)
    {
        string rows = history.Count == 0
            ? """<tr><td colspan="4" class="h-empty">No history yet.</td></tr>"""
            : string.Join(Environment.NewLine, history.Select(item => $$"""
              <tr class="h-row">
                <td class="h-title"><button class="h-link" data-url="{{Attr(item.Url)}}">{{Html(item.Title)}}</button></td>
                <td class="h-url"><button class="h-link h-url-text" data-url="{{Attr(item.Url)}}">{{Html(item.Url)}}</button></td>
                <td class="h-time">{{Html(FormatTimestamp(item.LastVisitedUtc))}}</td>
                <td class="h-del"><button class="h-delete" data-delete-history="{{Attr(item.Id)}}">×</button></td>
              </tr>
              """));

        return Page("History", $$"""
        <style>
          .h-shell { width:100%;max-width:900px;margin:0 auto;padding:48px 24px 48px; }
          .h-header { display:flex;align-items:center;gap:16px;margin-bottom:20px; }
          .h-header h1 { font-size:13px;font-weight:400;letter-spacing:0.18em;text-transform:uppercase;color:rgba(255,255,255,0.35);flex-shrink:0; }
          .h-count { flex:1;text-align:center;font-size:11px;color:rgba(255,255,255,0.20);letter-spacing:0.05em; }
          .h-table { width:100%;border-collapse:collapse;table-layout:fixed; }
          .h-row { height:40px;border-bottom:1px solid rgba(255,255,255,0.06); }
          .h-row:last-child { border-bottom:none; }
          .h-title { width:35%;overflow:hidden;padding:0 10px 0 0; }
          .h-url { width:40%;overflow:hidden;padding:0 10px; }
          .h-time { width:100px;text-align:right;font-size:11px;color:rgba(255,255,255,0.25);white-space:nowrap;padding:0 8px 0 0; }
          .h-del { width:32px;text-align:center; }
          .h-link { display:block;width:100%;background:transparent;padding:0;text-align:left;font-size:13px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;cursor:pointer;color:rgba(255,255,255,0.80); }
          .h-url-text { color:rgba(255,255,255,0.30);font-size:11px; }
          .h-link:hover { color:rgba(255,255,255,1); }
          .h-url-text:hover { color:rgba(255,255,255,0.55); }
          .h-delete { width:32px;height:32px;background:transparent;border:none;font-size:16px;color:rgba(255,255,255,0.40);cursor:pointer;padding:0;line-height:32px;text-align:center;border-radius:6px; }
          .h-delete:hover { color:rgba(255,255,255,0.80); }
          .h-empty { padding:32px 0;font-size:13px;color:rgba(255,255,255,0.25);text-align:center; }
        </style>
        <div class="h-shell">
          <div class="h-header">
            <h1>History</h1>
            <span class="h-count">{{history.Count.ToString(CultureInfo.InvariantCulture)}} visits</span>
            <button class="primary-action" data-action="clearHistory">Clear all</button>
          </div>
          <table class="h-table"><tbody>{{rows}}</tbody></table>
        </div>
        """);
    }

    public static string Bookmarks(IReadOnlyList<BookmarkItem> bookmarks)
    {
        string rows = bookmarks.Count == 0
            ? """<div class="empty">No bookmarks yet.</div>"""
            : string.Join(Environment.NewLine, bookmarks.Select(item => $$"""
              <article class="list-row">
                <button class="row-main" data-url="{{Attr(item.Url)}}">
                  <strong>{{Html(item.Title)}}</strong>
                  <span>{{Html(item.Url)}}</span>
                </button>
                <button class="small-action" data-delete-bookmark="{{Attr(item.Id)}}">Delete</button>
              </article>
              """));

        return Page("Bookmarks", $$"""
        <main class="page-shell">
          <header class="page-header">
            <div><h1>Bookmarks</h1><p>{{bookmarks.Count.ToString(CultureInfo.InvariantCulture)}} saved</p></div>
          </header>
          <section class="list-stack">{{rows}}</section>
        </main>
        """);
    }

    public static string Settings(BrowserProfile profile)
    {
        string engineButtons = string.Join(Environment.NewLine, SearchEngine.All.Select(engine =>
        {
            string selected = engine.Name == profile.Settings.SearchEngine ? " selected" : "";
            return $$"""
              <button class="choice{{selected}}" data-engine="{{Attr(engine.Name)}}">
                <strong>{{Html(engine.Name)}}</strong>
                <span>{{Html(engine.HomeUrl)}}</span>
              </button>
              """;
        }));

        string adBlockClass = profile.AdBlockEnabled ? " on" : "";

        return Page("Settings", $$"""
        <main class="page-shell">
          <header class="page-header"><div><h1>Settings</h1><p>Flint</p></div></header>
          <nav class="tabs" aria-label="Settings">
            <button class="active" data-tab="search">Search</button>
            <button data-tab="data">Data</button>
            <button data-tab="features">Features</button>
            <button data-tab="about">About</button>
          </nav>
          <section class="tab-panel active" id="tab-search">
            <div class="choice-grid">{{engineButtons}}</div>
          </section>
          <section class="tab-panel" id="tab-data">
            <div class="settings-row" style="margin-bottom:8px;">
              <div><strong>Download Folder</strong><span>{{Html(profile.DownloadFolder)}}</span></div>
              <button class="primary-action" data-action="changeDownloadFolder">Change</button>
            </div>
            <div class="settings-row">
              <div><strong>History</strong><span>{{profile.History.Count.ToString(CultureInfo.InvariantCulture)}} visits saved</span></div>
              <button class="primary-action" data-action="clearHistory">Clear</button>
            </div>
          </section>
          <section class="tab-panel" id="tab-features">
            <div class="settings-row">
              <div><strong>Ad Blocker</strong><span>Block ads and trackers using a host-based blocklist</span></div>
              <button class="toggle-track{{adBlockClass}}" data-adblock-toggle></button>
            </div>
          </section>
          <section class="tab-panel" id="tab-about">
            <div class="list-row" style="flex-direction:column;align-items:flex-start;gap:18px;padding:24px 20px;">
              <div>
                <strong style="font-size:15px;letter-spacing:0.08em;">Flint</strong>
                <span style="display:block;font-size:11px;color:rgba(255,255,255,0.28);margin-top:4px;letter-spacing:0.12em;text-transform:uppercase;">A minimal browser</span>
              </div>
              <p style="font-size:13px;line-height:1.75;color:rgba(255,255,255,0.55);">Flint was born from a simple idea — how stripped down can a browser be to ensure peak performance, zero telemetry, zero bloat.</p>
              <p style="font-size:13px;line-height:1.75;color:rgba(255,255,255,0.55);">No extensions. No sync. No accounts. No phoning home. Just a window to the web, powered by WebView2, with a transparent glass shell and nothing in the way.</p>
              <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;width:100%;">
                <div style="padding:12px 14px;background:rgba(255,255,255,0.04);border:1px solid rgba(255,255,255,0.10);border-radius:10px;"><span style="font-size:10px;letter-spacing:0.14em;text-transform:uppercase;color:rgba(255,255,255,0.25);">Engine</span><strong style="display:block;font-size:12px;margin-top:4px;color:rgba(255,255,255,0.65);">Chromium / WebView2</strong></div>
                <div style="padding:12px 14px;background:rgba(255,255,255,0.04);border:1px solid rgba(255,255,255,0.10);border-radius:10px;"><span style="font-size:10px;letter-spacing:0.14em;text-transform:uppercase;color:rgba(255,255,255,0.25);">Telemetry</span><strong style="display:block;font-size:12px;margin-top:4px;color:rgba(255,255,255,0.65);">None</strong></div>
                <div style="padding:12px 14px;background:rgba(255,255,255,0.04);border:1px solid rgba(255,255,255,0.10);border-radius:10px;"><span style="font-size:10px;letter-spacing:0.14em;text-transform:uppercase;color:rgba(255,255,255,0.25);">Platform</span><strong style="display:block;font-size:12px;margin-top:4px;color:rgba(255,255,255,0.65);">Windows / .NET 8</strong></div>
                <div style="padding:12px 14px;background:rgba(255,255,255,0.04);border:1px solid rgba(255,255,255,0.10);border-radius:10px;"><span style="font-size:10px;letter-spacing:0.14em;text-transform:uppercase;color:rgba(255,255,255,0.25);">Extensions</span><strong style="display:block;font-size:12px;margin-top:4px;color:rgba(255,255,255,0.65);">None</strong></div>
              </div>
            </div>
          </section>
        </main>
        """);
    }

    public static string Downloads(IReadOnlyList<DownloadEntry> downloads, string downloadFolder)
    {
        string rows = downloads.Count == 0
            ? """<tr><td colspan="5" class="dl-empty">No downloads yet.</td></tr>"""
            : string.Join(Environment.NewLine, downloads.Select(entry =>
            {
                string stateCell;
                if (entry.State == "Complete")
                    stateCell = $"""<span class="dl-state-ok">Complete</span> <button class="small-action" data-open-file="{Attr(entry.FilePath)}">Open</button>""";
                else if (entry.State is "Failed" or "Cancelled")
                    stateCell = $"""<span class="dl-state-err">{Html(entry.State)}</span>""";
                else
                    stateCell = "";

                return $$"""
                  <tr class="dl-row">
                    <td class="dl-name">{{Html(entry.FileName)}}</td>
                    <td class="dl-url">{{Html(entry.Url)}}</td>
                    <td class="dl-size">{{Html(FormatBytes(entry.TotalBytes))}}</td>
                    <td class="dl-time">{{Html(FormatTimestamp(entry.StartedAt))}}</td>
                    <td class="dl-actions">{{stateCell}} <button class="dl-remove" data-remove-download="{{Attr(entry.Id)}}">×</button></td>
                  </tr>
                  """;
            }));

        return Page("Downloads", $$"""
        <style>
          .dl-shell { width:100%;max-width:960px;margin:0 auto;padding:48px 24px; }
          .dl-header { display:flex;align-items:center;gap:16px;margin-bottom:16px; }
          .dl-header h1 { font-size:13px;font-weight:400;letter-spacing:0.18em;text-transform:uppercase;color:rgba(255,255,255,0.35); }
          .dl-folder { flex:1;font-size:11px;color:rgba(255,255,255,0.20);white-space:nowrap;overflow:hidden;text-overflow:ellipsis; }
          .dl-table { width:100%;border-collapse:collapse;table-layout:fixed; }
          .dl-row { height:40px;border-bottom:1px solid rgba(255,255,255,0.06); }
          .dl-row:last-child { border-bottom:none; }
          .dl-name { width:22%;font-size:13px;color:rgba(255,255,255,0.80);overflow:hidden;text-overflow:ellipsis;white-space:nowrap;padding:0 10px 0 0; }
          .dl-url { width:28%;font-size:11px;color:rgba(255,255,255,0.28);overflow:hidden;text-overflow:ellipsis;white-space:nowrap;padding:0 10px; }
          .dl-size { width:80px;font-size:11px;color:rgba(255,255,255,0.28);white-space:nowrap;text-align:right;padding:0 10px; }
          .dl-time { width:110px;font-size:11px;color:rgba(255,255,255,0.25);white-space:nowrap;text-align:right;padding:0 10px; }
          .dl-actions { width:1%;white-space:nowrap;text-align:right;padding:0 0 0 8px; }
          .dl-bar-wrap { display:inline-block;width:80px;height:4px;background:rgba(255,255,255,0.08);border-radius:2px;vertical-align:middle; }
          .dl-bar-fill { height:100%;background:rgba(116,247,255,0.70);border-radius:2px; }
          .dl-state-ok { font-size:11px;color:rgba(116,247,255,0.80); }
          .dl-state-err { font-size:11px;color:rgba(255,100,100,0.70); }
          .dl-remove { width:28px;height:28px;background:transparent;border:none;font-size:16px;color:rgba(255,255,255,0.30);cursor:pointer;padding:0;line-height:28px;text-align:center;border-radius:6px;vertical-align:middle;margin-left:4px; }
          .dl-remove:hover { color:rgba(255,255,255,0.70); }
          .dl-empty { padding:32px 0;font-size:13px;color:rgba(255,255,255,0.25);text-align:center; }
        </style>
        <div class="dl-shell">
          <div class="dl-header">
            <h1>Downloads</h1>
            <span class="dl-folder">{{Html(downloadFolder)}}</span>
            <button class="primary-action" data-action="changeDownloadFolder">Change folder</button>
          </div>
          <table class="dl-table"><tbody>{{rows}}</tbody></table>
        </div>
        """);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "—";
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }

    private static string FormatTimestamp(DateTime timestamp) =>
        timestamp.ToString("MMM d, h:mm tt", CultureInfo.CurrentCulture);

    private static string Page(string title, string body) => $$"""
    <!doctype html>
    <html lang="en">
    <head>
      <meta charset="utf-8" />
      <meta name="viewport" content="width=device-width, initial-scale=1" />
      <title>{{Html(title)}}</title>
      <style>
        :root { color-scheme: dark; }
        *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
        html, body { height: 100%; background: transparent !important; font-family: "Segoe UI", system-ui, sans-serif; color: rgba(255,255,255,0.85); }
        body { position: relative; overflow-x: hidden; }
        button, input { font: inherit; color: inherit; border: none; cursor: pointer; }
        .corner { position: fixed; top: 22px; font-size: 12px; color: rgba(255,255,255,0.20); letter-spacing: 0.25em; user-select: none; pointer-events: none; }
        .corner-left { left: 28px; }
        .page-shell { max-width: 640px; margin: 0 auto; padding: 56px 24px 48px; }
        .page-header { display: flex; align-items: center; justify-content: space-between; gap: 16px; margin-bottom: 20px; }
        .page-header h1 { font-size: 13px; font-weight: 400; letter-spacing: 0.18em; text-transform: uppercase; color: rgba(255,255,255,0.35); }
        .page-header p { font-size: 11px; color: rgba(255,255,255,0.20); margin-top: 4px; letter-spacing: 0.05em; }
        .list-stack { display: grid; gap: 8px; }
        .list-row, .settings-row, .empty { display: flex; align-items: center; justify-content: space-between; gap: 16px; min-height: 62px; padding: 12px 16px; background: rgba(255,255,255,0.06); border: 1px solid rgba(255,255,255,0.12); border-radius: 14px; backdrop-filter: blur(5px); -webkit-backdrop-filter: blur(5px); transition: background 150ms ease, border-color 150ms ease; }
        .list-row:hover { background: rgba(255,255,255,0.09); border-color: rgba(255,255,255,0.20); }
        .row-main { min-width: 0; flex: 1; text-align: left; background: transparent; padding: 0; }
        .row-main strong { display: block; font-size: 13px; font-weight: 500; }
        .row-main span { display: block; font-size: 11px; color: rgba(255,255,255,0.38); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; margin-top: 2px; }
        .primary-action, .small-action { flex-shrink: 0; height: 32px; padding: 0 14px; font-size: 11px; letter-spacing: 0.05em; background: rgba(255,255,255,0.06); border: 1px solid rgba(255,255,255,0.12); border-radius: 8px; color: rgba(255,255,255,0.50); backdrop-filter: blur(5px); transition: background 150ms ease, border-color 150ms ease; }
        .primary-action:hover, .small-action:hover { background: rgba(255,255,255,0.10); border-color: rgba(255,255,255,0.22); }
        .tabs { display: flex; gap: 8px; margin-bottom: 14px; }
        .tabs button { height: 32px; padding: 0 16px; font-size: 11px; letter-spacing: 0.08em; text-transform: uppercase; background: rgba(255,255,255,0.06); border: 1px solid rgba(255,255,255,0.12); border-radius: 8px; color: rgba(255,255,255,0.40); backdrop-filter: blur(5px); transition: background 150ms ease, border-color 150ms ease, color 150ms ease; }
        .tabs button.active { background: rgba(255,255,255,0.10); border-color: rgba(255,255,255,0.22); color: rgba(255,255,255,0.85); }
        .tabs button:hover:not(.active) { background: rgba(255,255,255,0.09); color: rgba(255,255,255,0.60); }
        .tab-panel { display: none; } .tab-panel.active { display: block; }
        .choice-grid { display: grid; grid-template-columns: repeat(2,1fr); gap: 8px; }
        .choice { min-height: 72px; padding: 14px 16px; text-align: left; background: rgba(255,255,255,0.06); border: 1px solid rgba(255,255,255,0.12); border-radius: 14px; backdrop-filter: blur(5px); -webkit-backdrop-filter: blur(5px); transition: background 150ms ease, border-color 150ms ease; }
        .choice strong { display: block; font-size: 13px; font-weight: 500; }
        .choice span { display: block; font-size: 11px; color: rgba(255,255,255,0.38); margin-top: 3px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
        .choice:hover:not(.selected) { background: rgba(255,255,255,0.09); border-color: rgba(255,255,255,0.20); }
        .choice.selected { background: rgba(255,255,255,0.10); border-color: rgba(255,255,255,0.28); }
        .settings-row span { display: block; font-size: 11px; color: rgba(255,255,255,0.38); margin-top: 2px; }
        .toggle-track { position: relative; width: 44px; height: 24px; border-radius: 12px; background: rgba(255,255,255,0.10); border: 1px solid rgba(255,255,255,0.16); transition: background 200ms ease, border-color 200ms ease; cursor: pointer; flex-shrink: 0; padding: 0; }
        .toggle-track.on { background: rgba(116,247,255,0.22); border-color: rgba(116,247,255,0.42); }
        .toggle-track::after { content: ''; position: absolute; top: 3px; left: 3px; width: 16px; height: 16px; border-radius: 50%; background: rgba(255,255,255,0.45); transition: transform 200ms ease, background 200ms ease; }
        .toggle-track.on::after { transform: translateX(20px); background: rgba(116,247,255,0.90); }
      </style>
    </head>
    <body>
      <div class="corner corner-left">{{title.ToLowerInvariant()}}</div>
      {{body}}
      <script>
        const post = (payload) => {
          if (window.chrome && window.chrome.webview)
            window.chrome.webview.postMessage(JSON.stringify(payload));
        };
        document.addEventListener("click", (event) => {
          const el = event.target.closest("[data-action],[data-url],[data-query],[data-delete-history],[data-delete-bookmark],[data-engine],[data-tab],[data-adblock-toggle],[data-open-file],[data-remove-download]");
          if (!el) return;
          if (el.dataset.tab) {
            document.querySelectorAll("[data-tab]").forEach(t => t.classList.toggle("active", t.dataset.tab === el.dataset.tab));
            document.querySelectorAll(".tab-panel").forEach(p => p.classList.toggle("active", p.id === "tab-" + el.dataset.tab));
            return;
          }
          if (el.hasAttribute('data-adblock-toggle')) {
            const on = !el.classList.contains('on');
            el.classList.toggle('on', on);
            post({ type:'setAdBlock', enabled:on }); return;
          }
          if (el.dataset.action) post({ type: el.dataset.action });
          if (el.dataset.url) post({ type: "openUrl", url: el.dataset.url });
          if (el.dataset.query) post({ type: "search", query: el.dataset.query });
          if (el.dataset.deleteHistory) post({ type: "deleteHistory", id: el.dataset.deleteHistory });
          if (el.dataset.deleteBookmark) post({ type: "deleteBookmark", id: el.dataset.deleteBookmark });
          if (el.dataset.engine) post({ type: "setSearchEngine", engine: el.dataset.engine });
          if (el.dataset.openFile) post({ type: "openFile", path: el.dataset.openFile });
          if (el.dataset.removeDownload) post({ type: "removeDownload", id: el.dataset.removeDownload });
        });
      </script>
    </body>
    </html>
    """;

    private static string FormatTimestamp(DateTimeOffset timestamp) =>
        timestamp.ToLocalTime().ToString("MMM d, h:mm tt", CultureInfo.CurrentCulture);

    private static string Html(string value) => WebUtility.HtmlEncode(value);
    private static string Attr(string value) => WebUtility.HtmlEncode(value);
}

using System.Globalization;
using System.Net;

namespace Flint;

public static class ShellPages
{
    public static string Home(SearchEngine searchEngine) => """
    <!doctype html>
    <html lang="en">
    <head>
      <meta charset="utf-8" />
      <meta name="viewport" content="width=device-width, initial-scale=1" />
      <title>Flint</title>
      <style>
        :root { color-scheme: dark; }

        *, *::before, *::after {
          box-sizing: border-box;
          margin: 0;
          padding: 0;
        }

        html, body {
          height: 100%;
          background: transparent !important;
          font-family: "Segoe UI", system-ui, sans-serif;
        }

        body {
          position: relative;
          overflow-x: hidden;
        }

        .corner {
          position: fixed;
          top: 22px;
          font-size: 12px;
          color: rgba(255, 255, 255, 0.20);
          letter-spacing: 0.25em;
          user-select: none;
          pointer-events: none;
        }

        .corner-left { left: 28px; }

        .corner-right {
          right: 28px;
          letter-spacing: 0.08em;
          font-variant-numeric: tabular-nums;
        }

        .home {
          position: absolute;
          top: 45vh;
          left: 50%;
          transform: translate(-50%, -50%);
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: 32px;
          width: 100%;
          padding: 0 24px;
        }

        .search {
          -webkit-appearance: none;
          appearance: none;
          display: block;
          width: 100%;
          max-width: 520px;
          height: 44px;
          padding: 0 16px;
          background: rgba(255, 255, 255, 0.06);
          border: 1px solid rgba(255, 255, 255, 0.10);
          border-radius: 10px;
          outline: none;
          color: #fff;
          font-size: 14px;
          font-family: inherit;
          backdrop-filter: blur(5px);
          -webkit-backdrop-filter: blur(5px);
          transition: border-color 150ms ease;
        }

        .search::placeholder {
          color: rgba(255, 255, 255, 0.25);
        }

        .search:focus {
          border-color: rgba(255, 255, 255, 0.22);
        }

        .cards {
          display: flex;
          flex-wrap: nowrap;
          gap: 12px;
          padding: 20px 0;
        }

        .card {
          width: 22vw;
          max-width: 180px;
          height: 160px;
          flex-shrink: 0;
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: space-between;
          padding: 28px 0 22px;
          background: rgba(255, 255, 255, 0.06);
          border: 1px solid rgba(255, 255, 255, 0.15);
          border-radius: 16px;
          color: inherit;
          cursor: pointer;
          backdrop-filter: blur(5px);
          -webkit-backdrop-filter: blur(5px);
          transition: background 150ms ease, border-color 150ms ease;
          user-select: none;
        }

        .card:hover {
          background: rgba(255, 255, 255, 0.09);
          border-color: rgba(255, 255, 255, 0.22);
        }

        .card svg { opacity: 0.5; }

        .card span {
          font-size: 11px;
          letter-spacing: 0.12em;
          text-transform: uppercase;
          color: rgba(255, 255, 255, 0.50);
        }

        @media (max-width: 600px) {
          .corner { display: none; }
          .card { width: 20vw; height: 120px; padding: 18px 0 14px; }
          .card svg { width: 22px; height: 22px; }
        }

        @media (max-width: 380px) {
          .cards { gap: 6px; }
          .card { width: 18vw; }
        }
      </style>
    </head>
    <body>
      <div class="corner corner-left">flint</div>
      <div class="corner corner-right" id="clock"></div>

      <div class="home">
        <input class="search" id="q" type="text" autocomplete="off" spellcheck="false"
               placeholder="search or type a url" />

        <div class="cards">
          <button class="card" data-action="openSearch">
            <svg width="28" height="28" viewBox="0 0 28 28" fill="none" xmlns="http://www.w3.org/2000/svg">
              <circle cx="14" cy="14" r="9.5" stroke="white" stroke-width="1.5"/>
              <ellipse cx="14" cy="14" rx="4.5" ry="9.5" stroke="white" stroke-width="1.5"/>
              <line x1="4.5" y1="14" x2="23.5" y2="14" stroke="white" stroke-width="1.5" stroke-linecap="round"/>
            </svg>
            <span>web</span>
          </button>

          <button class="card" data-query="social">
            <svg width="28" height="28" viewBox="0 0 28 28" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M5 8C5 7.45 5.45 7 6 7H22C22.55 7 23 7.45 23 8V17C23 17.55 22.55 18 22 18H16L11 22V18H6C5.45 18 5 17.55 5 17V8Z" stroke="white" stroke-width="1.5" stroke-linejoin="round"/>
            </svg>
            <span>social</span>
          </button>

          <button class="card" data-query="streaming">
            <svg width="28" height="28" viewBox="0 0 28 28" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="4" y="7" width="20" height="14" rx="2" stroke="white" stroke-width="1.5"/>
              <path d="M11.5 11.5L18 14L11.5 16.5V11.5Z" fill="white"/>
            </svg>
            <span>media</span>
          </button>

          <button class="card" data-query="shopping">
            <svg width="28" height="28" viewBox="0 0 28 28" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M7 11H21L19.5 21H8.5L7 11Z" stroke="white" stroke-width="1.5" stroke-linejoin="round"/>
              <path d="M10.5 11C10.5 11 10.5 7.5 14 7.5C17.5 7.5 17.5 11 17.5 11" stroke="white" stroke-width="1.5" stroke-linecap="round"/>
            </svg>
            <span>shop</span>
          </button>
        </div>
      </div>

      <script>
        const post = p => window.chrome?.webview?.postMessage(JSON.stringify(p));

        (function tick() {
          const d = new Date();
          const hh = String(d.getHours()).padStart(2, '0');
          const mm = String(d.getMinutes()).padStart(2, '0');
          document.getElementById('clock').textContent = hh + ':' + mm;
          const ms = (60 - d.getSeconds()) * 1000 - d.getMilliseconds();
          setTimeout(tick, ms);
        })();

        function isUrl(s) {
          return /^https?:\/\//i.test(s) || (!s.includes(' ') && /\.[a-z]{2,}/i.test(s));
        }

        document.getElementById('q').addEventListener('keydown', function(e) {
          if (e.key !== 'Enter') return;
          const v = this.value.trim();
          if (!v) return;
          if (isUrl(v)) post({ type: 'openUrl', url: /^https?:\/\//i.test(v) ? v : 'https://' + v });
          else post({ type: 'search', query: v });
        });

        document.querySelectorAll('.card').forEach(function(card) {
          card.addEventListener('click', function() {
            if (card.dataset.action) post({ type: card.dataset.action });
            else if (card.dataset.query) post({ type: 'search', query: card.dataset.query });
          });
        });
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
          .h-shell {
            width: 100%;
            max-width: 900px;
            margin: 0 auto;
            padding: 48px 24px 48px;
          }
          .h-header {
            display: flex;
            align-items: center;
            gap: 16px;
            margin-bottom: 20px;
          }
          .h-header h1 {
            font-size: 13px;
            font-weight: 400;
            letter-spacing: 0.18em;
            text-transform: uppercase;
            color: rgba(255,255,255,0.35);
            flex-shrink: 0;
          }
          .h-count {
            flex: 1;
            text-align: center;
            font-size: 11px;
            color: rgba(255,255,255,0.20);
            letter-spacing: 0.05em;
          }
          .h-table {
            width: 100%;
            border-collapse: collapse;
            table-layout: fixed;
          }
          .h-row {
            height: 40px;
            border-bottom: 1px solid rgba(255,255,255,0.06);
          }
          .h-row:last-child { border-bottom: none; }
          .h-title {
            width: 35%;
            overflow: hidden;
            padding: 0 10px 0 0;
          }
          .h-url {
            width: 40%;
            overflow: hidden;
            padding: 0 10px;
          }
          .h-time {
            width: 100px;
            text-align: right;
            font-size: 11px;
            color: rgba(255,255,255,0.25);
            white-space: nowrap;
            padding: 0 8px 0 0;
          }
          .h-del {
            width: 32px;
            text-align: center;
          }
          .h-link {
            display: block;
            width: 100%;
            background: transparent;
            padding: 0;
            text-align: left;
            font-size: 13px;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            cursor: pointer;
            color: rgba(255,255,255,0.80);
          }
          .h-url-text { color: rgba(255,255,255,0.30); font-size: 11px; }
          .h-link:hover { color: rgba(255,255,255,1); }
          .h-url-text:hover { color: rgba(255,255,255,0.55); }
          .h-delete {
            width: 32px;
            height: 32px;
            background: transparent;
            border: none;
            font-size: 16px;
            color: rgba(255,255,255,0.40);
            cursor: pointer;
            padding: 0;
            line-height: 32px;
            text-align: center;
            border-radius: 6px;
          }
          .h-delete:hover { color: rgba(255,255,255,0.80); }
          .h-empty {
            padding: 32px 0;
            font-size: 13px;
            color: rgba(255,255,255,0.25);
            text-align: center;
          }
        </style>
        <div class="h-shell">
          <div class="h-header">
            <h1>History</h1>
            <span class="h-count">{{history.Count.ToString(CultureInfo.InvariantCulture)}} visits</span>
            <button class="primary-action" data-action="clearHistory">Clear all</button>
          </div>
          <table class="h-table">
            <tbody>
              {{rows}}
            </tbody>
          </table>
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
            <div>
              <h1>Bookmarks</h1>
              <p>{{bookmarks.Count.ToString(CultureInfo.InvariantCulture)}} saved</p>
            </div>
          </header>
          <section class="list-stack">
            {{rows}}
          </section>
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
          <header class="page-header">
            <div>
              <h1>Settings</h1>
              <p>Flint</p>
            </div>
          </header>

          <nav class="tabs" aria-label="Settings">
            <button class="active" data-tab="search">Search</button>
            <button data-tab="data">Data</button>
            <button data-tab="features">Features</button>
            <button data-tab="about">About</button>
          </nav>

          <section class="tab-panel active" id="tab-search">
            <div class="choice-grid">
              {{engineButtons}}
            </div>
          </section>

          <section class="tab-panel" id="tab-data">
            <div class="settings-row" style="margin-bottom:8px;">
              <div>
                <strong>Download Folder</strong>
                <span>{{Html(profile.DownloadFolder)}}</span>
              </div>
              <button class="primary-action" data-action="changeDownloadFolder">Change</button>
            </div>
            <div class="settings-row">
              <div>
                <strong>History</strong>
                <span>{{profile.History.Count.ToString(CultureInfo.InvariantCulture)}} visits saved</span>
              </div>
              <button class="primary-action" data-action="clearHistory">Clear</button>
            </div>
          </section>

          <section class="tab-panel" id="tab-features">
            <div class="settings-row">
              <div>
                <strong>Ad Blocker</strong>
                <span>Block ads and trackers using a host-based blocklist</span>
              </div>
              <button class="toggle-track{{adBlockClass}}" data-adblock-toggle></button>
            </div>
          </section>

          <section class="tab-panel" id="tab-about">
            <div class="list-row" style="flex-direction:column; align-items:flex-start; gap:18px; padding:24px 20px;">
              <div>
                <strong style="font-size:15px; letter-spacing:0.08em;">Flint</strong>
                <span style="display:block; font-size:11px; color:rgba(255,255,255,0.28); margin-top:4px; letter-spacing:0.12em; text-transform:uppercase;">A minimal browser</span>
              </div>
              <p style="font-size:13px; line-height:1.75; color:rgba(255,255,255,0.55);">
                Flint was born from a simple idea — how stripped down can a browser be to ensure peak performance, zero telemetry, zero bloat.
              </p>
              <p style="font-size:13px; line-height:1.75; color:rgba(255,255,255,0.55);">
                No extensions. No sync. No accounts. No phoning home. Just a window to the web, powered by WebView2, with a transparent glass shell and nothing in the way.
              </p>
              <div style="display:grid; grid-template-columns:1fr 1fr; gap:8px; width:100%;">
                <div style="padding:12px 14px; background:rgba(255,255,255,0.04); border:1px solid rgba(255,255,255,0.10); border-radius:10px;">
                  <span style="font-size:10px; letter-spacing:0.14em; text-transform:uppercase; color:rgba(255,255,255,0.25);">Engine</span>
                  <strong style="display:block; font-size:12px; margin-top:4px; color:rgba(255,255,255,0.65);">Chromium / WebView2</strong>
                </div>
                <div style="padding:12px 14px; background:rgba(255,255,255,0.04); border:1px solid rgba(255,255,255,0.10); border-radius:10px;">
                  <span style="font-size:10px; letter-spacing:0.14em; text-transform:uppercase; color:rgba(255,255,255,0.25);">Telemetry</span>
                  <strong style="display:block; font-size:12px; margin-top:4px; color:rgba(255,255,255,0.65);">None</strong>
                </div>
                <div style="padding:12px 14px; background:rgba(255,255,255,0.04); border:1px solid rgba(255,255,255,0.10); border-radius:10px;">
                  <span style="font-size:10px; letter-spacing:0.14em; text-transform:uppercase; color:rgba(255,255,255,0.25);">Platform</span>
                  <strong style="display:block; font-size:12px; margin-top:4px; color:rgba(255,255,255,0.65);">Windows / .NET 8</strong>
                </div>
                <div style="padding:12px 14px; background:rgba(255,255,255,0.04); border:1px solid rgba(255,255,255,0.10); border-radius:10px;">
                  <span style="font-size:10px; letter-spacing:0.14em; text-transform:uppercase; color:rgba(255,255,255,0.25);">Extensions</span>
                  <strong style="display:block; font-size:12px; margin-top:4px; color:rgba(255,255,255,0.65);">None</strong>
                </div>
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
                double pct = entry.TotalBytes > 0 ? Math.Clamp(100.0 * entry.ReceivedBytes / entry.TotalBytes, 0, 100) : 0;
                string stateCell;
                if (entry.State == "Complete")
                    stateCell = $"""<span class="dl-state-ok">Complete</span> <button class="small-action" data-open-file="{Attr(entry.FilePath)}">Open</button>""";
                else if (entry.State is "Failed" or "Cancelled")
                    stateCell = $"""<span class="dl-state-err">{Html(entry.State)}</span>""";
                else
                    stateCell = $"""<div class="dl-bar-wrap"><div class="dl-bar-fill" style="width:{pct:F0}%"></div></div>""";

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
          .dl-shell {
            width: 100%;
            max-width: 960px;
            margin: 0 auto;
            padding: 48px 24px;
          }
          .dl-header {
            display: flex;
            align-items: center;
            gap: 16px;
            margin-bottom: 16px;
          }
          .dl-header h1 {
            font-size: 13px;
            font-weight: 400;
            letter-spacing: 0.18em;
            text-transform: uppercase;
            color: rgba(255,255,255,0.35);
          }
          .dl-folder {
            flex: 1;
            font-size: 11px;
            color: rgba(255,255,255,0.20);
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
          }
          .dl-table {
            width: 100%;
            border-collapse: collapse;
            table-layout: fixed;
          }
          .dl-row {
            height: 40px;
            border-bottom: 1px solid rgba(255,255,255,0.06);
          }
          .dl-row:last-child { border-bottom: none; }
          .dl-name {
            width: 22%;
            font-size: 13px;
            color: rgba(255,255,255,0.80);
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            padding: 0 10px 0 0;
          }
          .dl-url {
            width: 28%;
            font-size: 11px;
            color: rgba(255,255,255,0.28);
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
            padding: 0 10px;
          }
          .dl-size {
            width: 80px;
            font-size: 11px;
            color: rgba(255,255,255,0.28);
            white-space: nowrap;
            text-align: right;
            padding: 0 10px;
          }
          .dl-time {
            width: 110px;
            font-size: 11px;
            color: rgba(255,255,255,0.25);
            white-space: nowrap;
            text-align: right;
            padding: 0 10px;
          }
          .dl-actions {
            width: 1%;
            white-space: nowrap;
            text-align: right;
            padding: 0 0 0 8px;
          }
          .dl-bar-wrap {
            display: inline-block;
            width: 80px;
            height: 4px;
            background: rgba(255,255,255,0.08);
            border-radius: 2px;
            vertical-align: middle;
          }
          .dl-bar-fill {
            height: 100%;
            background: rgba(116,247,255,0.70);
            border-radius: 2px;
          }
          .dl-state-ok { font-size: 11px; color: rgba(116,247,255,0.80); }
          .dl-state-err { font-size: 11px; color: rgba(255,100,100,0.70); }
          .dl-remove {
            width: 28px;
            height: 28px;
            background: transparent;
            border: none;
            font-size: 16px;
            color: rgba(255,255,255,0.30);
            cursor: pointer;
            padding: 0;
            line-height: 28px;
            text-align: center;
            border-radius: 6px;
            vertical-align: middle;
            margin-left: 4px;
          }
          .dl-remove:hover { color: rgba(255,255,255,0.70); }
          .dl-empty {
            padding: 32px 0;
            font-size: 13px;
            color: rgba(255,255,255,0.25);
            text-align: center;
          }
        </style>
        <div class="dl-shell">
          <div class="dl-header">
            <h1>Downloads</h1>
            <span class="dl-folder">{{Html(downloadFolder)}}</span>
            <button class="primary-action" data-action="changeDownloadFolder">Change folder</button>
          </div>
          <table class="dl-table">
            <tbody>
              {{rows}}
            </tbody>
          </table>
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

        *, *::before, *::after {
          box-sizing: border-box;
          margin: 0;
          padding: 0;
        }

        html, body {
          height: 100%;
          background: transparent !important;
          font-family: "Segoe UI", system-ui, sans-serif;
          color: rgba(255, 255, 255, 0.85);
        }

        body {
          position: relative;
          overflow-x: hidden;
        }

        button, input {
          font: inherit;
          color: inherit;
          border: none;
          cursor: pointer;
        }

        .corner {
          position: fixed;
          top: 22px;
          font-size: 12px;
          color: rgba(255, 255, 255, 0.20);
          letter-spacing: 0.25em;
          user-select: none;
          pointer-events: none;
        }

        .corner-left { left: 28px; }

        .page-shell {
          max-width: 640px;
          margin: 0 auto;
          padding: 56px 24px 48px;
        }

        .page-header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          gap: 16px;
          margin-bottom: 20px;
        }

        .page-header h1 {
          font-size: 13px;
          font-weight: 400;
          letter-spacing: 0.18em;
          text-transform: uppercase;
          color: rgba(255, 255, 255, 0.35);
        }

        .page-header p {
          font-size: 11px;
          color: rgba(255, 255, 255, 0.20);
          margin-top: 4px;
          letter-spacing: 0.05em;
        }

        .list-stack {
          display: grid;
          gap: 8px;
        }

        .list-row,
        .settings-row,
        .empty {
          display: flex;
          align-items: center;
          justify-content: space-between;
          gap: 16px;
          min-height: 62px;
          padding: 12px 16px;
          background: rgba(255, 255, 255, 0.06);
          border: 1px solid rgba(255, 255, 255, 0.12);
          border-radius: 14px;
          backdrop-filter: blur(5px);
          -webkit-backdrop-filter: blur(5px);
          transition: background 150ms ease, border-color 150ms ease;
        }

        .list-row:hover {
          background: rgba(255, 255, 255, 0.09);
          border-color: rgba(255, 255, 255, 0.20);
        }

        .row-main {
          min-width: 0;
          flex: 1;
          text-align: left;
          background: transparent;
          padding: 0;
        }

        .row-main strong {
          display: block;
          font-size: 13px;
          font-weight: 500;
        }

        .row-main span {
          display: block;
          font-size: 11px;
          color: rgba(255, 255, 255, 0.38);
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
          margin-top: 2px;
        }

        .row-main small {
          display: block;
          font-size: 10px;
          color: rgba(255, 255, 255, 0.24);
          margin-top: 2px;
        }

        .primary-action,
        .small-action {
          flex-shrink: 0;
          height: 32px;
          padding: 0 14px;
          font-size: 11px;
          letter-spacing: 0.05em;
          background: rgba(255, 255, 255, 0.06);
          border: 1px solid rgba(255, 255, 255, 0.12);
          border-radius: 8px;
          color: rgba(255, 255, 255, 0.50);
          backdrop-filter: blur(5px);
          transition: background 150ms ease, border-color 150ms ease;
        }

        .primary-action:hover,
        .small-action:hover {
          background: rgba(255, 255, 255, 0.10);
          border-color: rgba(255, 255, 255, 0.22);
        }

        .tabs {
          display: flex;
          gap: 8px;
          margin-bottom: 14px;
        }

        .tabs button {
          height: 32px;
          padding: 0 16px;
          font-size: 11px;
          letter-spacing: 0.08em;
          text-transform: uppercase;
          background: rgba(255, 255, 255, 0.06);
          border: 1px solid rgba(255, 255, 255, 0.12);
          border-radius: 8px;
          color: rgba(255, 255, 255, 0.40);
          backdrop-filter: blur(5px);
          transition: background 150ms ease, border-color 150ms ease, color 150ms ease;
        }

        .tabs button.active {
          background: rgba(255, 255, 255, 0.10);
          border-color: rgba(255, 255, 255, 0.22);
          color: rgba(255, 255, 255, 0.85);
        }

        .tabs button:hover:not(.active) {
          background: rgba(255, 255, 255, 0.09);
          color: rgba(255, 255, 255, 0.60);
        }

        .tab-panel { display: none; }
        .tab-panel.active { display: block; }

        .choice-grid {
          display: grid;
          grid-template-columns: repeat(2, 1fr);
          gap: 8px;
        }

        .choice {
          min-height: 72px;
          padding: 14px 16px;
          text-align: left;
          background: rgba(255, 255, 255, 0.06);
          border: 1px solid rgba(255, 255, 255, 0.12);
          border-radius: 14px;
          backdrop-filter: blur(5px);
          -webkit-backdrop-filter: blur(5px);
          transition: background 150ms ease, border-color 150ms ease;
        }

        .choice strong {
          display: block;
          font-size: 13px;
          font-weight: 500;
        }

        .choice span {
          display: block;
          font-size: 11px;
          color: rgba(255, 255, 255, 0.38);
          margin-top: 3px;
          overflow: hidden;
          text-overflow: ellipsis;
          white-space: nowrap;
        }

        .choice:hover:not(.selected) {
          background: rgba(255, 255, 255, 0.09);
          border-color: rgba(255, 255, 255, 0.20);
        }

        .choice.selected {
          background: rgba(255, 255, 255, 0.10);
          border-color: rgba(255, 255, 255, 0.28);
        }

        .settings-row span {
          display: block;
          font-size: 11px;
          color: rgba(255, 255, 255, 0.38);
          margin-top: 2px;
        }

        .toggle-track {
          position: relative;
          width: 44px;
          height: 24px;
          border-radius: 12px;
          background: rgba(255, 255, 255, 0.10);
          border: 1px solid rgba(255, 255, 255, 0.16);
          transition: background 200ms ease, border-color 200ms ease;
          cursor: pointer;
          flex-shrink: 0;
          padding: 0;
        }

        .toggle-track.on {
          background: rgba(116, 247, 255, 0.22);
          border-color: rgba(116, 247, 255, 0.42);
        }

        .toggle-track::after {
          content: '';
          position: absolute;
          top: 3px;
          left: 3px;
          width: 16px;
          height: 16px;
          border-radius: 50%;
          background: rgba(255, 255, 255, 0.45);
          transition: transform 200ms ease, background 200ms ease;
        }

        .toggle-track.on::after {
          transform: translateX(20px);
          background: rgba(116, 247, 255, 0.90);
        }
      </style>
    </head>
    <body>
      <div class="corner corner-left">{{title.ToLowerInvariant()}}</div>
      {{body}}
      <script>
        const post = (payload) => {
          if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(JSON.stringify(payload));
          }
        };

        document.addEventListener("click", (event) => {
          const el = event.target.closest("[data-action], [data-url], [data-query], [data-delete-history], [data-delete-bookmark], [data-engine], [data-tab], [data-adblock-toggle], [data-open-file], [data-remove-download]");
          if (!el) return;

          if (el.dataset.tab) {
            document.querySelectorAll("[data-tab]").forEach(tab => tab.classList.toggle("active", tab.dataset.tab === el.dataset.tab));
            document.querySelectorAll(".tab-panel").forEach(panel => panel.classList.toggle("active", panel.id === "tab-" + el.dataset.tab));
            return;
          }

          if (el.hasAttribute('data-adblock-toggle')) {
            const on = !el.classList.contains('on');
            el.classList.toggle('on', on);
            post({ type: 'setAdBlock', enabled: on });
            return;
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

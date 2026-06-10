# Flint Changelog

## [Unreleased]

---

## [0.1.9] — 2026-06-10

### Fixed
- **Download dropdown layout rewritten** — replaced fixed 520 px height + global `yPos` arithmetic with explicit `Bounds` on every control; `ClientSize` is set to exactly `entries × 110 + 40 px` (footer); `showAllBtn` is stored as a field and repositioned in `UpdateDisplay()` so it always sits flush at the bottom with no empty space
- **Vertical text clipping in size/state labels** — label height was 16 px, too tight for Segoe UI 8.5 pt to render without clipping descenders; bumped to 22 px; `RowHeight` increased from 90 to 110 px to give all elements breathing room
- **Size label width** — widened from 190 to 210 px to accommodate longer strings without horizontal clipping
- **Live progress update format was always MB** — `BytesReceivedChanged` hardcoded `/ (1024 * 1024)` regardless of file size, so a 10 GB file showed `10240.0 MB`; replaced with an inline `Fmt()` local function using the same KB / MB / GB logic as `BuildSizeText`
- **Progress bar width formula** — was `pct * 0.8` which capped fill at 80 px on a 296 px track; corrected to `ratio * 296`
- **`Show all downloads` footer re-created on every refresh** — old code called `Controls.Clear()` nuking the footer then re-added it at `yPos`, causing drift; footer is now created once in `OnLoad` and only its `Bounds` are updated in `UpdateDisplay()`

## [0.1.8] — 2026-06-09

### Added
- **Download dropdown panel** — clicking the toolbar downloads button now opens a floating borderless `DownloadDropdownForm` (320 px wide) anchored below the button rather than navigating to `flint://downloads`; shows up to 10 most recent downloads; auto-dismisses when it loses focus via `OnDeactivate`
- **Per-entry download rows** — each row shows: filename (truncated to 40 chars with ellipsis), a 4 px progress bar (cyan `#00D4FF` fill while in progress, full-width on complete, red background on failure), an MB progress label (`x.0 MB of y.0 MB`), and a right-aligned percentage label
- **Real-time progress updates** — `BytesReceivedChanged` fires `BeginInvoke` to update the fill panel width and size/percent labels live while a download is in progress, via a `dropdownEntries` dictionary keyed by `entry.Id`
- **Open / dismiss buttons on completed rows** — `Open` launches the file with the default OS shell handler; `×` removes the entry from the in-memory list and refreshes the dropdown immediately
- **"Show all downloads" footer** — fixed footer button at the bottom of the dropdown navigates to `flint://downloads` and closes the panel
- **Active download badge on toolbar button** — the downloads `GlassButton` paints a cyan (`#00D4FF`) count badge in its top-right corner showing the number of in-progress downloads; repaints on `DownloadStarting` and `StateChanged`
- **Start toast on download begin** — `"{filename} downloading"` toast fires via `BeginInvoke` when a new download starts (in addition to the existing complete toast)
- **`ToggleDownloadDropdown()`** — toolbar button click toggles the dropdown open/closed; a second click while open closes it cleanly

### Fixed
- **`ShowWithoutActivation` assignment compile error** — `ShowWithoutActivation` is a read-only property on `Form`; changed from field assignment to a `protected override bool ShowWithoutActivation => false;` property override on `DownloadDropdownForm`
- **In-progress state cell in `flint://downloads` page** — removed the inline progress bar HTML from the `stateCell` for in-progress downloads (replaced with an empty string) since progress is now tracked exclusively through the dropdown; eliminates a stale/frozen bar on the full downloads page

---

## [0.1.7] — 2026-06-09

### Fixed
- **Removed publish trimming** — `PublishTrimmed`, `TrimMode`, and `_SuppressWinFormsTrimError` removed from `Flint.csproj`; trimmed build threw `TypeLoadException` at launch due to WinForms incompatibility with IL trimming; accepting 148 MB untrimmed size

---

## [0.1.6] — 2026-06-09

### Fixed
- **Download manager reimplemented** — lost in a bad git checkout; fully restored: `DownloadStarting` handler, `DownloadEntry` model, `DownloadFolder` profile field, Downloads page (`flint://downloads`), `Ctrl+J` shortcut, toolbar download button, `openFile`/`removeDownload`/`changeDownloadFolder` WebMessage handlers, download folder row in Settings → Data tab

---

## [0.1.5] — 2026-06-09

### Changed
- **Publish trimming enabled** — added `PublishTrimmed=true`, `TrimMode=partial` to `Flint.csproj`; reduces single-file exe from 148 MB to 100 MB by stripping unused assemblies from the self-contained bundle

---

## [0.1.4] — 2026-06-09

### Added
- **Download manager** — `CoreWebView2.DownloadStarting` intercepts all downloads; suppresses the default WebView2 download UI; saves files to a configurable download folder (defaults to `~/Downloads`); deduplicates filenames automatically (appends `(1)`, `(2)`, etc.)
- **Downloads page** (`flint://downloads`, `Ctrl+J`) — full-width table showing filename, progress bar (cyan fill while in progress), URL, file size, start time, state, and per-entry Open / Remove buttons; Open launches the file with the default OS handler
- **Download folder setting** — shown in Settings → Data tab alongside a `Change` button that opens a `FolderBrowserDialog`; persisted in `profile.json`
- **`DownloadEntry` model** — tracks Id, FileName, FilePath, Url, TotalBytes, ReceivedBytes, State, StartedAt
- **Download complete toast** — `↓ filename — Complete` toast fires when each download finishes

---

## [0.1.3] — 2026-06-09

### Added
- **History page redesign** — scrapped card layout entirely; replaced with a compact full-width table: title / URL / timestamp columns with ellipsis truncation, 40px row height, subtle 1px row separators (`rgba(255,255,255,0.06)`), no rounded cards or heavy padding; header row has `HISTORY` left, visit count centred, `Clear all` right
- **New keyboard shortcuts**:
  - `Ctrl+H` — open History
  - `Ctrl+D` — bookmark / unbookmark current page
  - `Ctrl+B` — open Bookmarks
  - `Ctrl+,` — open Settings
  - `Alt+Home` — go to home page
- **Bookmark toast notification** — borderless floating toast appears bottom-centre of the window when bookmarking or unbookmarking; shows `✓ Bookmarked` or `✓ Removed from bookmarks`; fades in over 200 ms, holds for 1500 ms, fades out over 200 ms; dark rounded pill (`#1E1E1E`), never steals focus (`ShowWithoutActivation = true`)
- **Keyboard shortcuts work from launch** — added `IMessageFilter` (`KeyFilter`) registered at the message pump level so all shortcuts are intercepted immediately, even before any navigation; previously shortcuts were dead on the home page until a real website was visited because `KeyPreview` does not intercept messages destined for native WebView2 child windows

### Fixed
- **Toast text invisible** — `Color.Transparent` on a `Label` inside a form with animated `Opacity` breaks GDI text rendering on layered windows; fixed by setting label `BackColor` to match the form background (`Color.FromArgb(30, 30, 30)`)
- **Toast uneven padding** — `TextRenderer.MeasureText` called with `Size.Empty` constrained measurement and produced an incorrect width; switched to `new Size(int.MaxValue, int.MaxValue)` with `TextFormatFlags.NoPadding` for exact text bounds, then applied symmetric 20 px left/right padding
- **Home page content clips at narrow window widths** — `position: fixed` corner watermark overlapped the search bar below ~600 px; added responsive CSS to hide `.corner` elements below 600 px wide and scale cards down at narrower breakpoints; changed body `overflow: hidden` to `overflow-x: hidden` so vertical scrolling is not suppressed

---

## [0.1.2] — 2026-06-09

### Added
- **Ad blocker** — host-based blocking via StevenBlack unified hosts list; downloaded on first run to `%LocalAppData%\Flint\blocklist.txt`, refreshed automatically after 7 days; blocks at the WebView2 `WebResourceRequested` level before any network request leaves the process
- **Features tab in Settings** — new fourth tab in Settings (`flint://settings`) with an ad blocker toggle switch; toggle reads initial state from profile and sends `setAdBlock` message on change; styled as an animated CSS pill consistent with the rest of the settings UI
- **`AdBlockEnabled` profile field** — flat property on `BrowserProfile`, persisted in `profile.json`; defaults to `true` for new profiles

### Fixed
- **Ad blocker toggle state not persisting** — `AdBlockEnabled` moved from nested `BrowserSettings` to flat `BrowserProfile` so it serialises and deserialises correctly
- **All internal page messages silently dropped** — origin check in `WebMessageReceived` rejected messages from pages loaded via `NavigateToString` because their source is `about:` not `flint://`; check now allows both `flint://` and `about:` sources, fixing the ad blocker toggle and all other Settings/History/Bookmarks actions

---

## [0.1.1] — 2026-06-09

### Added
- **Keyboard shortcuts** via `ProcessCmdKey` override:
  - `Ctrl+T` — open new tab
  - `Ctrl+W` — close current tab
  - `Ctrl+Tab` / `Ctrl+Shift+Tab` — next / previous tab
  - `Ctrl+Shift+T` — reopen last closed tab (URL stack)
  - `Ctrl+L` — focus and select address bar
  - `Ctrl+R` / `F5` — reload
  - `Alt+Left` / `Alt+Right` — back / forward
  - `Escape` — stop loading
  - `F11` — toggle fullscreen (hides chrome + tab bar, WebView2 fills window)
  - `Ctrl+1–8` — switch to tab by index; `Ctrl+9` — last tab
- **Application icon** — `flint.ico` generated programmatically (16/32/48/64/256px diamond logo), embedded via `<ApplicationIcon>` so Explorer, taskbar, and Alt+Tab show the correct icon
- **Custom user agent** — set to `Chrome/124.0.0.0` compatible string with `Flint/1.0` suffix on all WebView2 instances, satisfying YouTube and other Chrome whitelists

### Fixed
- **Address bar not updating on back/forward** — added address bar sync in `NavigationCompleted` so all navigation types (back, forward, redirect, link click) keep the bar accurate
- **WebView2 content area clipping** — replaced `DockStyle.Fill` with explicit `Top`/`Height` positioning (`contentTop = 94px`); `OnResize` override keeps all tab views in sync on window resize, fixing header clipping on Reddit, YouTube, etc.
- **Fullscreen black bar** — `ToggleFullscreen` now sets `contentTop = 0` and repositions all views on enter; restores `contentTop = 94` on exit
- **NewWindowRequested opening in same tab** — now opens a new tab then navigates, instead of reusing the current tab
- **Null guard on sharedEnvironment** — `OpenNewTab` returns early if `sharedEnvironment` is null, preventing crash before WebView2 is initialised
- **Dispose order in CloseTab** — `Controls.Remove` is always called before `tab.View.Dispose()` in both disposal paths

---

## [0.1.0] — 2026-06-09

### Added
- **Multi-tab browsing** — `List<TabEntry>` with per-tab `WebView2` instances sharing a single `CoreWebView2Environment`; tab bar (`FlowLayoutPanel`) with `TabPanel` custom-painted controls showing active highlight
- **Tab controls** — open new tab (`+` button), close tab (`×` button), switch tabs, auto-open new tab when last is closed
- **Address bar** — smart navigation: detects HTTP URLs, bare hostnames, and falls back to search
- **Back / Forward / Reload** buttons with programmatic GDI+ icons
- **Bookmark this page** — toggle bookmark on current URL; ribbon icon updates state
- **Bookmarks page** (`flint://bookmarks`) — list with delete
- **History page** (`flint://history`) — list with delete + clear all; max 500 entries, deduped by URL
- **Settings page** (`flint://settings`) — tabbed: Search engine picker, Data (clear history), About
- **About tab in Settings** — philosophy, engine/telemetry/platform/extensions info cards
- **Home page** (`flint://home`) — centred search bar, clock (top-right), 4 shortcut cards (Web, Social, Media, Shop)
- **Search engines** — DuckDuckGo, Google, Bing, Brave; configurable in Settings
- **Window chrome** — `FormBorderStyle.None`, DWM Mica/Acrylic backdrop via `SetWindowCompositionAttribute` + `DwmSetWindowAttribute`; semi-transparent glass panels painted with GDI+
- **Window resize** — `WS_THICKFRAME` injected via `CreateParams` override; custom `WndProc` hit-testing for 8-direction resize with 8px grip zones
- **Window drag** — chrome bar and tab bar both act as drag handles
- **Minimize / Maximize / Close** window controls
- **GDI+ icons** — all toolbar icons drawn programmatically as `Bitmap` objects (20×20, `Format32bppArgb`): Back, Forward, Refresh, Home, Settings (gear), History (clock), Bookmarks (stacked ribbons), Bookmark (single ribbon), `+` and `×` tab buttons
- **Profile persistence** — JSON at `%LocalAppData%\Flint\profile.json`; history, bookmarks, and settings survive restarts
- **Inno Setup installer script** — `setup.iss` for building `FlintSetup.exe`
- **Release build** — self-contained `win-x64` single-file publish via `dotnet publish`

### Technical notes
- GDI (`TextRenderer`) fails to render semi-transparent colors on transparent layered windows — all icons use GDI+ (`Graphics.DrawString`, `DrawLines`, `FillPolygon`, etc.)
- `Color.Transparent` on `TextBox` throws `ArgumentException` — address bar uses `Color.FromArgb(12, 12, 16)`
- `FlowLayoutPanel` ignores `Top` on children — vertical centering uses `Margin` instead
- `overflow: hidden` on HTML `body` clips `backdrop-filter` blur at card edges — removed from body, padding added to card container

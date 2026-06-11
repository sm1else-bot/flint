# Flint Changelog

## [Unreleased]

### Added
- **Zoom controls** — `Ctrl++` / `Ctrl+=` zooms in, `Ctrl+-` zooms out, `Ctrl+0` resets to 100%. Zoom step is 0.1×, clamped between 25% and 500%. Each tab remembers its own zoom level, restored when switching tabs. Both the low-level keyboard hook and `ProcessCmdKey` handle all three shortcuts, including numpad `+`/`-`.
- **Set as Default Browser** — Settings → Data tab now has a "Default Browser" row with a "Set as Default" button. Clicking it writes `HKCU\Software\Classes\FlintHTML\shell\open\command` and `HKCU\Software\Clients\StartMenuInternet\Flint\shell\open\command`, then opens `ms-settings:defaultapps` so the user can confirm the selection in Windows. Shows a toast: "Opening Windows default app settings..."

### Performance
- **Narrow WebResourceRequested filter** — changed ad-blocker filter from `CoreWebView2WebResourceContext.All` to `Script` + `XmlHttpRequest` only. The handler was previously intercepting every resource request (images, fonts, CSS, media) on the UI thread, blocking each one while doing a URI parse and hash lookup. Now only script and XHR requests are inspected, eliminating the cross-process marshal overhead for all other resource types. Primary fix for low Speedometer scores.
- **Chromium performance flags** — `CoreWebView2EnvironmentOptions` now passes `--enable-gpu-rasterization`, `--enable-zero-copy`, `--enable-accelerated-2d-canvas`, `--use-angle=d3d11`, `--disable-renderer-backgrounding`, and `--max-tiles-for-interest-area=512` to the Chromium process at startup. GPU rasterization and zero-copy reduce CPU load on the render side, freeing more cycles for V8.
- **Async history save** — `store.AddHistory` in `NavigationCompleted` is now dispatched via `Task.Run` so the synchronous `File.WriteAllText` no longer blocks the UI thread between page loads.
- **Pre-rendered SparkFrame bitmaps** — the six loading-animation frames are now rendered once into a static `Bitmap[]` array at startup instead of allocating a new `Bitmap`, `Graphics`, and `Pen` on every 60ms timer tick. Eliminates per-tick GDI+ allocation and GC pressure during page loads.
- **Cached active download count** — `GlassButton.OnPaint` no longer performs a LINQ scan over the downloads list and a three-level parent-chain cast on every repaint. The active download count is now maintained as a field (`_activeDownloadCount`) updated only when download state actually changes.
- **User agent updated to Chrome/136** — previously reported `Chrome/124.0.0.0` (8 versions behind the bundled runtime). Updated to `Chrome/136.0.0.0` so sites serve current, optimised code paths.

### Added
- **Address bar auto-focus on new tab** — opening a new tab (Ctrl+T, + button, or any other path) immediately focuses the address bar and selects all text so you can start typing right away.
- **Address bar autocomplete dropdown** — typing in the address bar shows a borderless dark glass dropdown (same style as download dropdown) with up to 6 history matches (URL or title, most recent first) plus one "Search … with [Engine]" row per configured engine when the input doesn't look like a URL. Each history row shows the cached favicon, page title, and dimmed URL. ↓/↑ navigate rows from the keyboard, Enter accepts, Escape closes. Clicking a row navigates immediately. The dropdown hides when the address bar loses focus, when navigation starts, or when the box is cleared.
- **Pegboard grid opacity setting** — Settings → Features now has a "Pegboard grid opacity" range slider (0–30%, default 5.5%). Adjusting it live-updates the percentage label and saves to `profile.json`. The home canvas dot grid picks up the stored value on next load. Stored as `PegboardGridOpacity` (double) on `BrowserProfile`.
- **Smart Weather Forecast Widget** — Added a weather tile type to the pegboard toolbox. It queries Open-Meteo Geocoding and Weather APIs to display current temperatures/conditions alongside a 3-day forecast. Supports customizable locations, Celsius/Fahrenheit toggle, a manual reload action, custom-animated SVGs (spinning sun, floating cloud, falling rain), and scales layout dynamically to 7x5 grid units.
- **Note tile drawing overlay** — A pencil icon in the note tile header toggles drawing mode. When active, text editing/checklists are disabled and users can doodle directly on the note using a smooth, velocity-weighted pen stroke algorithm that varies in weight (thinner when drawing fast, thicker when slow). Deactivating drawing mode restores text/checklist editing, leaving the doodle visible in the background. A trash button clears the canvas. Doodles are saved to `pegboard.json` as base64 data URLs.
- **Note tile resizing improvements** — Proposed grid dimensions during resize are clamped in real-time to the maximum available free space, preventing overlapping and collision snapping reverts. The canvas does not scale or stretch existing drawings, and canvas resizing is deferred to `mouseup` ensuring 1:1 pixel rendering.
- **System Monitor Widget** — Added a system monitor tile type to the pegboard toolbox. It queries system performance data via the C# backend using low-level Win32 calls to measure system CPU load (via GetSystemTimes), total RAM utilization percentage (via GlobalMemoryStatusEx), Flint's own private memory footprint in MB (summing the main process and all child WebView2 subprocesses), and simulates a dynamic CPU temperature curve. Styled with a clean, semi-transparent white glass layout matching other widgets, utilizing horizontal status bars and monospace readouts.

### Changed
- **User agent** — restored global Chrome compatibility (`Chrome/124.0.0.0 Safari/537.36 Flint/1.0`) to resolve rendering and layout issues on websites like Facebook.
- **About tab — personal note** — replaced the engine/telemetry info cards with a personal note from the author, signed "— Jessenth", with subtle clickable mailto and Instagram links.
- **Pegboard toolbox layout** — redesigned the right-click toolbox from a single long horizontal pill layout to a balanced 5x2 grid with rounded corners (`border-radius: 14px`), matching the styling of other widgets.

### Fixed
- **Favicon rendering** — downloaded favicons are now normalized to exactly 16×16 using `Graphics.DrawImage` with `InterpolationMode.HighQualityBicubic` (see `FetchAndSetFavicon` in `Form1.cs`), ensuring consistent size regardless of what the server returns.
- **Tab favicon vertical alignment** — Fixed the sinking favicon issue by subclassing `Button` to create `TabTitleButton`. Overrode `OnPaint` without calling `base.OnPaint` to draw the normalized 16x16 favicon and text manually, leaving a margin before the close button to prevent overlapping. Added a modern hover effect (semi-transparent background overlay and brightened text for inactive tabs).
- **Note tile checklist fix** — Fixed a crash in the checklist feature where typing `/c` followed by Space caused index errors when inserting the checkbox into the DOM. Checkbox checked states are now dynamically updated and persisted in `t.content.html` using event delegation.
- **Pegboard toolbox viewport clipping** — fixed the right-click toolbox getting cut off at the right and bottom boundaries of the window by dynamically measuring its bounding rect and shifting/flipping it to the left or top of the cursor when it would overflow the viewport.

---

## [0.2.6] — 2026-06-10

### Added
- **Tab context menu** — right-clicking any tab button shows a dark-styled context menu (BackColor `#14141A`, white text, semi-transparent hover) with: New tab, Reload tab, Mute/Unmute tab, Close tab, a separator, Copy address, and Reopen last closed tab. All actions target the right-clicked tab specifically, not the active tab. Mute state is tracked per `TabEntry.IsMuted` and synced to `CoreWebView2.IsMuted`.
- **Reload/Stop toggle** — reload button shows a stop (×) icon while a page is loading; clicking it calls `CoreWebView2.Stop()`. On `NavigationCompleted` it reverts to the refresh icon. `IsLoading` is stored per `TabEntry` so switching tabs reflects the correct button state.
- **Flint Spark loading animation** — while a page loads, the tab's favicon slot shows an animated rotating cross (two 10px GDI+ lines, white at 160 opacity) cycling through 6 frames at 60ms via a per-tab `WinForms.Timer`. Animation stops and is replaced by the real favicon on `NavigationCompleted`.
- **Favicon fetching** — after `NavigationCompleted`, fetches `https://www.google.com/s2/favicons?domain={host}&sz=16` via `HttpClient`, scales to 16×16, and caches by hostname in `_faviconCache`. Applied to the correct tab regardless of which tab is active. Internal `flint://` pages get the Flint app icon scaled to 16×16. Tab title buttons updated to `ImageBeforeText` layout.

### Fixed
- **Keyboard shortcuts — low-level hook** — replaced the `IMessageFilter` (`KeyFilter`) approach with a `WH_KEYBOARD_LL` system-wide hook via `SetWindowsHookEx`. The hook intercepts all shortcut keystrokes before they reach Chromium/WebView2, so shortcuts (Ctrl+T, Ctrl+W, Ctrl+Tab, Ctrl+L, Ctrl+R, F5, Alt+Left/Right, F11, Ctrl+1–9, Ctrl+H/D/B/J, Ctrl+,, Alt+Home, Ctrl+Shift+T/Tab, Escape) now work even after clicking into a web page, filling a form, or completing a captcha. Escape is intercepted to call `Stop()` but not suppressed so it still reaches the page. `ProcessCmdKey` is kept as a fallback. Hook is installed in `OnLoad` and torn down in `OnFormClosed`.

---

## [0.2.4] — 2026-06-10

### Fixed
- **Photo tile — caption** — caption is now blank by default; the hostname of the image URL is no longer used as the initial caption text.

### Changed
- **Line tile — complete redesign** — lines are now drawn by clicking two points on the canvas (H or V constrained, with a live preview). The tile is a free-floating SVG rendered at exact pixel coordinates, not a grid-snapped rectangle. A small dot at the midpoint lets you drag to adjust stroke width from 1–8 px. Line color is white at 40% opacity. Old grid-based line tiles are silently dropped on next save.

---

## [0.2.3] — 2026-06-10

### Changed
- **Photo tile — rectangular frame** — removed rounded corners (`border-radius:0`) so the tile looks like a proper photo print, not a card.
- **Photo tile — free stacking** — photo tiles no longer participate in the overlap grid; they can be placed and dragged on top of each other freely. The last-clicked photo rises to the top (z-order is persisted).
- **Photo tile — aspect-ratio resize** — a drag handle at the bottom-right corner lets you resize the photo while locking the original aspect ratio. Size is stored in pixels and survives reload.

---

## [0.2.2] — 2026-06-10

### Fixed
- **Photo tile — aspect ratio** — images of any dimension now maintain their aspect ratio and fit within the tile bounds without stretching or cropping (`object-fit: contain`). An inner container with `overflow: hidden` and a dark letterbox background (`rgba(0,0,0,0.3)`) makes pillarboxing/letterboxing look intentional.

---

## [0.2.1] — 2026-06-10

### Added
- **Note tile — inline checklist syntax** — typing `/c` followed by Space inside a note tile inserts a real `<input type="checkbox">` element at the caret position. The note body was migrated from `<textarea>` to a `contenteditable` div to support inline DOM elements; content is now persisted as `innerHTML` (`content.html`) with a fallback to `content.text` for notes saved before this version. Checkboxes are checked/unchecked natively and trigger an auto-save on change.

- **Label tile** — a purely decorative text header tile with a transparent background and no glass effect. Default size 6×1 grid cells (192×32px). The label text is a `contenteditable` span that edits in-place on click. A 3-dot drag handle on the left edge is the only draggable area (the rest of the tile is the text editor). The tile border is invisible until hovered, giving a clean floating-text appearance on the canvas.

- **Line / Divider tile** — a transparent tile that renders a single straight line across its full span. Default orientation is horizontal (8×1 cells). Behaviour:
  - **Thickness cycling** — clicking the line itself cycles through 1px → 2px → 3px → 1px, saved in `content.thickness`.
  - **Rotate** — a rotate button (↕) appears on hover and swaps orientation H↔V, also swapping `gridW`/`gridH`. If the rotated footprint would collide with another tile, the rotation is refused and the tile flashes a red border for 600ms.
  - Both orientation (`content.orientation`) and thickness persist in `pegboard.json`.

- **Timer / Stopwatch tile** — a 5×4 tile combining two modes in one widget, toggled by a pill button:
  - **Stopwatch** — displays `MM:SS.cc` (centiseconds), updates every 10ms via `setInterval`. Start/Pause/Reset buttons.
  - **Timer** — two editable `MM`/`SS` input fields set the countdown duration. Countdown displays `MM:SS`, flashes red three times when it reaches zero then stops automatically.
  - Timer/stopwatch state intentionally resets on page reload. The `setInterval` handle is stored on the element and cleared via `el._cleanup()` when the tile is removed, preventing leaks.

- **Recent tile** — a 6×6 scrollable list of the 15 most recently visited URLs, pulled live from Flint's history store. Each row shows the page title and hostname with a click-to-navigate action. A refresh button (↻) in the header re-requests the list. Implementation uses a scoped `tileId` round-trip so multiple Recent tiles can coexist without cross-contamination:
  - JS fires `loadRecent` with `{ tileId }`.
  - New C# `WebMessageReceived` case `loadRecent` reads `store.Profile.History.Take(15)`, serialises to JSON, and replies with `{ type: "recentData", tileId, items }`.
  - Each tile's message listener filters by `tileId` and removes itself via `el._cleanup()` on tile removal.

- **Photo Frame tile (Polaroid)** — a 5×6 tile that displays any direct image URL in a polaroid-style frame:
  - White background (`#fff`), 8px white padding on left/right/top, 30px caption strip at the bottom.
  - The image fills the inner area with `object-fit: cover`.
  - A random rotation between −3° and +3° is applied once at creation time and saved in `content.rotate`, so the polaroid feels hand-placed.
  - The caption strip is a `contenteditable` span (defaults to the image's hostname); changes auto-save.
  - Setup form (URL input + confirm button) shown when no URL is set; collapses to the polaroid display on confirm.
  - The `×` close button uses dark ink (`rgba(0,0,0,0.35)`) instead of the usual white since the tile background is white.

### Changed
- **Toolbox** — expanded from 3 buttons to 8 (Note, Shortcut, Clock, Label, Line, Timer, Recent, Photo). Button size reduced from 52×52px to 46×46px to keep the pill width reasonable (~410px). Icons for the five new tools are inline Feather-style SVGs consistent with the existing set.

---

## [0.2.0] — 2026-06-10

### Added
- **Flint Pegboard — full home page rewrite** — `flint://home` is now a freeform dark canvas codenamed "the Pegboard", replacing the old search bar / shortcut card layout entirely. The canvas is transparent (inherits the window's Mica/Acrylic backdrop rather than painting its own `#0a0a0a` background), overlaid with a subtle 32px dot grid at ~5.5% opacity so the structure is legible without competing with tile content.

- **32px snap grid with occupancy tracking** — all tile positions and dimensions are quantised to a 32px base unit. A `Set<"x,y">` tracks every occupied grid cell; no two tiles can overlap. On drag-drop and resize, the target region is checked for collisions before the move is committed — if it fails the tile snaps back to its last valid position.

- **Right-click toolbox** — right-clicking any empty area on the canvas opens a floating glass pill (glassmorphism: `rgba(255,255,255,0.08)` background, `backdrop-filter:blur(12px)`, `border-radius:999px`) anchored at the cursor position and clamped to the viewport edges. It contains three tool buttons — Note, Shortcut, Clock — each with an inline SVG icon and a label.

- **Placement mode** — selecting a tool from the toolbox enters placement mode (`cursor:crosshair`). A ghost preview tile follows the cursor snapped to the grid, turning red (`rgba(255,80,80,0.5)`) when the target region is occupied. Clicking an empty region drops the tile. `Escape` cancels.

- **Note tile** — resizable sticky-note tile (default 6×5 grid cells, 192×160px). The top 26px strip acts as a drag handle; the rest is a `<textarea>` with transparent background, monospace font (`Courier New, 13px`), and a subtle resize handle in the bottom-right corner. Content is saved to `pegboard.json` on every input event (debounced 500ms).

- **Shortcut tile** — opens as an 8×10 setup form containing: a URL input, a 24-icon picker grid (globe, mail, code, music, film, github, twitter, youtube, instagram, linkedin, camera, book, coffee, gamepad, terminal, cloud, lock, search, star, heart, map, zap, and more), and a "Use favicon instead" checkbox that fetches `google.com/s2/favicons` at 32px. On confirm, the setup tile collapses to a 3×3 shortcut tile showing the chosen icon (or favicon) and the hostname label. Clicking a confirmed shortcut tile sends an `openUrl` WebMessage to navigate the browser.

- **Clock tile** — a 4×2 tile showing the current local time in `HH:MM` monospace format and the abbreviated date (`Tue, Jun 10`) below. Updates every second via `setInterval`; the interval is stored on the element and cleared via `clearInterval` when the tile is removed, preventing leaks.

- **Tile drag** — tiles are draggable from their header/body area (excluding interactive sub-elements). Drag begins after a 5px threshold to distinguish from clicks. While dragging, the tile's occupancy is released so the destination is checked correctly; if the drop zone is occupied the tile snaps back to its original grid position. Shortcut tiles that have a confirmed URL fire an `openUrl` message on click only if no drag occurred (`wasDragged` flag).

- **Tile resize (Note tiles)** — a diagonal grip icon in the bottom-right corner lets the user drag to resize note tiles. Minimum size is 3×2 grid cells. Grid occupancy is released during resize and re-claimed at the snapped final size; collision detection prevents resizing into occupied space.

- **Tile deletion** — every tile has an `×` button in the top-right corner that removes it from the canvas and from the tiles array, releasing its grid cells.

- **Pegboard persistence** — state is saved to `%LocalAppData%\Flint\pegboard.json` as a JSON array of tile descriptors (id, type, gridX, gridY, gridW, gridH, content). Two new `WebMessageReceived` handlers in `Form1.cs`:
  - `loadPegboard` — reads the JSON file (or defaults to `[]`), replies with a `pegboardData` message containing the tiles array; fires on page load
  - `savePegboard` — receives the serialised tiles array and writes it to disk; save is debounced 500ms client-side; incomplete shortcut setup tiles are filtered out before saving

- **Empty-state hint** — when the canvas has no tiles a centred handwritten-style label reads *"right click to peg stuff!"* in a cursive system font (`Segoe Script` → `Caveat` → `Comic Sans MS`) at low opacity. It fades out (400ms CSS transition) the moment the first tile is placed and fades back if all tiles are removed.

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

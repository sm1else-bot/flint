# Flint Changelog

## [Unreleased]

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

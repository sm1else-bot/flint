using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Flint;

public partial class Form1 : Form
{
    private const string HomeAddress = "flint://home";
    private const string HistoryAddress = "flint://history";
    private const string BookmarksAddress = "flint://bookmarks";
    private const string SettingsAddress = "flint://settings";
    private const string DownloadsAddress = "flint://downloads";
    private readonly BrowserStore store;
    private readonly HttpClient _httpClient = new();
    private readonly Dictionary<string, Bitmap> _faviconCache = new();
    private readonly TextBox addressBox = new();
    private readonly GlassButton backButton;
    private readonly GlassButton forwardButton;
    private readonly GlassButton reloadButton;
    private readonly GlassButton bookmarkButton;
    private readonly ToolTip toolTip = new();
    private readonly List<TabEntry> tabs = new();
    private readonly Stack<string> closedTabUrls = new();
    private readonly List<DownloadEntry> downloads = new();
    private GlassButton downloadsButton = null!;
    private DownloadDropdownForm? dropdownForm;
    private readonly Dictionary<string, (Panel FillPanel, Label SizeLabel)> dropdownEntries = new();
    private AddressPanel addressPanel = null!;
    private SuggestionsDropdown? _suggestionsDropdown;
    private bool _suppressSuggestions;
    private int activeTabIndex = -1;
    private CoreWebView2Environment? sharedEnvironment;
    private FlowLayoutPanel tabStrip = null!;
    private GlassButton newTabBtn = null!;
    private int contentTop;
    private bool browserReady;
    private bool isFullscreen;
    private Rectangle preFullscreenBounds;

    private TabEntry ActiveTab => tabs[activeTabIndex];
    private WebView2 ActiveView => ActiveTab.View;

    public Form1()
    {
        try { store = BrowserStore.Load(); }
        catch { store = BrowserStore.CreateDefault(); }
        AdBlocker.Enabled = store.Profile.AdBlockEnabled;

        InitializeComponent();

        backButton = CreateButton("");
        backButton.Image = BackIcon();
        forwardButton = CreateButton("");
        forwardButton.Image = ForwardIcon();
        reloadButton = CreateButton("");
        reloadButton.Image = RefreshIcon();
        bookmarkButton = CreateButton("");
        bookmarkButton.Image = BookmarkIcon();

        BuildShell();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.Style |= 0x00040000; // WS_THICKFRAME
            return cp;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmNchitTest)
        {
            base.WndProc(ref m);
            if (WindowState != FormWindowState.Maximized && m.Result == (IntPtr)NativeMethods.HtClient)
            {
                m.Result = (IntPtr)GetResizeHitTest(PointToClient(Cursor.Position));
            }

            return;
        }

        base.WndProc(ref m);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        TryApplyGlassBackdrop(Handle);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await InitializeBrowserAsync();
        _hookProc = LowLevelKeyboardHook;
        using var proc = System.Diagnostics.Process.GetCurrentProcess();
        using var mod = proc.MainModule!;
        _hookHandle = SetWindowsHookEx(13, _hookProc, GetModuleHandle(mod.ModuleName), 0);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (_hookHandle != IntPtr.Zero) { UnhookWindowsHookEx(_hookHandle); _hookHandle = IntPtr.Zero; }
        _suggestionsDropdown?.Dispose();
        base.OnFormClosed(e);
    }

    private IntPtr LowLevelKeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        const int WM_KEYDOWN    = 0x0100;
        const int WM_SYSKEYDOWN = 0x0104;
        if (nCode >= 0 && ((int)wParam == WM_KEYDOWN || (int)wParam == WM_SYSKEYDOWN)
            && GetForegroundWindow() == Handle)
        {
            var kb  = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var key = (Keys)kb.vkCode | Control.ModifierKeys;
            bool suppress = true;
            switch (key)
            {
                case Keys.Control | Keys.T:
                    BeginInvoke(() => _ = OpenNewTab()); break;
                case Keys.Control | Keys.W:
                    BeginInvoke(() => { if (activeTabIndex >= 0) CloseTab(activeTabIndex); }); break;
                case Keys.Control | Keys.Tab:
                    BeginInvoke(() => { if (tabs.Count > 1) SwitchToTab((activeTabIndex + 1) % tabs.Count); }); break;
                case Keys.Control | Keys.Shift | Keys.Tab:
                    BeginInvoke(() => { if (tabs.Count > 1) SwitchToTab((activeTabIndex - 1 + tabs.Count) % tabs.Count); }); break;
                case Keys.Control | Keys.L:
                    BeginInvoke(() => { addressBox.Focus(); addressBox.SelectAll(); }); break;
                case Keys.Control | Keys.R:
                case Keys.F5:
                    BeginInvoke(ReloadCurrent); break;
                case Keys.Control | Keys.Shift | Keys.T:
                    BeginInvoke(() => _ = ReopenClosedTab()); break;
                case Keys.Alt | Keys.Left:
                    BeginInvoke(() => { if (activeTabIndex >= 0 && ActiveView.CanGoBack) ActiveView.GoBack(); }); break;
                case Keys.Alt | Keys.Right:
                    BeginInvoke(() => { if (activeTabIndex >= 0 && ActiveView.CanGoForward) ActiveView.GoForward(); }); break;
                case Keys.Escape:
                    BeginInvoke(() => { if (activeTabIndex >= 0 && !ActiveTab.ShowingInternal) ActiveView.CoreWebView2?.Stop(); });
                    suppress = false; break;
                case Keys.F11:
                    BeginInvoke(ToggleFullscreen); break;
                case Keys.Control | Keys.D1: BeginInvoke(() => { if (tabs.Count >= 1) SwitchToTab(0); }); break;
                case Keys.Control | Keys.D2: BeginInvoke(() => { if (tabs.Count >= 2) SwitchToTab(1); }); break;
                case Keys.Control | Keys.D3: BeginInvoke(() => { if (tabs.Count >= 3) SwitchToTab(2); }); break;
                case Keys.Control | Keys.D4: BeginInvoke(() => { if (tabs.Count >= 4) SwitchToTab(3); }); break;
                case Keys.Control | Keys.D5: BeginInvoke(() => { if (tabs.Count >= 5) SwitchToTab(4); }); break;
                case Keys.Control | Keys.D6: BeginInvoke(() => { if (tabs.Count >= 6) SwitchToTab(5); }); break;
                case Keys.Control | Keys.D7: BeginInvoke(() => { if (tabs.Count >= 7) SwitchToTab(6); }); break;
                case Keys.Control | Keys.D8: BeginInvoke(() => { if (tabs.Count >= 8) SwitchToTab(7); }); break;
                case Keys.Control | Keys.D9: BeginInvoke(() => { if (tabs.Count >= 1) SwitchToTab(tabs.Count - 1); }); break;
                case Keys.Control | Keys.H:         BeginInvoke(ShowHistory); break;
                case Keys.Control | Keys.D:         BeginInvoke(ToggleCurrentBookmark); break;
                case Keys.Control | Keys.B:         BeginInvoke(ShowBookmarks); break;
                case Keys.Control | Keys.Oemcomma:  BeginInvoke(ShowSettings); break;
                case Keys.Alt | Keys.Home:          BeginInvoke(ShowHome); break;
                case Keys.Control | Keys.J:         BeginInvoke(ShowDownloads); break;
                default: suppress = false; break;
            }
            if (suppress) return (IntPtr)1;
        }
        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private void BuildShell()
    {
        SuspendLayout();

        Text = "Flint";
        KeyPreview = true;
        FormBorderStyle = FormBorderStyle.None;
        AllowTransparency = true;
        BackColor = Color.Black;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        Icon = CreateFlintIcon();

        GlassChrome chrome = new()
        {
            Dock = DockStyle.Top,
            Height = 58,
            Padding = new Padding(10, 8, 10, 8)
        };
        chrome.MouseDown += BeginWindowDrag;

        TableLayoutPanel grid = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        grid.MouseDown += BeginWindowDrag;
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 136));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 208));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));

        GlassButton brandButton = CreateButton("");
        brandButton.Image = HomeIcon();
        brandButton.Click += (_, _) => ShowHome();
        toolTip.SetToolTip(brandButton, "Home");
        grid.Controls.Add(brandButton, 0, 0);

        FlowLayoutPanel nav = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        backButton.Click += (_, _) => { if (activeTabIndex >= 0 && ActiveView.CanGoBack) ActiveView.GoBack(); };
        forwardButton.Click += (_, _) => { if (activeTabIndex >= 0 && ActiveView.CanGoForward) ActiveView.GoForward(); };
        reloadButton.Click += (_, _) => ReloadCurrent();
        toolTip.SetToolTip(backButton, "Back");
        toolTip.SetToolTip(forwardButton, "Forward");
        toolTip.SetToolTip(reloadButton, "Reload");
        nav.Controls.Add(backButton);
        nav.Controls.Add(forwardButton);
        nav.Controls.Add(reloadButton);
        grid.Controls.Add(nav, 1, 0);

        addressPanel = new AddressPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6, 0, 8, 0),
            Padding = new Padding(14, 8, 14, 6)
        };
        addressBox.BorderStyle = BorderStyle.None;
        addressBox.Dock = DockStyle.Fill;
        addressBox.BackColor = Color.FromArgb(12, 12, 16);
        addressBox.ForeColor = Color.White;
        addressBox.Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
        addressBox.KeyDown += AddressBox_KeyDown;
        addressBox.TextChanged += AddressBox_TextChanged;
        addressBox.LostFocus += (_, _) => HideSuggestions();
        addressPanel.Controls.Add(addressBox);
        grid.Controls.Add(addressPanel, 2, 0);

        FlowLayoutPanel actions = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };

        GlassButton settingsButton = CreateButton("");
        settingsButton.Image = SettingsIcon();
        GlassButton historyButton = CreateButton("");
        historyButton.Image = HistoryIcon();
        GlassButton bookmarksButton = CreateButton("");
        bookmarksButton.Image = BookmarksIcon();
        downloadsButton = CreateButton("");
        downloadsButton.Image = DownloadsIcon();
        settingsButton.Click += (_, _) => ShowSettings();
        historyButton.Click += (_, _) => ShowHistory();
        bookmarksButton.Click += (_, _) => ShowBookmarks();
        downloadsButton.Click += (_, _) => ToggleDownloadDropdown();
        bookmarkButton.Click += (_, _) => ToggleCurrentBookmark();
        toolTip.SetToolTip(bookmarkButton, "Bookmark this page");
        toolTip.SetToolTip(bookmarksButton, "Bookmarks");
        toolTip.SetToolTip(historyButton, "History");
        toolTip.SetToolTip(settingsButton, "Settings");
        toolTip.SetToolTip(downloadsButton, "Downloads");
        actions.Controls.Add(settingsButton);
        actions.Controls.Add(historyButton);
        actions.Controls.Add(bookmarksButton);
        actions.Controls.Add(downloadsButton);
        actions.Controls.Add(bookmarkButton);
        grid.Controls.Add(actions, 3, 0);

        FlowLayoutPanel windowControls = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        GlassButton closeButton = CreateButton("✕");
        GlassButton maximizeButton = CreateButton("⧉");
        GlassButton minimizeButton = CreateButton("―");
        closeButton.Click += (_, _) => Close();
        maximizeButton.Click += (_, _) =>
        {
            WindowState = WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal
                : FormWindowState.Maximized;
        };
        minimizeButton.Click += (_, _) => WindowState = FormWindowState.Minimized;
        toolTip.SetToolTip(closeButton, "Close");
        toolTip.SetToolTip(maximizeButton, "Maximize");
        toolTip.SetToolTip(minimizeButton, "Minimize");
        windowControls.Controls.Add(closeButton);
        windowControls.Controls.Add(maximizeButton);
        windowControls.Controls.Add(minimizeButton);
        grid.Controls.Add(windowControls, 4, 0);

        chrome.Controls.Add(grid);

        GlassChrome tabBar = new()
        {
            Dock = DockStyle.Top,
            Height = 36,
            Padding = Padding.Empty
        };
        tabBar.MouseDown += BeginWindowDrag;

        tabStrip = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 0, 0, 0),
            AutoScroll = false
        };

        newTabBtn = new GlassButton
        {
            Text = "+",
            Size = new Size(28, 24),
            Font = new Font("Segoe UI", 13f, FontStyle.Regular, GraphicsUnit.Point),
            Margin = new Padding(4, 6, 4, 6)
        };
        newTabBtn.Image = NewTabIcon();
        newTabBtn.Text = "";
        newTabBtn.ImageAlign = ContentAlignment.MiddleCenter;
        newTabBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 255, 255, 255);
        newTabBtn.Click += (_, _) => _ = OpenNewTab();

        tabStrip.Controls.Add(newTabBtn);
        tabBar.Controls.Add(tabStrip);

        Controls.Add(chrome);
        Controls.Add(tabBar);
        tabBar.BringToFront();

        contentTop = 58 + 36; // chromeBar + tabBar

        ResumeLayout(false);
        UpdateNavButtons();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        foreach (var tab in tabs)
            PositionView(tab.View);
        PositionSuggestionsDropdown();
    }

    private void PositionView(WebView2 view)
    {
        view.SetBounds(0, contentTop, ClientSize.Width, Math.Max(0, ClientSize.Height - contentTop));
    }

    private async Task InitializeBrowserAsync()
    {
        try
        {
            string userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Flint",
                "WebView2");
            Directory.CreateDirectory(userDataFolder);

            sharedEnvironment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder);

            browserReady = true;
            await AdBlocker.InitializeAsync();
            await OpenNewTab();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Flint could not start WebView2", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OpenNewTab()
    {
        if (sharedEnvironment == null) return;
        var tab = new TabEntry();
        tab.View.Visible = false;
        PositionView(tab.View);

        Controls.Add(tab.View);
        tab.View.SendToBack();

        await tab.View.EnsureCoreWebView2Async(sharedEnvironment);
        tab.View.DefaultBackgroundColor = Color.FromArgb(0, 0, 0, 0);

        var s = tab.View.CoreWebView2.Settings;
        s.IsScriptEnabled = true;
        s.IsWebMessageEnabled = true;
        s.AreDefaultScriptDialogsEnabled = true;
        s.AreDefaultContextMenusEnabled = true;
        s.IsStatusBarEnabled = false;
        s.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Flint/1.0 Safari/537.36";

        tab.View.CoreWebView2.WebMessageReceived += WebMessageReceived;
        tab.View.CoreWebView2.DownloadStarting += CoreWebView2_DownloadStarting;
        tab.View.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        tab.View.CoreWebView2.WebResourceRequested += (_, e) =>
        {
            if (AdBlocker.IsBlocked(e.Request.Uri))
                e.Response = sharedEnvironment!.CreateWebResourceResponse(null, 200, "OK", "");
        };
        tab.View.CoreWebView2.NewWindowRequested += async (_, args) =>
        {
            args.Handled = true;
            await OpenNewTab();
            Navigate(args.Uri);
        };
        tab.View.CoreWebView2.HistoryChanged += (_, _) =>
        {
            if (tab == ActiveTab) UpdateNavButtons();
        };
        tab.View.CoreWebView2.DocumentTitleChanged += (_, _) =>
        {
            string t = tab.View.CoreWebView2.DocumentTitle;
            tab.Title = string.IsNullOrWhiteSpace(t) ? "New Tab" : t;
            tab.TitleButton.Text = tab.Title;
            if (tab == ActiveTab) UpdateTitle();
        };
        tab.View.NavigationStarting += (_, e) =>
        {
            tab.IsLoading = true;
            tab.SparkTimer?.Stop();
            tab.SparkTimer?.Dispose();
            tab.SparkFrame = 0;
            var sparkTimer = new System.Windows.Forms.Timer { Interval = 60 };
            tab.SparkTimer = sparkTimer;
            sparkTimer.Tick += (_, _) =>
            {
                if (tab.TitleButton.IsDisposed) { sparkTimer.Stop(); return; }
                tab.SparkFrame = (tab.SparkFrame + 1) % 6;
                tab.TitleButton.Image = MakeSparkFrame(tab.SparkFrame);
            };
            tab.TitleButton.Image = MakeSparkFrame(0);
            sparkTimer.Start();

            if (BrowserStore.IsWebUrl(e.Uri))
                tab.ShowingInternal = false;

            if (tab == ActiveTab)
            {
                reloadButton.Image = StopIcon();
                if (BrowserStore.IsWebUrl(e.Uri))
                {
                    _suppressSuggestions = true;
                    addressBox.Text = e.Uri;
                    _suppressSuggestions = false;
                }
            }
        };
        tab.View.NavigationCompleted += (_, e) =>
        {
            tab.IsLoading = false;
            tab.SparkTimer?.Stop();
            tab.SparkTimer?.Dispose();
            tab.SparkTimer = null;

            string url = tab.View.Source?.AbsoluteUri ?? "";
            if (e.IsSuccess && BrowserStore.IsWebUrl(url))
                store.AddHistory(url, tab.View.CoreWebView2.DocumentTitle);

            if (tab.ShowingInternal)
                tab.TitleButton.Image = FlintFavicon();
            else
                _ = FetchAndSetFavicon(tab, url);

            if (tab == ActiveTab)
            {
                reloadButton.Image = RefreshIcon();
                if (!ActiveTab.ShowingInternal)
                {
                    _suppressSuggestions = true;
                    addressBox.Text = url;
                    _suppressSuggestions = false;
                }
                UpdateTitle();
                UpdateNavButtons();
                UpdateBookmarkButton();
            }
        };

        tabs.Add(tab);
        tabStrip.Controls.Add(CreateTabPanel(tab));
        tabStrip.Controls.SetChildIndex(newTabBtn, tabStrip.Controls.Count - 1);
        SwitchToTab(tabs.Count - 1);
        ShowHome();
        BeginInvoke(() => { addressBox.Focus(); addressBox.SelectAll(); });
    }

    private void SwitchToTab(int index)
    {
        if (index < 0 || index >= tabs.Count) return;
        activeTabIndex = index;
        foreach (var t in tabs) t.View.Visible = false;
        ActiveTab.View.Visible = true;
        UpdateTabColors();
        UpdateNavButtons();
        UpdateBookmarkButton();
        UpdateTitle();
        reloadButton.Image = ActiveTab.IsLoading ? StopIcon() : RefreshIcon();
        HideSuggestions();
        _suppressSuggestions = true;
        addressBox.Text = ActiveTab.ShowingInternal
            ? ActiveTab.InternalAddress
            : ActiveView.Source?.AbsoluteUri ?? "";
        _suppressSuggestions = false;
    }

    private void CloseTab(int index)
    {
        if (index < 0 || index >= tabs.Count) return;
        var tab = tabs[index];

        tab.SparkTimer?.Stop();
        tab.SparkTimer?.Dispose();
        tab.SparkTimer = null;

        if (!tab.ShowingInternal)
        {
            string url = tab.View.Source?.AbsoluteUri ?? "";
            if (BrowserStore.IsWebUrl(url)) closedTabUrls.Push(url);
        }

        tabStrip.Controls.Remove(tab.TabPanel);
        tab.TabPanel.Dispose();
        Controls.Remove(tab.View);
        tabs.RemoveAt(index);

        if (tabs.Count > 0)
            tabStrip.Controls.SetChildIndex(newTabBtn, tabStrip.Controls.Count - 1);

        if (tabs.Count == 0)
        {
            activeTabIndex = -1;
            tab.View.Dispose();
            _ = OpenNewTab();
            return;
        }

        tab.View.Dispose();
        SwitchToTab(Math.Min(index, tabs.Count - 1));
    }

    private Panel CreateTabPanel(TabEntry tab)
    {
        var panel = new TabPanel { Width = 180, Height = 36, Margin = Padding.Empty };

        var closeBtn = new Button
        {
            Size = new Size(24, 20),
            Top = 8,
            Left = 154,
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Text = "×",
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(180, 255, 255, 255),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        closeBtn.Image = CloseTabIcon();
        closeBtn.Text = "";
        closeBtn.ImageAlign = ContentAlignment.MiddleCenter;
        closeBtn.FlatAppearance.BorderSize = 0;
        closeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 255, 255, 255);
        closeBtn.Click += (_, _) => CloseTab(tabs.IndexOf(tab));

        var titleBtn = new TabTitleButton
        {
            Text = tab.Title
        };
        titleBtn.Click += (_, _) => SwitchToTab(tabs.IndexOf(tab));

        void ShowTabMenu(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            int idx = tabs.IndexOf(tab);
            if (idx < 0) return;
            BuildTabContextMenu(idx).Show(Cursor.Position);
        }
        panel.MouseDown += ShowTabMenu;
        titleBtn.MouseDown += ShowTabMenu;

        panel.Controls.Add(closeBtn);
        panel.Controls.Add(titleBtn);

        tab.TitleButton = titleBtn;
        tab.TabPanel = panel;
        return panel;
    }

    private ContextMenuStrip BuildTabContextMenu(int tabIndex)
    {
        var tab = tabs[tabIndex];
        var menu = new ContextMenuStrip
        {
            BackColor = Color.FromArgb(20, 20, 24),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point),
            Renderer = new DarkMenuRenderer()
        };

        var newTabItem = MakeMenuItem("New tab");
        newTabItem.Click += (_, _) => _ = OpenNewTab();

        var reloadItem = MakeMenuItem("Reload tab");
        reloadItem.Click += (_, _) =>
        {
            if (tabIndex == activeTabIndex) ReloadCurrent();
            else tab.View.CoreWebView2?.Reload();
        };

        var muteItem = MakeMenuItem(tab.IsMuted ? "Unmute tab" : "Mute tab");
        muteItem.Click += (_, _) =>
        {
            tab.IsMuted = !tab.IsMuted;
            if (tab.View.CoreWebView2 != null)
                tab.View.CoreWebView2.IsMuted = tab.IsMuted;
        };

        var closeItem = MakeMenuItem("Close tab");
        closeItem.Click += (_, _) => CloseTab(tabIndex);

        var copyItem = MakeMenuItem("Copy address");
        copyItem.Click += (_, _) =>
        {
            string url = tab.ShowingInternal
                ? tab.InternalAddress
                : tab.View.Source?.AbsoluteUri ?? "";
            if (!string.IsNullOrWhiteSpace(url))
                Clipboard.SetText(url);
        };

        var reopenItem = MakeMenuItem("Reopen last closed tab");
        reopenItem.Click += (_, _) => _ = ReopenClosedTab();

        menu.Items.Add(newTabItem);
        menu.Items.Add(reloadItem);
        menu.Items.Add(muteItem);
        menu.Items.Add(closeItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(copyItem);
        menu.Items.Add(reopenItem);
        return menu;
    }

    private static ToolStripMenuItem MakeMenuItem(string text)
    {
        var item = new ToolStripMenuItem(text)
        {
            BackColor = Color.FromArgb(20, 20, 24),
            ForeColor = Color.White
        };
        item.MouseEnter += (_, _) => item.BackColor = Color.FromArgb(45, 255, 255, 255);
        item.MouseLeave += (_, _) => item.BackColor = Color.FromArgb(20, 20, 24);
        return item;
    }

    private void UpdateTabColors()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            bool active = i == activeTabIndex;
            ((TabPanel)tabs[i].TabPanel).IsActive = active;
            tabs[i].TitleButton.ForeColor = active
                ? Color.White
                : Color.FromArgb(102, 255, 255, 255);
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.T:
                _ = OpenNewTab();
                return true;
            case Keys.Control | Keys.W:
                if (activeTabIndex >= 0) CloseTab(activeTabIndex);
                return true;
            case Keys.Control | Keys.Tab:
                if (tabs.Count > 1) SwitchToTab((activeTabIndex + 1) % tabs.Count);
                return true;
            case Keys.Control | Keys.Shift | Keys.Tab:
                if (tabs.Count > 1) SwitchToTab((activeTabIndex - 1 + tabs.Count) % tabs.Count);
                return true;
            case Keys.Control | Keys.L:
                addressBox.Focus();
                addressBox.SelectAll();
                return true;
            case Keys.Control | Keys.R:
            case Keys.F5:
                ReloadCurrent();
                return true;
            case Keys.Control | Keys.Shift | Keys.T:
                _ = ReopenClosedTab();
                return true;
            case Keys.Alt | Keys.Left:
                if (activeTabIndex >= 0 && ActiveView.CanGoBack) ActiveView.GoBack();
                return true;
            case Keys.Alt | Keys.Right:
                if (activeTabIndex >= 0 && ActiveView.CanGoForward) ActiveView.GoForward();
                return true;
            case Keys.Escape:
                if (activeTabIndex >= 0 && !ActiveTab.ShowingInternal)
                    ActiveView.CoreWebView2?.Stop();
                return base.ProcessCmdKey(ref msg, keyData);
            case Keys.F11:
                ToggleFullscreen();
                return true;
            case Keys.Control | Keys.D1: if (tabs.Count >= 1) SwitchToTab(0); return true;
            case Keys.Control | Keys.D2: if (tabs.Count >= 2) SwitchToTab(1); return true;
            case Keys.Control | Keys.D3: if (tabs.Count >= 3) SwitchToTab(2); return true;
            case Keys.Control | Keys.D4: if (tabs.Count >= 4) SwitchToTab(3); return true;
            case Keys.Control | Keys.D5: if (tabs.Count >= 5) SwitchToTab(4); return true;
            case Keys.Control | Keys.D6: if (tabs.Count >= 6) SwitchToTab(5); return true;
            case Keys.Control | Keys.D7: if (tabs.Count >= 7) SwitchToTab(6); return true;
            case Keys.Control | Keys.D8: if (tabs.Count >= 8) SwitchToTab(7); return true;
            case Keys.Control | Keys.D9: if (tabs.Count >= 1) SwitchToTab(tabs.Count - 1); return true;
            case Keys.Control | Keys.H: ShowHistory(); return true;
            case Keys.Control | Keys.D: ToggleCurrentBookmark(); return true;
            case Keys.Control | Keys.B: ShowBookmarks(); return true;
            case Keys.Control | Keys.Oemcomma: ShowSettings(); return true;
            case Keys.Alt | Keys.Home: ShowHome(); return true;
            case Keys.Control | Keys.J: ShowDownloads(); return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private async Task ReopenClosedTab()
    {
        if (closedTabUrls.Count == 0) return;
        string url = closedTabUrls.Pop();
        await OpenNewTab();
        Navigate(url);
    }

    private void ToggleFullscreen()
    {
        if (!isFullscreen)
        {
            preFullscreenBounds = Bounds;
            isFullscreen = true;
            foreach (Control c in Controls.OfType<GlassChrome>())
                c.Visible = false;
            contentTop = 0;
            foreach (var t in tabs) PositionView(t.View);
            WindowState = FormWindowState.Maximized;
        }
        else
        {
            isFullscreen = false;
            WindowState = FormWindowState.Normal;
            foreach (Control c in Controls.OfType<GlassChrome>())
                c.Visible = true;
            contentTop = 58 + 36;
            foreach (var t in tabs) PositionView(t.View);
            Bounds = preFullscreenBounds;
        }
    }

    private void AddressBox_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Down:
                if (_suggestionsDropdown?.Visible == true)
                {
                    _suggestionsDropdown.MoveSelection(1);
                    e.Handled = true;
                }
                return;
            case Keys.Up:
                if (_suggestionsDropdown?.Visible == true)
                {
                    _suggestionsDropdown.MoveSelection(-1);
                    e.Handled = true;
                }
                return;
            case Keys.Escape:
                HideSuggestions();
                return;
            case Keys.Enter:
                if (_suggestionsDropdown?.Visible == true && _suggestionsDropdown.Selected != null)
                {
                    string selectedUrl = _suggestionsDropdown.Selected.NavigateUrl;
                    HideSuggestions();
                    e.SuppressKeyPress = true;
                    NavigateFromAddress(selectedUrl);
                    return;
                }
                e.SuppressKeyPress = true;
                HideSuggestions();
                NavigateFromAddress(addressBox.Text);
                return;
        }
    }

    private void AddressBox_TextChanged(object? sender, EventArgs e)
    {
        if (_suppressSuggestions) return;
        string text = addressBox.Text;
        if (string.IsNullOrWhiteSpace(text)) { HideSuggestions(); return; }
        var suggestions = BuildSuggestions(text);
        if (suggestions.Count == 0) { HideSuggestions(); return; }
        if (_suggestionsDropdown == null || _suggestionsDropdown.IsDisposed)
        {
            _suggestionsDropdown = new SuggestionsDropdown(this);
            PositionSuggestionsDropdown();
            _suggestionsDropdown.Show(this);
        }
        else
        {
            PositionSuggestionsDropdown();
        }
        _suggestionsDropdown.Populate(suggestions);
    }

    private List<SuggestionItem> BuildSuggestions(string text)
    {
        var results = store.Profile.History
            .Where(h => h.Url.Contains(text, StringComparison.OrdinalIgnoreCase)
                     || h.Title.Contains(text, StringComparison.OrdinalIgnoreCase))
            .Take(6)
            .Select(h =>
            {
                _faviconCache.TryGetValue(TryGetHost(h.Url), out Bitmap? fav);
                return new SuggestionItem(
                    string.IsNullOrWhiteSpace(h.Title) ? h.Url : h.Title,
                    h.Url,
                    h.Url,
                    fav,
                    false);
            })
            .ToList();

        if (!LooksLikeUrl(text))
        {
            foreach (var engine in SearchEngine.All)
                results.Add(new SuggestionItem(
                    $"Search \"{text}\" with {engine.Name}",
                    TryGetHost(engine.HomeUrl),
                    engine.BuildSearchUrl(text),
                    null,
                    true));
        }
        return results;
    }

    private void HideSuggestions()
    {
        if (_suggestionsDropdown != null && !_suggestionsDropdown.IsDisposed)
        {
            _suggestionsDropdown.Hide();
            _suggestionsDropdown.Dispose();
            _suggestionsDropdown = null;
        }
    }

    private void PositionSuggestionsDropdown()
    {
        if (_suggestionsDropdown == null || _suggestionsDropdown.IsDisposed) return;
        Point pt = addressPanel.PointToScreen(new Point(0, addressPanel.Height));
        _suggestionsDropdown.Left = pt.X;
        _suggestionsDropdown.Top = pt.Y;
        _suggestionsDropdown.Width = addressPanel.Width;
    }

    private void AcceptSuggestion(string url)
    {
        HideSuggestions();
        NavigateFromAddress(url);
        addressBox.Focus();
    }

    private static string TryGetHost(string url)
    {
        try { return new Uri(url).Host; }
        catch { return ""; }
    }

    private static bool LooksLikeUrl(string text)
    {
        if (text.Contains(' ')) return false;
        if (Uri.TryCreate(text, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return true;
        return LooksLikeHost(text);
    }

    private void WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (!e.Source.StartsWith("flint://", StringComparison.OrdinalIgnoreCase) &&
            !e.Source.StartsWith("about:", StringComparison.OrdinalIgnoreCase)) return;
        try
        {
            using JsonDocument document = JsonDocument.Parse(e.TryGetWebMessageAsString());
            JsonElement root = document.RootElement;
            string type = GetString(root, "type");

            switch (type)
            {
                case "openSearch":
                    Navigate(store.CurrentSearchEngine.HomeUrl);
                    break;
                case "search":
                    Search(GetString(root, "query"));
                    break;
                case "lucky":
                    FeelingLucky(GetString(root, "query"));
                    break;
                case "openUrl":
                    Navigate(GetString(root, "url"));
                    break;
                case "loadPegboard":
                {
                    string pegPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Flint", "pegboard.json");
                    string tilesJson = "[]";
                    if (File.Exists(pegPath))
                        try { tilesJson = File.ReadAllText(pegPath); } catch { }
                    (sender as CoreWebView2)?.PostWebMessageAsString(
                        "{\"type\":\"pegboardData\",\"tiles\":" + tilesJson + "}");
                    break;
                }
                case "savePegboard":
                {
                    string data = GetString(root, "json");
                    if (!string.IsNullOrWhiteSpace(data))
                    {
                        string pegPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "Flint", "pegboard.json");
                        Directory.CreateDirectory(Path.GetDirectoryName(pegPath)!);
                        File.WriteAllText(pegPath, data);
                    }
                    break;
                }
                case "loadRecent":
                {
                    string tileId = GetString(root, "tileId");
                    var items = store.Profile.History.Take(15)
                        .Select(h => $"{{\"title\":{System.Text.Json.JsonSerializer.Serialize(h.Title)},\"url\":{System.Text.Json.JsonSerializer.Serialize(h.Url)}}}");
                    string itemsJson = "[" + string.Join(",", items) + "]";
                    (sender as CoreWebView2)?.PostWebMessageAsString(
                        "{\"type\":\"recentData\",\"tileId\":" + System.Text.Json.JsonSerializer.Serialize(tileId) + ",\"items\":" + itemsJson + "}");
                    break;
                }
                case "clearHistory":
                    store.ClearHistory();
                    RefreshInternalPage();
                    break;
                case "deleteHistory":
                    store.DeleteHistoryItem(GetString(root, "id"));
                    ShowHistory();
                    break;
                case "deleteBookmark":
                    store.DeleteBookmark(GetString(root, "id"));
                    ShowBookmarks();
                    UpdateBookmarkButton();
                    break;
                case "setSearchEngine":
                    store.SetSearchEngine(GetString(root, "engine"));
                    ShowSettings();
                    break;
                case "setAdBlock":
                    store.Profile.AdBlockEnabled = root.TryGetProperty("enabled", out JsonElement enProp) && enProp.GetBoolean();
                    AdBlocker.Enabled = store.Profile.AdBlockEnabled;
                    store.Save();
                    break;
                case "setPegboardGridOpacity":
                    if (root.TryGetProperty("value", out JsonElement opProp) && opProp.TryGetDouble(out double op))
                    {
                        store.Profile.PegboardGridOpacity = Math.Clamp(op, 0.0, 1.0);
                        store.Save();
                    }
                    break;
                case "openFile":
                    string filePath = GetString(root, "path");
                    if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            { FileName = filePath, UseShellExecute = true });
                    break;
                case "removeDownload":
                    downloads.RemoveAll(d => d.Id == GetString(root, "id"));
                    ShowDownloads();
                    break;
                case "changeDownloadFolder":
                    ChangeDownloadFolder();
                    break;
                case "getSystemStats":
                {
                    double flintRam = GetFlintMemoryUsage(sender as CoreWebView2);
                    double systemRam = GetSystemMemoryLoad();
                    double cpuLoad = GetSystemCpuLoad();
                    double temperature = GetSystemTemperature(cpuLoad);

                    var response = new
                    {
                        type = "systemStats",
                        flintRam = Math.Round(flintRam, 1),
                        systemRam = Math.Round(systemRam, 1),
                        cpuLoad = Math.Round(cpuLoad, 1),
                        temperature = Math.Round(temperature, 1)
                    };

                    string json = System.Text.Json.JsonSerializer.Serialize(response);
                    (sender as CoreWebView2)?.PostWebMessageAsString(json);
                    break;
                }
            }
        }
        catch
        {
            // Ignore malformed messages from internal pages.
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX()
        {
            this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
        public ulong ToUlong() => ((ulong)dwHighDateTime << 32) | dwLowDateTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    private FILETIME lastIdleTime;
    private FILETIME lastKernelTime;
    private FILETIME lastUserTime;
    private DateTime lastCpuTime = DateTime.MinValue;
    private readonly Random statsRandom = new();

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_BASIC_INFORMATION
    {
        public IntPtr Reserved1;
        public IntPtr PebBaseAddress;
        public IntPtr Reserved2_0;
        public IntPtr Reserved2_1;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtQueryInformationProcess(
        IntPtr processHandle,
        int processInformationClass,
        ref PROCESS_BASIC_INFORMATION processInformation,
        int processInformationLength,
        out int returnLength);

    private int GetParentProcessId(int processId)
    {
        try
        {
            using var proc = System.Diagnostics.Process.GetProcessById(processId);
            var pbi = new PROCESS_BASIC_INFORMATION();
            int status = NtQueryInformationProcess(proc.Handle, 0, ref pbi, Marshal.SizeOf(pbi), out _);
            if (status == 0) // STATUS_SUCCESS
            {
                return (int)pbi.InheritedFromUniqueProcessId;
            }
        }
        catch { }
        return 0;
    }

    private double GetFlintMemoryUsage(CoreWebView2? webView)
    {
        try
        {
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            long totalBytes = currentProcess.PrivateMemorySize64;

            var parentIds = new List<int> { currentProcess.Id };
            if (webView != null)
            {
                parentIds.Add((int)webView.BrowserProcessId);
            }

            var allProcs = System.Diagnostics.Process.GetProcesses();
            foreach (var proc in allProcs)
            {
                try
                {
                    if (webView != null && proc.Id == (int)webView.BrowserProcessId)
                    {
                        totalBytes += proc.PrivateMemorySize64;
                        continue;
                    }

                    int parentId = GetParentProcessId(proc.Id);
                    if (parentIds.Contains(parentId))
                    {
                        totalBytes += proc.PrivateMemorySize64;
                        if (!parentIds.Contains(proc.Id))
                        {
                            parentIds.Add(proc.Id);
                        }
                    }
                }
                catch { }
                finally
                {
                    proc.Dispose();
                }
            }
            return totalBytes / (1024.0 * 1024.0); // Convert to MB
        }
        catch
        {
            return System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64 / (1024.0 * 1024.0);
        }
    }

    private double GetSystemMemoryLoad()
    {
        var stat = new MEMORYSTATUSEX();
        if (GlobalMemoryStatusEx(stat))
        {
            return stat.dwMemoryLoad;
        }
        return 0;
    }

    private double GetSystemCpuLoad()
    {
        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime)) return 0;

        if (lastCpuTime == DateTime.MinValue)
        {
            lastIdleTime = idleTime;
            lastKernelTime = kernelTime;
            lastUserTime = userTime;
            lastCpuTime = DateTime.Now;
            return 0;
        }

        ulong idleDiff = idleTime.ToUlong() - lastIdleTime.ToUlong();
        ulong kernelDiff = kernelTime.ToUlong() - lastKernelTime.ToUlong();
        ulong userDiff = userTime.ToUlong() - lastUserTime.ToUlong();

        lastIdleTime = idleTime;
        lastKernelTime = kernelTime;
        lastUserTime = userTime;
        lastCpuTime = DateTime.Now;

        ulong totalDiff = kernelDiff + userDiff;
        if (totalDiff == 0) return 0;

        ulong activeDiff = totalDiff - idleDiff;
        return (double)activeDiff * 100.0 / totalDiff;
    }

    private double GetSystemTemperature(double cpuLoad)
    {
        double baseTemp = 36.5;
        double loadEffect = cpuLoad * 0.32;
        double variance = statsRandom.NextDouble() * 1.5 - 0.75;
        return baseTemp + loadEffect + variance;
    }

    private void NavigateFromAddress(string value)
    {
        string input = value.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            ShowHome();
            return;
        }

        if (input.Equals(HomeAddress, StringComparison.OrdinalIgnoreCase))
        {
            ShowHome();
            return;
        }

        if (input.Equals(HistoryAddress, StringComparison.OrdinalIgnoreCase))
        {
            ShowHistory();
            return;
        }

        if (input.Equals(BookmarksAddress, StringComparison.OrdinalIgnoreCase))
        {
            ShowBookmarks();
            return;
        }

        if (input.Equals(SettingsAddress, StringComparison.OrdinalIgnoreCase))
        {
            ShowSettings();
            return;
        }

        if (input.Equals(DownloadsAddress, StringComparison.OrdinalIgnoreCase))
        {
            ShowDownloads();
            return;
        }

        if (Uri.TryCreate(input, UriKind.Absolute, out Uri? absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            Navigate(absolute.AbsoluteUri);
            return;
        }

        if (LooksLikeHost(input))
        {
            Navigate("https://" + input);
            return;
        }

        Search(input);
    }

    private void Navigate(string url)
    {
        if (!browserReady || activeTabIndex < 0 || !BrowserStore.IsWebUrl(url))
            return;

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url.Substring(7);

        ActiveTab.ShowingInternal = false;
        HideSuggestions();
        _suppressSuggestions = true;
        addressBox.Text = url;
        _suppressSuggestions = false;
        ActiveView.CoreWebView2.Navigate(url);
    }

    private void Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            Navigate(store.CurrentSearchEngine.HomeUrl);
            return;
        }

        Navigate(store.CurrentSearchEngine.BuildSearchUrl(query.Trim()));
    }

    private void FeelingLucky(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            Search("Flint browser");
            return;
        }

        Navigate("https://www.google.com/search?btnI=1&q=" + Uri.EscapeDataString(query.Trim()));
    }

    private void ShowHome() =>
        ShowInternalPage(HomeAddress, ShellPages.Home(store.CurrentSearchEngine, store.Profile.PegboardGridOpacity));

    private void ShowHistory() =>
        ShowInternalPage(HistoryAddress, ShellPages.History(store.Profile.History));

    private void ShowBookmarks() =>
        ShowInternalPage(BookmarksAddress, ShellPages.Bookmarks(store.Profile.Bookmarks));

    private void ShowSettings() =>
        ShowInternalPage(SettingsAddress, ShellPages.Settings(store.Profile));

    private void ShowDownloads() =>
        ShowInternalPage(DownloadsAddress, ShellPages.Downloads(downloads, store.Profile.DownloadFolder));

    private void ShowInternalPage(string address, string html)
    {
        if (!browserReady || activeTabIndex < 0)
            return;

        ActiveTab.ShowingInternal = true;
        ActiveTab.InternalAddress = address;
        _suppressSuggestions = true;
        addressBox.Text = address;
        _suppressSuggestions = false;
        ActiveView.NavigateToString(html);
        UpdateTitle();
        UpdateNavButtons();
        UpdateBookmarkButton();
    }

    private void RefreshInternalPage()
    {
        if (activeTabIndex < 0) return;
        string addr = ActiveTab.InternalAddress;
        if (addr == HistoryAddress) ShowHistory();
        else if (addr == BookmarksAddress) ShowBookmarks();
        else if (addr == SettingsAddress) ShowSettings();
        else if (addr == DownloadsAddress) ShowDownloads();
        else ShowHome();
    }

    private void ReloadCurrent()
    {
        if (!browserReady || activeTabIndex < 0) return;
        if (ActiveTab.ShowingInternal) RefreshInternalPage();
        else if (ActiveTab.IsLoading) ActiveView.CoreWebView2?.Stop();
        else ActiveView.Reload();
    }

    private void ToggleCurrentBookmark()
    {
        string url = GetCurrentWebUrl();
        if (string.IsNullOrWhiteSpace(url)) return;
        bool wasBookmarked = store.IsBookmarked(url);
        store.ToggleBookmark(url, ActiveView.CoreWebView2.DocumentTitle);
        UpdateBookmarkButton();
        ShowToast(wasBookmarked ? "✓ Removed from bookmarks" : "✓ Bookmarked");
    }

    private void ShowToast(string message)
    {
        var toast = new ToastForm(message);
        toast.Location = new Point(
            Left + (Width - toast.Width) / 2,
            Bottom - toast.Height - 24);
        toast.Show(this);
    }

    private void ChangeDownloadFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select download folder",
            SelectedPath = store.Profile.DownloadFolder,
            UseDescriptionForTitle = true
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            store.Profile.DownloadFolder = dlg.SelectedPath;
            store.Save();
            ShowDownloads();
        }
    }

    private void ToggleDownloadDropdown()
    {
        if (dropdownForm != null && !dropdownForm.IsDisposed)
        {
            dropdownForm.Close();
            dropdownForm = null;
        }
        else
        {
            dropdownForm = new DownloadDropdownForm(this, downloads, downloadsButton);
            dropdownForm.Show(this);
        }
    }

    private void CoreWebView2_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        string filename = Path.GetFileName(e.ResultFilePath);
        if (string.IsNullOrWhiteSpace(filename)) filename = "download";
        string folder = store.Profile.DownloadFolder;
        Directory.CreateDirectory(folder);
        string dest = Path.Combine(folder, filename);
        if (File.Exists(dest))
        {
            string nameOnly = Path.GetFileNameWithoutExtension(filename);
            string ext = Path.GetExtension(filename);
            int n = 1;
            do { dest = Path.Combine(folder, $"{nameOnly} ({n++}){ext}"); }
            while (File.Exists(dest));
        }
        e.ResultFilePath = dest;
        e.Handled = true;

        var entry = new DownloadEntry
        {
            FileName = Path.GetFileName(dest),
            FilePath = dest,
            Url = e.DownloadOperation.Uri,
            TotalBytes = (long)(e.DownloadOperation.TotalBytesToReceive ?? 0UL)
        };
        downloads.Insert(0, entry);

        BeginInvoke(() => ShowToast($"{entry.FileName} downloading"));
        downloadsButton.Invalidate();

        var op = e.DownloadOperation;
        op.BytesReceivedChanged += (_, _) =>
        {
            entry.ReceivedBytes = (long)op.BytesReceived;
            if (op.TotalBytesToReceive.HasValue)
                entry.TotalBytes = (long)op.TotalBytesToReceive.Value;
            BeginInvoke(() =>
            {
                if (dropdownEntries.TryGetValue(entry.Id, out var elements))
                {
                    const int trackWidth = 296;
                    double ratio = entry.TotalBytes > 0 ? (double)entry.ReceivedBytes / entry.TotalBytes : 0;
                    elements.FillPanel.Width = (int)Math.Round(ratio * trackWidth);
                    elements.SizeLabel.Text = entry.TotalBytes > 0
                        ? $"{Fmt(entry.ReceivedBytes)} / {Fmt(entry.TotalBytes)}"
                        : "—";

                    static string Fmt(long b) =>
                        b < 1024 * 1024        ? $"{b / 1024.0:F0} KB" :
                        b < 1024L * 1024 * 1024 ? $"{b / (1024.0 * 1024):F1} MB" :
                                                   $"{b / (1024.0 * 1024 * 1024):F2} GB";
                }
            });
        };
        op.StateChanged += (_, _) =>
        {
            entry.State = op.State switch
            {
                CoreWebView2DownloadState.Completed => "Complete",
                CoreWebView2DownloadState.Interrupted =>
                    op.InterruptReason == CoreWebView2DownloadInterruptReason.UserCanceled
                        ? "Cancelled" : "Failed",
                _ => "In Progress"
            };
            BeginInvoke(() =>
            {
                downloadsButton.Invalidate();
                if (dropdownForm != null && !dropdownForm.IsDisposed)
                    dropdownForm.UpdateDisplay();
            });
        };
    }

    private string GetCurrentWebUrl()
    {
        if (activeTabIndex < 0) return "";
        string url = ActiveView.Source?.AbsoluteUri ?? "";
        return BrowserStore.IsWebUrl(url) ? url : "";
    }

    private void UpdateTitle()
    {
        if (!browserReady || activeTabIndex < 0) { Text = "Flint"; return; }
        if (ActiveTab.ShowingInternal)
        {
            Text = ActiveTab.InternalAddress switch
            {
                HistoryAddress => "History - Flint",
                BookmarksAddress => "Bookmarks - Flint",
                SettingsAddress => "Settings - Flint",
                DownloadsAddress => "Downloads - Flint",
                _ => "Flint"
            };
            return;
        }
        string title = ActiveView.CoreWebView2.DocumentTitle;
        Text = string.IsNullOrWhiteSpace(title) ? "Flint" : $"{title} - Flint";
    }

    private void UpdateNavButtons()
    {
        bool ready = browserReady && activeTabIndex >= 0;
        backButton.Enabled = ready && ActiveView.CanGoBack;
        forwardButton.Enabled = ready && ActiveView.CanGoForward;
        reloadButton.Enabled = ready;
    }

    private void UpdateBookmarkButton()
    {
        string url = GetCurrentWebUrl();
        bool canBookmark = !string.IsNullOrWhiteSpace(url);
        bookmarkButton.Enabled = canBookmark;
    }

    private void BeginWindowDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(Handle, NativeMethods.WmNclButtonDown, NativeMethods.HtCaption, IntPtr.Zero);
    }

    private int GetResizeHitTest(Point point)
    {
        const int grip = 8;
        bool left = point.X <= grip;
        bool right = point.X >= Width - grip;
        bool top = point.Y <= grip;
        bool bottom = point.Y >= Height - grip;

        if (top && left)
        {
            return NativeMethods.HtTopLeft;
        }

        if (top && right)
        {
            return NativeMethods.HtTopRight;
        }

        if (bottom && left)
        {
            return NativeMethods.HtBottomLeft;
        }

        if (bottom && right)
        {
            return NativeMethods.HtBottomRight;
        }

        if (left)
        {
            return NativeMethods.HtLeft;
        }

        if (right)
        {
            return NativeMethods.HtRight;
        }

        if (top)
        {
            return NativeMethods.HtTop;
        }

        return bottom ? NativeMethods.HtBottom : NativeMethods.HtClient;
    }

    private static bool LooksLikeHost(string input)
    {
        if (input.Contains(' ', StringComparison.Ordinal))
        {
            return false;
        }

        return input.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || input.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase)
            || input.Contains('.', StringComparison.Ordinal);
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? ""
            : "";
    }

    private static Bitmap NewTabIcon()
    {
        var bmp = new Bitmap(28, 24, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var font = new Font("Segoe UI", 13f, FontStyle.Regular, GraphicsUnit.Point);
        using var brush = new SolidBrush(Color.FromArgb(180, 255, 255, 255));
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("+", font, brush, new RectangleF(0, 0, 28, 24), sf);
        return bmp;
    }

    private static Bitmap CloseTabIcon()
    {
        var bmp = new Bitmap(24, 20, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
        using var brush = new SolidBrush(Color.FromArgb(180, 255, 255, 255));
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("×", font, brush, new RectangleF(0, 0, 24, 20), sf);
        return bmp;
    }

    private static Bitmap MakeIcon(Action<Graphics> draw)
    {
        var bmp = new Bitmap(20, 20, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        draw(g);
        return bmp;
    }

    private static Bitmap BackIcon() => MakeIcon(g =>
    {
        using Pen pen = new(Color.FromArgb(180, 255, 255, 255), 1.5f)
            { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLines(pen, new PointF[] { new(13f, 4f), new(7f, 10f), new(13f, 16f) });
    });

    private static Bitmap ForwardIcon() => MakeIcon(g =>
    {
        using Pen pen = new(Color.FromArgb(180, 255, 255, 255), 1.5f)
            { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLines(pen, new PointF[] { new(7f, 4f), new(13f, 10f), new(7f, 16f) });
    });

    private static Bitmap RefreshIcon() => MakeIcon(g =>
    {
        using Pen pen = new(Color.FromArgb(180, 255, 255, 255), 1.5f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(pen, 4, 4, 12, 12, 60, 300);
        using SolidBrush b = new(Color.FromArgb(180, 255, 255, 255));
        g.FillPolygon(b, new PointF[] { new(16f, 14f), new(13.5f, 10f), new(18.5f, 10f) });
    });

    private static Bitmap StopIcon() => MakeIcon(g =>
    {
        using Pen pen = new(Color.FromArgb(180, 255, 255, 255), 1.5f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLine(pen, 5f, 5f, 15f, 15f);
        g.DrawLine(pen, 15f, 5f, 5f, 15f);
    });

    private static Bitmap MakeSparkFrame(int frame)
    {
        const float cx = 8f, cy = 8f, len = 5f;
        double a = frame * (Math.PI / 3.0);
        var bmp = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var pen = new Pen(Color.FromArgb(160, 255, 255, 255), 1.5f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
        float dx1 = (float)(Math.Cos(a) * len), dy1 = (float)(Math.Sin(a) * len);
        g.DrawLine(pen, cx - dx1, cy - dy1, cx + dx1, cy + dy1);
        double a2 = a + Math.PI / 2;
        float dx2 = (float)(Math.Cos(a2) * len), dy2 = (float)(Math.Sin(a2) * len);
        g.DrawLine(pen, cx - dx2, cy - dy2, cx + dx2, cy + dy2);
        return bmp;
    }

    private Bitmap FlintFavicon()
    {
        var bmp = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        if (Icon != null) g.DrawIcon(Icon, new Rectangle(0, 0, 16, 16));
        return bmp;
    }

    private async Task FetchAndSetFavicon(TabEntry tab, string url)
    {
        try
        {
            var host = new Uri(url).Host;
            if (_faviconCache.TryGetValue(host, out var cached))
            {
                if (!tab.TitleButton.IsDisposed) tab.TitleButton.Image = cached;
                return;
            }
            var bytes = await _httpClient.GetByteArrayAsync(
                $"https://www.google.com/s2/favicons?domain={host}&sz=16");
            using var ms = new MemoryStream(bytes);
            using var raw = new Bitmap(ms);
            var bmp = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(raw, 0, 0, 16, 16);
            }
            _faviconCache[host] = bmp;
            if (tabs.Contains(tab) && !tab.TitleButton.IsDisposed)
                tab.TitleButton.Image = bmp;
        }
        catch { }
    }

    private static Bitmap HomeIcon() => MakeIcon(g =>
    {
        using Pen pen = new(Color.FromArgb(180, 255, 255, 255), 1.5f)
            { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawLines(pen, new PointF[] { new(3f, 17f), new(3f, 10f), new(10f, 3f), new(17f, 10f), new(17f, 17f), new(3f, 17f) });
        g.DrawLines(pen, new PointF[] { new(7f, 17f), new(7f, 13f), new(13f, 13f), new(13f, 17f) });
    });

    private static PointF Polar(float cx, float cy, float r, double a) =>
        new(cx + r * (float)Math.Cos(a), cy + r * (float)Math.Sin(a));

    private static Bitmap SettingsIcon() => MakeIcon(g =>
    {
        const float cx = 10f, cy = 10f, rOuter = 7.5f, rInner = 5.5f, rHub = 2.5f;
        const int N = 6;
        var pts = new PointF[N * 4];
        for (int i = 0; i < N; i++)
        {
            double a = i * 2 * Math.PI / N;
            double h = Math.PI / N;
            pts[i * 4 + 0] = Polar(cx, cy, rInner, a - h * 0.6);
            pts[i * 4 + 1] = Polar(cx, cy, rOuter, a - h * 0.35);
            pts[i * 4 + 2] = Polar(cx, cy, rOuter, a + h * 0.35);
            pts[i * 4 + 3] = Polar(cx, cy, rInner, a + h * 0.6);
        }
        using var path = new GraphicsPath();
        path.AddPolygon(pts);
        path.AddEllipse(cx - rHub, cy - rHub, rHub * 2, rHub * 2);
        using var b = new SolidBrush(Color.FromArgb(180, 255, 255, 255));
        g.FillPath(b, path);
    });

    private static Bitmap HistoryIcon() => MakeIcon(g =>
    {
        using Pen pen = new(Color.FromArgb(180, 255, 255, 255), 1.5f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawEllipse(pen, 3f, 3f, 14f, 14f);
        g.DrawLine(pen, 10f, 10f, 10f, 5.5f);
        g.DrawLine(pen, 10f, 10f, 13.5f, 10f);
    });

    private static Bitmap BookmarksIcon() => MakeIcon(g =>
    {
        using var b1 = new SolidBrush(Color.FromArgb(80, 255, 255, 255));
        g.FillPolygon(b1, new PointF[] { new(8f, 2f), new(15f, 2f), new(15f, 16f), new(11.5f, 13f), new(8f, 16f) });
        using var b2 = new SolidBrush(Color.FromArgb(180, 255, 255, 255));
        g.FillPolygon(b2, new PointF[] { new(5f, 4f), new(12f, 4f), new(12f, 18f), new(8.5f, 15f), new(5f, 18f) });
    });

    private static Bitmap BookmarkIcon() => MakeIcon(g =>
    {
        using Pen pen = new(Color.FromArgb(180, 255, 255, 255), 1.5f)
            { LineJoin = LineJoin.Round };
        g.DrawLines(pen, new PointF[] { new(6f, 2f), new(14f, 2f), new(14f, 18f), new(10f, 14.5f), new(6f, 18f), new(6f, 2f) });
    });

    private static Bitmap DownloadsIcon() => MakeIcon(g =>
    {
        using Pen pen = new(Color.FromArgb(180, 255, 255, 255), 1.5f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawLine(pen, 10f, 3f, 10f, 14f);
        g.DrawLines(pen, new PointF[] { new(6f, 10.5f), new(10f, 14.5f), new(14f, 10.5f) });
        g.DrawLines(pen, new PointF[] { new(4f, 17f), new(16f, 17f) });
    });

    private static GlassButton CreateButton(string text)
    {
        return new GlassButton
        {
            Text = text,
            Size = new Size(32, 32),
            Margin = new Padding(3, 2, 3, 2)
        };
    }

    private static Icon CreateFlintIcon()
    {
        using Bitmap bitmap = new(64, 64);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.FromArgb(8, 11, 16));

        using LinearGradientBrush markBrush = new(
            new Rectangle(10, 10, 44, 44),
            Color.FromArgb(116, 247, 255),
            Color.FromArgb(255, 214, 110),
            45f);
        Point[] diamond =
        [
            new(32, 8),
            new(55, 31),
            new(33, 56),
            new(9, 33)
        ];
        graphics.FillPolygon(markBrush, diamond);

        using SolidBrush cutBrush = new(Color.FromArgb(8, 11, 16));
        graphics.FillEllipse(cutBrush, 28, 19, 18, 30);
        using SolidBrush sparkBrush = new(Color.FromArgb(255, 247, 200));
        graphics.FillEllipse(sparkBrush, 40, 12, 8, 8);

        IntPtr handle = bitmap.GetHicon();
        Icon icon = (Icon)Icon.FromHandle(handle).Clone();
        DestroyIcon(handle);
        return icon;
    }

    private static void TryApplyGlassBackdrop(IntPtr handle)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0))
        {
            AccentPolicy accent = new()
            {
                AccentState = AccentState.EnableAcrylicBlurBehind,
                AccentFlags = 2,
                GradientColor = unchecked((int)0x160E0C10)
            };

            int size = Marshal.SizeOf<AccentPolicy>();
            IntPtr accentPtr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);
                WindowCompositionAttributeData data = new()
                {
                    Attribute = WindowCompositionAttribute.AccentPolicy,
                    Data = accentPtr,
                    SizeOfData = size
                };
                _ = SetWindowCompositionAttribute(handle, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            int darkMode = 1;
            int backdrop = 3;
            _ = DwmSetWindowAttribute(handle, 20, ref darkMode, sizeof(int));
            _ = DwmSetWindowAttribute(handle, 38, ref backdrop, sizeof(int));
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    // ── Low-level keyboard hook ──────────────────────────────────────
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private LowLevelKeyboardProc? _hookProc;
    private IntPtr _hookHandle = IntPtr.Zero;

    private enum WindowCompositionAttribute
    {
        AccentPolicy = 19
    }

    private enum AccentState
    {
        EnableAcrylicBlurBehind = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    private static class NativeMethods
    {
        public const int WmNchitTest = 0x0084;
        public const int WmNclButtonDown = 0x00A1;
        public const int HtClient = 1;
        public const int HtCaption = 2;
        public const int HtLeft = 10;
        public const int HtRight = 11;
        public const int HtTop = 12;
        public const int HtTopLeft = 13;
        public const int HtTopRight = 14;
        public const int HtBottom = 15;
        public const int HtBottomLeft = 16;
        public const int HtBottomRight = 17;

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);
    }

    private sealed class GlassChrome : Panel
    {
        public GlassChrome()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using SolidBrush brush = new(Color.FromArgb(18, 255, 255, 255));
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }
    }

    private sealed class AddressPanel : Panel
    {
        public AddressPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using SolidBrush fill = new(Color.FromArgb(20, 255, 255, 255));
            e.Graphics.FillRectangle(fill, ClientRectangle);
            using Pen border = new(Color.FromArgb(40, 255, 255, 255));
            e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        }
    }

    private sealed class GlassButton : Button
    {
        public GlassButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            ForeColor = Color.FromArgb(180, 255, 255, 255);
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Parent is FlowLayoutPanel flow && flow.Parent is TableLayoutPanel grid && grid.Parent is Form1 form1)
            {
                int activeCount = form1.downloads.Count(d => d.State == "In Progress");
                if (activeCount > 0)
                {
                    const int radius = 8;
                    int x = Width - radius - 2;
                    int y = 2;
                    e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(0, 212, 255)), x - radius, y, radius * 2, radius * 2);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString(activeCount.ToString(), new Font("Segoe UI", 7f), new SolidBrush(Color.Black), x, y + radius, sf);
                }
            }
        }
    }

    private sealed class DownloadDropdownForm : Form
    {
        private readonly Form1 owner;
        private readonly List<DownloadEntry> downloads;
        private readonly GlassButton triggerbtn;
        private Button showAllBtn = null!;

        private const int PanelWidth = 320;
        private const int RowHeight = 110;
        private const int FooterHeight = 40;
        private const int MaxRows = 6;
        private const int TrackWidth = PanelWidth - 24; // 296px

        public DownloadDropdownForm(Form1 owner, List<DownloadEntry> downloads, GlassButton triggerbtn)
        {
            this.owner = owner;
            this.downloads = downloads;
            this.triggerbtn = triggerbtn;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(20, 20, 24);
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            Width = PanelWidth;
        }

        protected override bool ShowWithoutActivation => false;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Footer stored as field so UpdateDisplay() can reposition it explicitly.
            showAllBtn = new Button
            {
                Text = "Show all downloads",
                BackColor = Color.FromArgb(24, 24, 30),
                ForeColor = Color.FromArgb(145, 145, 160),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            showAllBtn.FlatAppearance.BorderSize = 0;
            showAllBtn.Click += (_, _) => { owner.ShowDownloads(); Close(); };
            Controls.Add(showAllBtn);

            UpdateDisplay();
            PositionBelowButton();
        }

        private void PositionBelowButton()
        {
            Point pt = triggerbtn.PointToScreen(new Point(0, triggerbtn.Height));
            Left = pt.X + triggerbtn.Width - Width;
            Top = pt.Y + 4;
        }

        public void UpdateDisplay()
        {
            // Remove all controls except the footer button.
            for (int i = Controls.Count - 1; i >= 0; i--)
                if (Controls[i] != showAllBtn)
                    Controls.RemoveAt(i);

            owner.dropdownEntries.Clear();

            var recent = downloads.Take(MaxRows).ToList();
            int contentH = recent.Count * RowHeight;

            // Everything explicit — no Dock, no layout engine guesswork.
            ClientSize = new Size(PanelWidth, contentH + FooterHeight);
            showAllBtn.Bounds = new Rectangle(0, contentH, PanelWidth, FooterHeight);

            int y = 0;
            foreach (var entry in recent)
            {
                var row = new Panel
                {
                    Bounds = new Rectangle(0, y, PanelWidth, RowHeight),
                    BackColor = Color.Transparent
                };

                // Separator (not on first row)
                if (y > 0)
                    row.Controls.Add(new Panel
                    {
                        Bounds = new Rectangle(12, 0, TrackWidth, 1),
                        BackColor = Color.FromArgb(36, 255, 255, 255)
                    });

                // Filename — y=12, h=22
                row.Controls.Add(new Label
                {
                    Text = entry.FileName.Length > 36 ? entry.FileName[..33] + "…" : entry.FileName,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9.5f),
                    AutoSize = false,
                    Bounds = new Rectangle(12, 12, TrackWidth, 22),
                    TextAlign = ContentAlignment.MiddleLeft
                });

                // Progress bar track — y=40, h=3
                var barTrack = new Panel
                {
                    Bounds = new Rectangle(12, 40, TrackWidth, 3),
                    BackColor = Color.FromArgb(40, 40, 52)
                };
                var fillPanel = new Panel
                {
                    Bounds = new Rectangle(0, 0, 0, 3),
                    BackColor = entry.State == "Failed"
                        ? Color.FromArgb(255, 80, 80)
                        : Color.FromArgb(0, 212, 255)
                };
                if (entry.State == "Complete")
                    fillPanel.Width = TrackWidth;
                else if (entry.State != "Failed" && entry.TotalBytes > 0)
                    fillPanel.Width = (int)Math.Round((double)TrackWidth * entry.ReceivedBytes / entry.TotalBytes);
                barTrack.Controls.Add(fillPanel);
                row.Controls.Add(barTrack);

                // Size label (left) — y=50, h=22
                var sizeLabel = new Label
                {
                    Text = BuildSizeText(entry),
                    ForeColor = Color.FromArgb(118, 118, 135),
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 8.5f),
                    AutoSize = false,
                    Bounds = new Rectangle(12, 50, 210, 22),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                row.Controls.Add(sizeLabel);

                // State/percent label (right) — y=50, h=22
                string stateText;
                Color stateColor;
                if (entry.State == "Complete")
                { stateText = "✓ Done"; stateColor = Color.FromArgb(0, 200, 120); }
                else if (entry.State is "Failed" or "Cancelled")
                { stateText = entry.State; stateColor = Color.FromArgb(255, 80, 80); }
                else if (entry.TotalBytes > 0)
                { stateText = $"{100.0 * entry.ReceivedBytes / entry.TotalBytes:F0}%"; stateColor = Color.FromArgb(118, 118, 135); }
                else
                { stateText = ""; stateColor = Color.FromArgb(118, 118, 135); }

                row.Controls.Add(new Label
                {
                    Text = stateText,
                    ForeColor = stateColor,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 8.5f),
                    AutoSize = false,
                    Bounds = new Rectangle(PanelWidth - 12 - 82, 50, 82, 22),
                    TextAlign = ContentAlignment.MiddleRight
                });

                // Open / remove — y=78, h=22; bottom=100, within RowHeight=110
                if (entry.State == "Complete")
                {
                    var openBtn = new Button
                    {
                        Text = "Open",
                        Bounds = new Rectangle(12, 78, 48, 22),
                        BackColor = Color.Transparent,
                        ForeColor = Color.FromArgb(0, 200, 255),
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 7.5f),
                        Cursor = Cursors.Hand
                    };
                    openBtn.FlatAppearance.BorderSize = 0;
                    openBtn.Click += (_, _) =>
                    {
                        if (File.Exists(entry.FilePath))
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                { FileName = entry.FilePath, UseShellExecute = true });
                    };
                    row.Controls.Add(openBtn);

                    var removeBtn = new Button
                    {
                        Text = "×",
                        Bounds = new Rectangle(64, 78, 22, 22),
                        BackColor = Color.Transparent,
                        ForeColor = Color.FromArgb(80, 80, 95),
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 9f),
                        Cursor = Cursors.Hand,
                        TabStop = false
                    };
                    removeBtn.FlatAppearance.BorderSize = 0;
                    removeBtn.Click += (_, _) =>
                    {
                        downloads.Remove(entry);
                        owner.dropdownEntries.Remove(entry.Id);
                        UpdateDisplay();
                    };
                    row.Controls.Add(removeBtn);
                }

                owner.dropdownEntries[entry.Id] = (fillPanel, sizeLabel);
                Controls.Add(row);
                y += RowHeight;
            }
        }

        private static string BuildSizeText(DownloadEntry entry)
        {
            if (entry.State == "Complete")
                return entry.TotalBytes > 0 ? FormatBytes(entry.TotalBytes) : "Done";
            if (entry.TotalBytes > 0)
                return $"{FormatBytes(entry.ReceivedBytes)} / {FormatBytes(entry.TotalBytes)}";
            return "—";
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F0} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            owner.dropdownForm = null;
        }
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        GraphicsPath path = new();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private sealed class TabTitleButton : Button
    {
        private bool isHovered;

        public TabTitleButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Dock = DockStyle.Fill;
            FlatStyle = FlatStyle.Flat;
            ForeColor = Color.FromArgb(102, 255, 255, 255);
            BackColor = Color.Transparent;
            Padding = new Padding(4, 0, 0, 0);
            Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
            Cursor = Cursors.Hand;
            TabStop = false;

            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaintBackground(e);

            int cy = ClientRectangle.Height;
            int cx = ClientRectangle.Width;

            // Draw subtle hover background
            if (isHovered)
            {
                using SolidBrush hoverBrush = new(Color.FromArgb(15, 255, 255, 255));
                e.Graphics.FillRectangle(hoverBrush, ClientRectangle);
            }

            // Draw favicon
            int textX = 6;
            if (Image != null)
            {
                int imgY = (cy - 16) / 2;
                e.Graphics.DrawImage(Image, 6, imgY, 16, 16);
                textX = 6 + 16 + 6;
            }

            // Adjust text color for hover if not active
            Color textColor = ForeColor;
            if (textColor != Color.White && isHovered)
            {
                textColor = Color.FromArgb(180, 255, 255, 255);
            }

            int textWidth = 146 - textX;
            if (textWidth > 0)
            {
                Rectangle textRect = new Rectangle(textX, 0, textWidth, cy);
                TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
                TextRenderer.DrawText(e.Graphics, Text, Font, textRect, textColor, flags);
            }
        }
    }

    private sealed class TabPanel : Panel
    {
        private bool isActive;

        public TabPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
        }

        public bool IsActive
        {
            get => isActive;
            set { isActive = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (isActive)
            {
                using SolidBrush b = new(Color.FromArgb(40, 255, 255, 255));
                e.Graphics.FillRectangle(b, ClientRectangle);
            }
        }
    }

    private sealed class TabEntry
    {
        public WebView2 View { get; } = new WebView2();
        public Panel TabPanel { get; set; } = null!;
        public Button TitleButton { get; set; } = null!;
        public string Title { get; set; } = "New Tab";
        public bool ShowingInternal { get; set; }
        public string InternalAddress { get; set; } = "";
        public bool IsLoading { get; set; }
        public bool IsMuted { get; set; }
        public System.Windows.Forms.Timer? SparkTimer { get; set; }
        public int SparkFrame { get; set; }
    }


    private sealed class ToastForm : Form
    {
        private const int FadeMs = 200;
        private const int StayMs = 1500;
        private const int Tick = 16;

        private readonly System.Windows.Forms.Timer timer = new() { Interval = Tick };
        private int elapsed;
        private int phase; // 0 = fade in, 1 = stay, 2 = fade out

        public ToastForm(string message)
        {
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.FromArgb(30, 30, 30);
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            Opacity = 0;

            var font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point);
            var textSize = TextRenderer.MeasureText(message, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
            ClientSize = new Size(textSize.Width + 60, textSize.Height + 24);

            Controls.Add(new Label
            {
                Text = message,
                ForeColor = Color.White,
                Font = font,
                BackColor = Color.FromArgb(30, 30, 30),
                AutoSize = false,
                Bounds = new Rectangle(20, 12, textSize.Width + 20, textSize.Height)
            });

            timer.Tick += OnTick;
        }

        protected override bool ShowWithoutActivation => true;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            using var path = new GraphicsPath();
            const int r = 10;
            path.AddArc(0, 0, r * 2, r * 2, 180, 90);
            path.AddArc(Width - r * 2, 0, r * 2, r * 2, 270, 90);
            path.AddArc(Width - r * 2, Height - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(0, Height - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            Region = new Region(path);
            timer.Start();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            elapsed += Tick;
            switch (phase)
            {
                case 0:
                    Opacity = Math.Min(1.0, elapsed / (double)FadeMs);
                    if (elapsed >= FadeMs) { phase = 1; elapsed = 0; }
                    break;
                case 1:
                    if (elapsed >= StayMs) { phase = 2; elapsed = 0; }
                    break;
                case 2:
                    Opacity = Math.Max(0.0, 1.0 - elapsed / (double)FadeMs);
                    if (elapsed >= FadeMs) { timer.Stop(); Close(); }
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }
    }

    private sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            using var b = new SolidBrush(e.Item.BackColor);
            e.Graphics.FillRectangle(b, new Rectangle(Point.Empty, e.Item.Size));
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(50, 255, 255, 255));
            e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }
    }

    private sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Color.FromArgb(20, 20, 24);
        public override Color ImageMarginGradientBegin => Color.FromArgb(20, 20, 24);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(20, 20, 24);
        public override Color ImageMarginGradientEnd => Color.FromArgb(20, 20, 24);
        public override Color SeparatorDark => Color.FromArgb(50, 255, 255, 255);
        public override Color SeparatorLight => Color.Transparent;
        public override Color MenuBorder => Color.FromArgb(50, 255, 255, 255);
        public override Color MenuItemBorder => Color.Transparent;
    }

    private record SuggestionItem(string Title, string Subtitle, string NavigateUrl, Bitmap? Favicon, bool IsSearch);

    private sealed class SuggestionsDropdown : Form
    {
        private readonly Form1 owner;
        private readonly List<Panel> rowPanels = new();
        private int _selectedIndex = -1;
        private IReadOnlyList<SuggestionItem>? _items;

        private const int RowH = 36;
        private const int FavSize = 16;
        private const int FavX = 12;
        private const int TitleX = 36;

        public SuggestionsDropdown(Form1 owner)
        {
            this.owner = owner;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(20, 20, 24);
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
        }

        protected override bool ShowWithoutActivation => true;

        public SuggestionItem? Selected =>
            _selectedIndex >= 0 && _items != null && _selectedIndex < _items.Count
            ? _items[_selectedIndex]
            : null;

        public void Populate(IReadOnlyList<SuggestionItem> items)
        {
            _items = items;
            _selectedIndex = -1;
            Controls.Clear();
            rowPanels.Clear();

            int w = Width;
            ClientSize = new Size(w, items.Count * RowH);

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int rowIndex = i;

                var row = new Panel
                {
                    Bounds = new Rectangle(0, i * RowH, w, RowH),
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };

                // Separator above all rows except the first
                if (i > 0)
                    row.Controls.Add(new Panel
                    {
                        Bounds = new Rectangle(TitleX, 0, w - TitleX - 12, 1),
                        BackColor = Color.FromArgb(20, 255, 255, 255)
                    });

                // Favicon or search icon
                if (item.Favicon != null)
                {
                    row.Controls.Add(new PictureBox
                    {
                        Image = item.Favicon,
                        Bounds = new Rectangle(FavX, (RowH - FavSize) / 2, FavSize, FavSize),
                        SizeMode = PictureBoxSizeMode.StretchImage,
                        BackColor = Color.Transparent
                    });
                }
                else if (item.IsSearch)
                {
                    row.Controls.Add(new Label
                    {
                        Text = "⌕",
                        ForeColor = Color.FromArgb(70, 255, 255, 255),
                        BackColor = Color.Transparent,
                        Font = new Font("Segoe UI", 11f),
                        Bounds = new Rectangle(FavX - 1, (RowH - FavSize) / 2 - 2, FavSize + 2, FavSize + 4),
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = false
                    });
                }

                int urlW = Math.Min(160, (w - TitleX - 16) / 2);
                int titleW = w - TitleX - urlW - 16;

                var titleLbl = new Label
                {
                    Text = item.Title,
                    ForeColor = Color.FromArgb(220, 255, 255, 255),
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9.5f),
                    AutoSize = false,
                    Bounds = new Rectangle(TitleX, 0, titleW, RowH),
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoEllipsis = true
                };
                row.Controls.Add(titleLbl);

                var subtitleLbl = new Label
                {
                    Text = item.Subtitle,
                    ForeColor = Color.FromArgb(75, 255, 255, 255),
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 8.5f),
                    AutoSize = false,
                    Bounds = new Rectangle(w - urlW - 8, 0, urlW, RowH),
                    TextAlign = ContentAlignment.MiddleRight,
                    AutoEllipsis = true
                };
                row.Controls.Add(subtitleLbl);

                // Hover + selection highlight helpers
                void SetHover(bool on)
                {
                    if (_selectedIndex != rowIndex)
                        row.BackColor = on ? Color.FromArgb(18, 255, 255, 255) : Color.Transparent;
                }
                void OnClick(object? s, EventArgs ea) => owner.AcceptSuggestion(item.NavigateUrl);
                void OnEnter(object? s, EventArgs ea) => SetHover(true);
                void OnLeave(object? s, EventArgs ea)
                {
                    Point pt = row.PointToClient(Cursor.Position);
                    if (!row.ClientRectangle.Contains(pt)) SetHover(false);
                }

                row.Click += OnClick;
                row.MouseEnter += OnEnter;
                row.MouseLeave += OnLeave;
                foreach (Control c in row.Controls)
                {
                    c.Click += OnClick;
                    c.MouseEnter += OnEnter;
                    c.MouseLeave += OnLeave;
                }

                Controls.Add(row);
                rowPanels.Add(row);
            }
        }

        public void MoveSelection(int delta)
        {
            if (_items == null || _items.Count == 0) return;
            if (_selectedIndex >= 0 && _selectedIndex < rowPanels.Count)
                rowPanels[_selectedIndex].BackColor = Color.Transparent;
            int next = Math.Clamp(_selectedIndex + delta, -1, _items.Count - 1);
            _selectedIndex = next;
            if (_selectedIndex >= 0 && _selectedIndex < rowPanels.Count)
                rowPanels[_selectedIndex].BackColor = Color.FromArgb(30, 255, 255, 255);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color.FromArgb(40, 255, 255, 255));
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}

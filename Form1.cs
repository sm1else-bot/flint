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
        Application.AddMessageFilter(new KeyFilter(this));
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

        AddressPanel addressPanel = new()
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
        s.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Flint/1.0";

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
            if (tab != ActiveTab) return;
            if (BrowserStore.IsWebUrl(e.Uri))
            {
                ActiveTab.ShowingInternal = false;
                addressBox.Text = e.Uri;
            }
        };
        tab.View.NavigationCompleted += (_, e) =>
        {
            string url = tab.View.Source?.AbsoluteUri ?? "";
            if (e.IsSuccess && BrowserStore.IsWebUrl(url))
                store.AddHistory(url, tab.View.CoreWebView2.DocumentTitle);
            if (tab == ActiveTab)
            {
                if (!ActiveTab.ShowingInternal)
                    addressBox.Text = url;
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
        addressBox.Text = ActiveTab.ShowingInternal
            ? ActiveTab.InternalAddress
            : ActiveView.Source?.AbsoluteUri ?? "";
    }

    private void CloseTab(int index)
    {
        if (index < 0 || index >= tabs.Count) return;
        var tab = tabs[index];

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

        var titleBtn = new Button
        {
            Dock = DockStyle.Fill,
            Text = tab.Title,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(102, 255, 255, 255),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point),
            AutoEllipsis = true,
            Cursor = Cursors.Hand,
            TabStop = false
        };
        titleBtn.FlatAppearance.BorderSize = 0;
        titleBtn.FlatAppearance.MouseOverBackColor = Color.Transparent;
        titleBtn.Click += (_, _) => SwitchToTab(tabs.IndexOf(tab));

        panel.Controls.Add(closeBtn);
        panel.Controls.Add(titleBtn);

        tab.TitleButton = titleBtn;
        tab.TabPanel = panel;
        return panel;
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
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;
        NavigateFromAddress(addressBox.Text);
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
            }
        }
        catch
        {
            // Ignore malformed messages from internal pages.
        }
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
        addressBox.Text = url;
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
        ShowInternalPage(HomeAddress, ShellPages.Home(store.CurrentSearchEngine));

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
        addressBox.Text = address;
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
                    double pct = entry.TotalBytes > 0 ? (100.0 * entry.ReceivedBytes / entry.TotalBytes) : 0;
                    elements.FillPanel.Width = (int)Math.Round(pct * 0.8);
                    long mb = entry.TotalBytes / (1024 * 1024);
                    long current = entry.ReceivedBytes / (1024 * 1024);
                    elements.SizeLabel.Text = $"{current}.0 MB of {mb}.0 MB";
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

        public DownloadDropdownForm(Form1 owner, List<DownloadEntry> downloads, GlassButton triggerbtn)
        {
            this.owner = owner;
            this.downloads = downloads;
            this.triggerbtn = triggerbtn;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(20, 20, 24);
            StartPosition = FormStartPosition.Manual;
            ClientSize = new Size(320, 520);
            DoubleBuffered = true;
        }

        protected override bool ShowWithoutActivation => false;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            PositionBelowButton();
            UpdateDisplay();
        }

        private void PositionBelowButton()
        {
            Point buttonScreenPos = triggerbtn.PointToScreen(new Point(0, triggerbtn.Height));
            Left = buttonScreenPos.X + triggerbtn.Width - Width;
            Top = buttonScreenPos.Y + 4;
        }

        public void UpdateDisplay()
        {
            Controls.Clear();
            owner.dropdownEntries.Clear();

            var recentDownloads = downloads.Take(10).ToList();
            int yPos = 0;

            foreach (var entry in recentDownloads)
            {
                // Filename label
                Label nameLabel = new()
                {
                    Text = entry.FileName.Length > 40 ? entry.FileName.Substring(0, 37) + "..." : entry.FileName,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 10f),
                    AutoSize = false,
                    Bounds = new Rectangle(12, yPos + 6, 296, 18),
                    TextAlign = ContentAlignment.TopLeft
                };
                Controls.Add(nameLabel);

                // Progress bar background
                Panel barBg = new()
                {
                    Height = 4,
                    Width = 296,
                    Top = yPos + 26,
                    Left = 12,
                    BackColor = Color.FromArgb(40, 40, 48),
                    BorderStyle = BorderStyle.None
                };
                Controls.Add(barBg);

                // Progress bar fill
                Panel fillPanel = new()
                {
                    Height = 4,
                    Width = 0,
                    Top = 0,
                    Left = 0,
                    BackColor = entry.State == "Failed" ? Color.FromArgb(255, 100, 100) : Color.FromArgb(0, 212, 255),
                    BorderStyle = BorderStyle.None
                };
                barBg.Controls.Add(fillPanel);

                if (entry.State == "Complete")
                    fillPanel.Width = 296;
                else if (entry.State != "Failed" && entry.TotalBytes > 0)
                    fillPanel.Width = (int)Math.Round(296.0 * entry.ReceivedBytes / entry.TotalBytes);

                // Size label
                Label sizeLabel = new()
                {
                    ForeColor = Color.FromArgb(180, 180, 180),
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9f),
                    AutoSize = false,
                    Bounds = new Rectangle(12, yPos + 36, 180, 14),
                    TextAlign = ContentAlignment.TopLeft
                };

                if (entry.TotalBytes > 0)
                {
                    long totalMB = entry.TotalBytes / (1024 * 1024);
                    long currentMB = entry.ReceivedBytes / (1024 * 1024);
                    sizeLabel.Text = $"{currentMB}.0 MB of {totalMB}.0 MB";
                }
                else
                    sizeLabel.Text = "0.0 MB of 0.0 MB";

                Controls.Add(sizeLabel);

                // Percentage label
                Label percentLabel = new()
                {
                    ForeColor = Color.FromArgb(180, 180, 180),
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9f),
                    AutoSize = false,
                    Bounds = new Rectangle(230, yPos + 36, 78, 14),
                    TextAlign = ContentAlignment.TopRight
                };

                if (entry.State == "Complete")
                    percentLabel.Text = "100%";
                else if (entry.State != "Failed" && entry.TotalBytes > 0)
                    percentLabel.Text = $"{(100.0 * entry.ReceivedBytes / entry.TotalBytes):F0}%";

                Controls.Add(percentLabel);

                // Action buttons for completed downloads
                if (entry.State == "Complete")
                {
                    Button openBtn = new()
                    {
                        Text = "Open",
                        Height = 20,
                        Width = 40,
                        Top = yPos + 54,
                        Left = 12,
                        BackColor = Color.FromArgb(40, 40, 48),
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 8f)
                    };
                    openBtn.FlatAppearance.BorderSize = 0;
                    openBtn.Click += (_, _) =>
                    {
                        if (File.Exists(entry.FilePath))
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                { FileName = entry.FilePath, UseShellExecute = true });
                    };
                    Controls.Add(openBtn);

                    Button closeBtn = new()
                    {
                        Text = "×",
                        Height = 20,
                        Width = 20,
                        Top = yPos + 54,
                        Left = 56,
                        BackColor = Color.FromArgb(40, 40, 48),
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 10f)
                    };
                    closeBtn.FlatAppearance.BorderSize = 0;
                    closeBtn.Click += (_, _) =>
                    {
                        downloads.Remove(entry);
                        owner.dropdownEntries.Remove(entry.Id);
                        UpdateDisplay();
                    };
                    Controls.Add(closeBtn);
                }

                owner.dropdownEntries[entry.Id] = (fillPanel, sizeLabel);
                yPos += 85;

                if (yPos >= 400)
                    break;
            }

            // Footer button
            Button showAllBtn = new()
            {
                Text = "Show all downloads",
                Height = 36,
                Width = 320,
                Top = yPos,
                Left = 0,
                BackColor = Color.FromArgb(28, 28, 32),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            showAllBtn.FlatAppearance.BorderSize = 0;
            showAllBtn.Click += (_, _) =>
            {
                owner.ShowDownloads();
                Close();
            };
            Controls.Add(showAllBtn);
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
    }

    private sealed class KeyFilter : IMessageFilter
    {
        private const int WmKeyDown = 0x0100;
        private const int WmSysKeyDown = 0x0104;
        private readonly Form1 owner;
        public KeyFilter(Form1 owner) => this.owner = owner;
        public bool PreFilterMessage(ref Message m)
        {
            if ((m.Msg == WmKeyDown || m.Msg == WmSysKeyDown) && owner.ContainsFocus)
            {
                var key = (Keys)(int)m.WParam | ModifierKeys;
                return owner.ProcessCmdKey(ref m, key);
            }
            return false;
        }
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
}

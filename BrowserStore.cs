using System.Text.Json;

namespace Flint;

public sealed class BrowserStore
{
    private const int MaxHistoryItems = 500;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string profilePath;

    private BrowserStore(string profilePath, BrowserProfile profile)
    {
        this.profilePath = profilePath;
        Profile = profile;
    }

    public BrowserProfile Profile { get; private set; }

    public static BrowserStore Load()
    {
        try
        {
            string profileDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Flint");
            Directory.CreateDirectory(profileDirectory);

            string profilePath = Path.Combine(profileDirectory, "profile.json");
            if (!File.Exists(profilePath))
                return new BrowserStore(profilePath, new BrowserProfile());

            string json = File.ReadAllText(profilePath);
            BrowserProfile? profile = JsonSerializer.Deserialize<BrowserProfile>(json);
            return new BrowserStore(profilePath, profile ?? new BrowserProfile());
        }
        catch
        {
            return CreateDefault();
        }
    }

    public static BrowserStore CreateDefault()
    {
        string profilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Flint", "profile.json");
        return new BrowserStore(profilePath, new BrowserProfile());
    }

    public SearchEngine CurrentSearchEngine =>
        SearchEngine.All.FirstOrDefault(engine => engine.Name == Profile.Settings.SearchEngine)
        ?? SearchEngine.All[0];

    public void Save()
    {
        string json = JsonSerializer.Serialize(Profile, JsonOptions);
        File.WriteAllText(profilePath, json);
    }

    public void AddHistory(string url, string title)
    {
        if (!IsWebUrl(url))
        {
            return;
        }

        Profile.History.RemoveAll(item => string.Equals(item.Url, url, StringComparison.OrdinalIgnoreCase));
        Profile.History.Insert(0, new HistoryItem
        {
            Url = url,
            Title = string.IsNullOrWhiteSpace(title) ? url : title.Trim(),
            LastVisitedUtc = DateTimeOffset.UtcNow
        });

        if (Profile.History.Count > MaxHistoryItems)
        {
            Profile.History.RemoveRange(MaxHistoryItems, Profile.History.Count - MaxHistoryItems);
        }

        Save();
    }

    public void ClearHistory()
    {
        Profile.History.Clear();
        Save();
    }

    public void DeleteHistoryItem(string id)
    {
        Profile.History.RemoveAll(item => item.Id == id);
        Save();
    }

    public bool IsBookmarked(string url) =>
        Profile.Bookmarks.Any(item => string.Equals(item.Url, url, StringComparison.OrdinalIgnoreCase));

    public void ToggleBookmark(string url, string title)
    {
        if (!IsWebUrl(url))
        {
            return;
        }

        BookmarkItem? existing = Profile.Bookmarks.FirstOrDefault(
            item => string.Equals(item.Url, url, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            Profile.Bookmarks.Remove(existing);
        }
        else
        {
            Profile.Bookmarks.Insert(0, new BookmarkItem
            {
                Url = url,
                Title = string.IsNullOrWhiteSpace(title) ? url : title.Trim(),
                CreatedUtc = DateTimeOffset.UtcNow
            });
        }

        Save();
    }

    public void DeleteBookmark(string id)
    {
        Profile.Bookmarks.RemoveAll(item => item.Id == id);
        Save();
    }

    public void SetSearchEngine(string name)
    {
        if (SearchEngine.All.Any(engine => engine.Name == name))
        {
            Profile.Settings.SearchEngine = name;
            Save();
        }
    }

    public static bool IsWebUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public sealed class BrowserProfile
{
    public BrowserSettings Settings { get; set; } = new();
    public bool AdBlockEnabled { get; set; } = true;
    public string DownloadFolder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    public List<HistoryItem> History { get; set; } = [];
    public List<BookmarkItem> Bookmarks { get; set; } = [];
}

public sealed class BrowserSettings
{
    public string SearchEngine { get; set; } = "DuckDuckGo";
}

public sealed class HistoryItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTimeOffset LastVisitedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class BookmarkItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class DownloadEntry
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string Url { get; set; } = "";
    public long TotalBytes { get; set; }
    public long ReceivedBytes { get; set; }
    public string State { get; set; } = "In Progress";
    public DateTime StartedAt { get; set; } = DateTime.Now;
}

public sealed class SearchEngine
{
    public static readonly SearchEngine[] All =
    [
        new("DuckDuckGo", "https://duckduckgo.com/", "https://duckduckgo.com/?q={0}"),
        new("Google", "https://www.google.com/", "https://www.google.com/search?q={0}"),
        new("Bing", "https://www.bing.com/", "https://www.bing.com/search?q={0}"),
        new("Brave", "https://search.brave.com/", "https://search.brave.com/search?q={0}")
    ];

    public SearchEngine(string name, string homeUrl, string searchUrl)
    {
        Name = name;
        HomeUrl = homeUrl;
        SearchUrl = searchUrl;
    }

    public string Name { get; }
    public string HomeUrl { get; }
    public string SearchUrl { get; }

    public string BuildSearchUrl(string query) =>
        string.Format(SearchUrl, Uri.EscapeDataString(query));
}

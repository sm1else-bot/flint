namespace Flint;

public static class AdBlocker
{
    private const string HostsUrl = "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts";

    private static readonly string BlocklistPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Flint", "blocklist.txt");

    private static HashSet<string> blockedDomains = new(StringComparer.OrdinalIgnoreCase);

    public static bool Enabled { get; set; } = true;

    public static async Task InitializeAsync()
    {
        try
        {
            bool needsDownload = !File.Exists(BlocklistPath)
                || (DateTime.UtcNow - File.GetLastWriteTimeUtc(BlocklistPath)).TotalDays > 7;

            if (needsDownload)
                await DownloadAsync();

            if (File.Exists(BlocklistPath))
                Parse();
        }
        catch { }
    }

    private static async Task DownloadAsync()
    {
        using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        string text = await http.GetStringAsync(HostsUrl);
        await File.WriteAllTextAsync(BlocklistPath, text);
    }

    private static void Parse()
    {
        var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string line in File.ReadLines(BlocklistPath))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || parts[0] != "0.0.0.0") continue;
            string domain = parts[1];
            if (domain.StartsWith('#') || domain == "0.0.0.0" || !domain.Contains('.'))
                continue;
            domains.Add(domain);
        }
        blockedDomains = domains;
    }

    public static bool IsBlocked(string url)
    {
        if (!Enabled || blockedDomains.Count == 0) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)) return false;
        return blockedDomains.Contains(uri.Host);
    }
}

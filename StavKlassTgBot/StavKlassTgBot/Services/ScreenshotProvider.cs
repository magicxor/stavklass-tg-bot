using System.Text.Json;
using Flurl;
using Microsoft.Extensions.Options;
using StavKlassTgBot.Models;

namespace StavKlassTgBot.Services;

public class ScreenshotProvider
{
    private readonly IOptions<StavKlassTgBotOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;

    private bool _initialized;
    private readonly List<ScreenshotInfo> _screenshots = [];
    private readonly Random _random = new();

    public ScreenshotProvider(IOptions<StavKlassTgBotOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _options = options;
        _httpClientFactory = httpClientFactory;
    }

    private async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return;

        var client = _httpClientFactory.CreateClient();
        var url = _options.Value.FileHostingUrl;
        var screenshotCatalogUri = new Uri(url, UriKind.Absolute).AppendPathSegment("ScreenshotCatalog.json");
        var json = await client.GetStringAsync(screenshotCatalogUri, cancellationToken);
        var screenshotCatalog = JsonSerializer.Deserialize<List<ScreenshotInfo>>(json);
        if (screenshotCatalog is not null)
            _screenshots.AddRange(screenshotCatalog);

        _initialized = true;
    }

    public async Task<ScreenshotInfo?> GetScreenshotAsync(string file, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var screenshot = _screenshots.FirstOrDefault(s => s.File == file);
        return screenshot ?? null;
    }

    public async Task<List<ScreenshotInfo>> FindScreenshotsAsync(string substring, int limit = 10, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        var screenshots = string.IsNullOrWhiteSpace(substring)
            ? _screenshots.OrderBy(s => _random.Next(int.MinValue, int.MaxValue)).AsQueryable()
            : _screenshots.AsQueryable();

        screenshots = screenshots
            .Where(s => s.Text != null && s.Text.Contains(substring))
            .Take(limit);

        return screenshots.ToList();
    }
}

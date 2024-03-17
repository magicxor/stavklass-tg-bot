using System.Collections.ObjectModel;
using System.Text.Json;
using Flurl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StavKlassTgBot.Enums;
using StavKlassTgBot.Models;

namespace StavKlassTgBot.Services;

public sealed class ScreenshotProvider : IDisposable
{
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private readonly Random _random = new();

    private readonly IOptions<StavKlassTgBotOptions> _options;
    private readonly ILogger<ScreenshotProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITimer _timer;

    private bool _initialized;
    private IReadOnlyCollection<ScreenshotInfo> _screenshots = [];

    public ScreenshotProvider(IOptions<StavKlassTgBotOptions> options,
        ILogger<ScreenshotProvider> logger,
        IHttpClientFactory httpClientFactory,
        TimeProvider timeProvider)
    {
        _options = options;
        _logger = logger;
        _httpClientFactory = httpClientFactory;

        _timer = timeProvider.CreateTimer(PeriodicUpdate, null, TimeSpan.FromHours(3), TimeSpan.FromHours(3));
    }

    private async Task InitializeAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        await _semaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            if (_initialized && !force)
                return;

            var client = _httpClientFactory.CreateClient(nameof(HttpClientTypes.ExternalContent));
            var url = _options.Value.FileHostingUrl;
            var screenshotCatalogUri = new Uri(url, UriKind.Absolute).AppendPathSegment("ScreenshotCatalog.json");
            var json = await client.GetStringAsync(screenshotCatalogUri, cancellationToken);
            var screenshotCatalog = JsonSerializer.Deserialize<List<ScreenshotInfo>>(json);

            _logger.LogInformation("Screenshot catalog updated. Count: {Count}", screenshotCatalog?.Count ?? 0);

            _screenshots = screenshotCatalog?.AsReadOnly() ?? ReadOnlyCollection<ScreenshotInfo>.Empty;

            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update screenshot catalog");
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private void PeriodicUpdate(object? state)
    {
        _logger.LogInformation("Periodic update started");
        _ = InitializeAsync(force: true);
    }

    public async Task<IReadOnlyCollection<ScreenshotInfo>> FindScreenshotsAsync(string substring, int limit = 10, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken: cancellationToken);

        var screenshots = string.IsNullOrWhiteSpace(substring)
            ? _screenshots.OrderBy(_ => _random.Next(int.MinValue, int.MaxValue)).AsQueryable()
            : _screenshots.AsQueryable();

        screenshots = screenshots
            .Where(s => s.Text != null && s.Text.Contains(substring, StringComparison.InvariantCultureIgnoreCase))
            .Take(limit);

        return screenshots.ToList().AsReadOnly();
    }

    public void Dispose()
    {
        _semaphoreSlim.Dispose();
        _timer.Dispose();
    }
}

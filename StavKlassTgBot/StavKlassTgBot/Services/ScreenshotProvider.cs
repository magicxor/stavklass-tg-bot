﻿using System.Text.Json;
using Flurl;
using Microsoft.Extensions.Options;
using StavKlassTgBot.Enums;
using StavKlassTgBot.Models;

namespace StavKlassTgBot.Services;

public sealed class ScreenshotProvider : IDisposable
{
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    private readonly IOptions<StavKlassTgBotOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly List<ScreenshotInfo> _screenshots = [];
    private readonly Random _random = new();
    private bool _initialized;

    public ScreenshotProvider(IOptions<StavKlassTgBotOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _options = options;
        _httpClientFactory = httpClientFactory;
    }

    private async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _semaphoreSlim.WaitAsync(cancellationToken);

        try
        {
            if (_initialized)
                return;

            var client = _httpClientFactory.CreateClient(nameof(HttpClientTypes.ExternalContent));
            var url = _options.Value.FileHostingUrl;
            var screenshotCatalogUri = new Uri(url, UriKind.Absolute).AppendPathSegment("ScreenshotCatalog.json");
            var json = await client.GetStringAsync(screenshotCatalogUri, cancellationToken);
            var screenshotCatalog = JsonSerializer.Deserialize<List<ScreenshotInfo>>(json);
            if (screenshotCatalog is not null)
                _screenshots.AddRange(screenshotCatalog);

            _initialized = true;
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task<List<ScreenshotInfo>> FindScreenshotsAsync(string substring, int limit = 10, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        var screenshots = string.IsNullOrWhiteSpace(substring)
            ? _screenshots.OrderBy(_ => _random.Next(int.MinValue, int.MaxValue)).AsQueryable()
            : _screenshots.AsQueryable();

        screenshots = screenshots
            .Where(s => s.Text != null && s.Text.Contains(substring, StringComparison.InvariantCultureIgnoreCase))
            .Take(limit);

        return screenshots.ToList();
    }

    public void Dispose()
    {
        _semaphoreSlim.Dispose();
    }
}

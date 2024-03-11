using System.Text.Json.Serialization;

namespace StavKlassTgBot.Models;

public sealed class ScreenshotInfo
{
    [JsonPropertyName("file")]
    public string? File { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("width")]
    public int? Width { get; init; }

    [JsonPropertyName("height")]
    public int? Height { get; init; }
}

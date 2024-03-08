using System.ComponentModel.DataAnnotations;

namespace StavKlassTgBot.Models;

public class StavKlassTgBotOptions
{
    [Required]
    [RegularExpression(@".*:.*")]
    public required string TelegramBotApiKey { get; init; }

    [Required]
    [Url]
    public required string FileHostingUrl { get; init; }
}

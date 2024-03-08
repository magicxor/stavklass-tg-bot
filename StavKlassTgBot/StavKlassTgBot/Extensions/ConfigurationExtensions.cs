using Microsoft.Extensions.Configuration;
using StavKlassTgBot.Enums;
using StavKlassTgBot.Models;

namespace StavKlassTgBot.Extensions;

public static class ConfigurationExtensions
{
    public static string? GetTelegramBotApiKey(this IConfiguration configuration)
    {
        return configuration.GetSection(nameof(OptionSections.StavKlassTgBot)).GetValue<string>(nameof(StavKlassTgBotOptions.TelegramBotApiKey));
    }
}

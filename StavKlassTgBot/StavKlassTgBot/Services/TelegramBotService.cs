using Flurl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StavKlassTgBot.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

namespace StavKlassTgBot.Services;

public sealed class TelegramBotService
{
    private static readonly ReceiverOptions ReceiverOptions = new()
    {
        AllowedUpdates = [UpdateType.InlineQuery],
    };

    private readonly ILogger<TelegramBotService> _logger;
    private readonly IOptions<StavKlassTgBotOptions> _options;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly ScreenshotProvider _screenshotProvider;

    public TelegramBotService(ILogger<TelegramBotService> logger,
        IOptions<StavKlassTgBotOptions> options,
        ITelegramBotClient telegramBotClient,
        ScreenshotProvider screenshotProvider)
    {
        _logger = logger;
        _options = options;
        _telegramBotClient = telegramBotClient;
        _screenshotProvider = screenshotProvider;
    }

    private Task HandleUpdateAsync(ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received update with type={Update}", update.Type.ToString());

        // ReSharper disable once AsyncVoidLambda
        ThreadPool.QueueUserWorkItem(async _ => await HandleUpdateFunctionAsync(botClient, update, cancellationToken));

        return Task.CompletedTask;
    }

    private async Task HandleUpdateFunctionAsync(ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        try
        {
            if (update.InlineQuery is { } inlineQuery)
            {
                _logger.LogInformation("Inline query received. Query (length: {QueryLength}): {Query}",
                    inlineQuery.Query.Length,
                    inlineQuery.Query);

                var url = _options.Value.FileHostingUrl;
                var photosUri = new Uri(url, UriKind.Absolute).AppendPathSegment("photos");
                var screenshots = await _screenshotProvider.FindScreenshotsAsync(inlineQuery.Query, 10, cancellationToken);
                var inlineResults = screenshots
                    .ConvertAll(screenshotInfo => new InlineQueryResultPhoto(
                        screenshotInfo.File ?? Guid.NewGuid().ToString(),
                        $"{photosUri}/{screenshotInfo.File}",
                        $"{photosUri}/{screenshotInfo.File}")
                    {
                        PhotoHeight = screenshotInfo.Height,
                        PhotoWidth = screenshotInfo.Width,
                    })
;

                await botClient.AnswerInlineQueryAsync(inlineQuery.Id, inlineResults, 604800, false, cancellationToken: cancellationToken);
                _logger.LogInformation("Inline query answered. Sent {Count} results: {Results}",
                    inlineResults.Count,
                    string.Join(", ", inlineResults.Select(r => r.PhotoUrl)));
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while handling update");
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ApiRequestException apiRequestException)
        {
            _logger.LogError(exception,
                @"Telegram API Error. ErrorCode={ErrorCode}, RetryAfter={RetryAfter}, MigrateToChatId={MigrateToChatId}",
                apiRequestException.ErrorCode,
                apiRequestException.Parameters?.RetryAfter,
                apiRequestException.Parameters?.MigrateToChatId);
        }
        else
        {
            _logger.LogError(exception, @"Telegram API Error");
        }

        return Task.CompletedTask;
    }

    public void Start(CancellationToken cancellationToken)
    {
        _telegramBotClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: ReceiverOptions,
            cancellationToken: cancellationToken
        );
    }
}

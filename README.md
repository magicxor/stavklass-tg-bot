# stavklass-tg-bot
inline Telegram bot for posting stavklass.ru pictures (screenshots from [ok.ru](https://ok.ru/))

[Demo](https://github.com/magicxor/stavklass-tg-bot/assets/8275793/f62f8af4-c51a-4218-ba05-b40dcea94593)

### Docker

```powershell
docker build . --progress=plain --file=StavKlassTgBot/Dockerfile -t stav-klass-tg-bot:latest
```

Environment variables: `STAVKLASS_StavKlassTgBot__FileHostingUrl`, `STAVKLASS_StavKlassTgBot__TelegramBotApiKey`

## Back-end

See https://github.com/magicxor/stavklass

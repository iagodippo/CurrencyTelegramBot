using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CurrencyTelegramBot;

public class CurrencyBotWorker : BackgroundService
{
    private readonly UserConfigRepository _userConfigRepository;
    private readonly ITelegramBotClient _bot;

    public CurrencyBotWorker(ITelegramBotClient bot,
        IServiceScopeFactory serviceProvider)
    {
        _bot = bot;
        var scope = serviceProvider.CreateScope();
        _userConfigRepository = scope.ServiceProvider.GetRequiredService<UserConfigRepository>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            cancellationToken: stoppingToken);

        await Task.Delay(-1, stoppingToken);
    }


    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
        {
            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text!.Trim();
            var config = await GetOrCreateConfig(chatId, update.Message.Chat.Username);

            switch (config.State)
            {
                case BotState.EsperaIntervalo:
                {
                    if (int.TryParse(text, out int minutos))
                    {
                        config.IntervaloMinutos = minutos;
                        config.State = BotState.Normal;
                        await bot.SendMessage(chatId,
                            $"Intervalo configurado para {minutos} minuto{(minutos > 1 ? "s" : "")}.",
                            cancellationToken: ct);
                    }
                    else
                    {
                        await bot.SendMessage(chatId, "Por favor, digite um número válido em minutos.",
                            cancellationToken: ct);
                    }

                    break;
                }
                case BotState.EsperaMoedas:
                {
                    var pares = text.ToUpper()
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                        .Select(p =>
                        {
                            var partes = p.Split("-", StringSplitOptions.TrimEntries);
                            return partes.Length == 2 ? (From: partes[0].Trim(), To: partes[1].Trim()) : default;
                        })
                        .Where(p => !string.IsNullOrWhiteSpace(p.From) && !string.IsNullOrWhiteSpace(p.To))
                        .ToList();

                    if (pares.Count == 0)
                    {
                        await bot.SendMessage(chatId, "Formato inválido. Use por exemplo: EUR-BRL,EUR-USD",
                            cancellationToken: ct);
                    }
                    else
                    {
                        foreach (var par in pares)
                        {
                            config.Coins.Add(new UserCoins(config.ChatId, par.From, par.To));
                        }

                        config.State = BotState.Normal;
                        var resumo = string.Join("\n", config.Coins.Select(p => $"{p.From} → {p.To}"));
                        await bot.SendMessage(chatId, $"Moedas configuradas:\n{resumo}", cancellationToken: ct);
                    }

                    break;
                }
            }

            if (text.Equals("/start", StringComparison.InvariantCultureIgnoreCase))
            {
                await bot.SendMessage(chatId, "Bem-vindo ao Bot de Câmbio!",
                    cancellationToken: ct);
                await bot.SendMessage(chatId,
                    "Siga as instruções e configure o intervalo de tempo das notificações e as moedas desejadas",
                    replyMarkup: MainMenu(), cancellationToken: ct);
            }

            await _userConfigRepository.UpsertAsync(config);
        }
        else if (update is { Type: UpdateType.CallbackQuery, CallbackQuery: not null })
        {
            var cb = update.CallbackQuery;
            long chatId = cb.Message.Chat.Id;
            var config = await GetOrCreateConfig(chatId, cb.Message.Chat.Username);

            switch (cb.Data)
            {
                case "config_intervalo":
                    config.State = BotState.EsperaIntervalo;
                    await bot.SendMessage(chatId, "Digite o intervalo de tempo em minutos:", cancellationToken: ct);
                    break;
                case "config_moedas":
                    config.State = BotState.EsperaMoedas;
                    await bot.SendMessage(chatId, "Digite as moedas desejadas no formato BRL-USD:",
                        cancellationToken: ct);
                    break;
                case "ver_status":
                    var status =
                        $"⏱ Intervalo: {config.IntervaloMinutos} min\n💱 Moedas: {string.Join("", config.Coins.Select(p => $"\n{p.From}→{p.To}"))}";
                    await bot.SendMessage(chatId, status, cancellationToken: ct);
                    break;
            }

            await bot.AnswerCallbackQuery(cb.Id, cancellationToken: ct);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    private async Task<User> GetOrCreateConfig(long chatId, string username = "")
    {
        return await _userConfigRepository.GetOrAddAsync(new User { ChatId = chatId, Username = username });
    }

    private static InlineKeyboardMarkup MainMenu() => new([
        [
            InlineKeyboardButton.WithCallbackData("⏱ Intervalo", "config_intervalo"),
            InlineKeyboardButton.WithCallbackData("💱 Moedas", "config_moedas")
        ],
        [InlineKeyboardButton.WithCallbackData("📊 Ver status", "ver_status")]
    ]);
}
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CurrencyTelegramBot;

public class CurrencyBotWorker(
    ITelegramBotClient _bot,
    ILogger<CurrencyBotWorker> _logger,
    IHttpClientFactory _httpClientFactory)
    : BackgroundService
{
    private readonly ConcurrentDictionary<long, UserConfig> _configs = new();
    private readonly HttpClient _client = _httpClientFactory.CreateClient("AwesomeApi");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Bot started.");

        // Tarefa recorrente para enviar cotações
        _ = Task.Run(() => EnviarCotacoesPeriodicamente(stoppingToken), stoppingToken);

        await Task.Delay(-1, stoppingToken);
    }

    private async Task EnviarCotacoesPeriodicamente(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            foreach (var config in _configs.Values)
            {
                if (config.IntervaloMinutos <= 0 || config.Moedas.Count == 0)
                    continue;

                var ultimaExecucao = config.UltimoEnvio;
                if (ultimaExecucao.AddMinutes(config.IntervaloMinutos) <= DateTime.UtcNow)
                {
                    config.UltimoEnvio = DateTime.UtcNow;
                    var msg = await ObterCotacoesAsync(config.Moedas);
                    await _bot.SendMessage(config.ChatId, msg, cancellationToken: ct);
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }

    private async Task<string> ObterCotacoesAsync(List<string> moedas)
    {
        try
        {
            var symbols = string.Join(",", moedas.Select(m => m + "-BRL"));
            var result = await _client.GetFromJsonAsync<Dictionary<string, Cotacao>>(symbols);

            return result == null ? "Não foi possível obter as cotações no momento." : string.Join("\n", result.Values.Select(v => $"💱 {v.code} → R$ {v.ask}"));
        }
        catch
        {
            return "Erro ao buscar as cotações.";
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
        {
            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text!.Trim();
            var config = GetOrCreateConfig(chatId);

            switch (config.State)
            {
                case BotState.EsperaIntervalo:
                {
                    if (int.TryParse(text, out int minutos))
                    {
                        config.IntervaloMinutos = minutos;
                        config.State = BotState.Normal;
                        await bot.SendMessage(chatId, $"Intervalo configurado para {minutos} minutos.", cancellationToken: ct);
                    }
                    else
                    {
                        await bot.SendMessage(chatId, "Por favor, digite um número válido em minutos.", cancellationToken: ct);
                    }
                    return;
                }
                case BotState.EsperaMoedas:
                    config.Moedas = text.ToUpper().Split(',', StringSplitOptions.TrimEntries).ToList();
                    config.State = BotState.Normal;
                    await bot.SendMessage(chatId, $"Moedas configuradas: {string.Join(", ", config.Moedas)}", cancellationToken: ct);
                    return;
            }

            if (text.Equals("/start", StringComparison.InvariantCultureIgnoreCase))
            {
                await bot.SendMessage(chatId, "Bem-vindo ao Bot de Câmbio!", replyMarkup: MainMenu(), cancellationToken: ct);
                await bot.SendMessage(chatId, "Siga as instruções e configure o intervalo de tempo das notificações e as moedas desejadas", replyMarkup: MainMenu(), cancellationToken: ct);
            }
        }
        else if (update is { Type: UpdateType.CallbackQuery, CallbackQuery: not null })
        {
            var cb = update.CallbackQuery;
            long chatId = cb.Message.Chat.Id;
            var config = GetOrCreateConfig(chatId);

            switch (cb.Data)
            {
                case "config_intervalo":
                    config.State = BotState.EsperaIntervalo;
                    await bot.SendMessage(chatId, "Digite o intervalo de tempo em minutos:", cancellationToken: ct);
                    break;
                case "config_moedas":
                    config.State = BotState.EsperaMoedas;
                    await bot.SendMessage(chatId, "Digite as moedas desejadas (ex: USD,EUR):", cancellationToken: ct);
                    break;
                case "ver_status":
                    var status = $"⏱ Intervalo: {config.IntervaloMinutos} min\n💱 Moedas: {string.Join(", ", config.Moedas)}";
                    await bot.SendMessage(chatId, status, cancellationToken: ct);
                    break;
            }

            await bot.AnswerCallbackQuery(cb.Id, cancellationToken: ct);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Erro no bot");
        return Task.CompletedTask;
    }

    private UserConfig GetOrCreateConfig(long chatId)
    {
        return _configs.GetOrAdd(chatId, id => new UserConfig { ChatId = id });
    }

    private static InlineKeyboardMarkup MainMenu() => new([
        [
            InlineKeyboardButton.WithCallbackData("⏱ Intervalo", "config_intervalo"),
            InlineKeyboardButton.WithCallbackData("💱 Moedas", "config_moedas")
        ],
        [InlineKeyboardButton.WithCallbackData("📊 Ver status", "ver_status")]
    ]);
}


public enum BotState
{
    Normal,
    EsperaIntervalo,
    EsperaMoedas
}

public class UserConfig
{
    public long ChatId { get; init; }
    public BotState State { get; set; } = BotState.Normal;
    public int IntervaloMinutos { get; set; }
    public List<string> Moedas { get; set; } = [];
    public DateTime UltimoEnvio { get; set; } = DateTime.MinValue;
}

public class Cotacao
{
    public string code { get; set; } = "";
    public string bid { get; set; } = "";
    public string ask { get; set; } = "";
    
}
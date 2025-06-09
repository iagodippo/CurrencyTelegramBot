using Telegram.Bot;

namespace CurrencyTelegramBot;

public class NotifyCurrencyWorker : BackgroundService
{
    private UserConfigRepository _userConfigRepository;
    private readonly HttpClient _client;
    private ITelegramBotClient _bot;
    
    public NotifyCurrencyWorker(ITelegramBotClient bot,
        IServiceScopeFactory serviceProvider,
        IHttpClientFactory httpClientFactory)
    {
        _bot = bot;
        _client = httpClientFactory.CreateClient("AwesomeApi");
        var scope = serviceProvider.CreateScope();
        _userConfigRepository = scope.ServiceProvider.GetRequiredService<UserConfigRepository>();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = Task.Run(() => EnviarCotacoesPeriodicamente(stoppingToken), stoppingToken);

        await Task.Delay(-1, stoppingToken);
    }
    
    private async Task EnviarCotacoesPeriodicamente(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var configs = await _userConfigRepository.GetAllAsync();
            foreach (var config in configs)
            {
                if (config.IntervaloMinutos <= 0 || config.Coins.Count == 0)
                    continue;

                var ultimaExecucao = config.UltimoEnvio;
                if (ultimaExecucao.AddMinutes(config.IntervaloMinutos) > DateTime.UtcNow)
                    continue;

                config.UltimoEnvio = DateTime.UtcNow;

                var msg = await ObterCotacoesAsync(config.Coins);
                await _bot.SendMessage(config.ChatId, msg, cancellationToken: ct);

                await _userConfigRepository.UpsertAsync(config);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }

    
    private async Task<string> ObterCotacoesAsync(List<UserCoins> coins)
    {
        try
        {
            var symbols = string.Join(",", coins.Select(p => $"{p.From}-{p.To}"));
            var result = await _client.GetFromJsonAsync<Dictionary<string, Cotacao>>(symbols);

            return result == null ? "Não foi possível obter as cotações." : string.Join("\n", result.Values.Select(v => $"💱 {v.code} → {v.codein} {v.ask}"));
        }
        catch
        {
            return "Erro ao buscar as cotações.";
        }
    }
}
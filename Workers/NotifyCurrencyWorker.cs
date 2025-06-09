using CurrencyTelegramBot.Entity;
using CurrencyTelegramBot.Models;
using CurrencyTelegramBot.Repository;
using Telegram.Bot;

namespace CurrencyTelegramBot.Workers;

public class NotifyCurrencyWorker : BackgroundService
{
    private UserRepository _userRepository;
    private readonly HttpClient _client;
    private ITelegramBotClient _bot;
    
    public NotifyCurrencyWorker(ITelegramBotClient bot,
        IServiceScopeFactory serviceProvider,
        IHttpClientFactory httpClientFactory)
    {
        _bot = bot;
        _client = httpClientFactory.CreateClient("AwesomeApi");
        var scope = serviceProvider.CreateScope();
        _userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();
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
            var users = await _userRepository.GetAllAsync();
            foreach (var user in users.Where(x => x.IsActive))
            {
                if (user.MinutesInterval <= 0 || user.Coins.Count == 0)
                    continue;

                var ultimaExecucao = user.LastNotify;
                if (ultimaExecucao.AddMinutes(user.MinutesInterval) > DateTime.UtcNow)
                    continue;

                user.LastNotify = DateTime.UtcNow;

                var msg = await ObterCotacoesAsync(user.Coins);
                await _bot.SendMessage(user.ChatId, msg, cancellationToken: ct);

                await _userRepository.UpsertAsync(user);
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
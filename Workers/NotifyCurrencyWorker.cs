using CurrencyTelegramBot.Entity;
using CurrencyTelegramBot.Models;
using CurrencyTelegramBot.Repository;
using Telegram.Bot;
using Polly;
using System.Net;

namespace CurrencyTelegramBot.Workers;

public class NotifyCurrencyWorker : BackgroundService
{
    private UserRepository _userRepository;
    private readonly HttpClient _client;
    private ITelegramBotClient _bot;
    
    // Cache para armazenar cotações com timestamp
    private readonly Dictionary<string, (Cotacao cotacao, DateTime timestamp)> _cotacoesCache = new();
    private readonly TimeSpan _cacheExpirationTime = TimeSpan.FromMinutes(5);
    
    // Política de retry com Polly
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    
    public NotifyCurrencyWorker(ITelegramBotClient bot,
        IServiceScopeFactory serviceProvider,
        IHttpClientFactory httpClientFactory)
    {
        _bot = bot;
        _client = httpClientFactory.CreateClient("AwesomeApi");
        var scope = serviceProvider.CreateScope();
        _userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();
        
        // Configuração da política de retry
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(10),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} após {timespan} segundos devido a Too Many Requests");
                });
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
            string symbols = string.Join(",", coins.Select(p => $"{p.From}-{p.To}"));
            
            // Verifica se as cotações estão no cache e não expiraram
            if (_cotacoesCache.TryGetValue(symbols, out var cacheEntry) && 
                DateTime.UtcNow - cacheEntry.timestamp < _cacheExpirationTime)
            {
                Console.WriteLine("Usando cotações do cache.");
                return $"💱 {cacheEntry.cotacao.code} → {cacheEntry.cotacao.codein} {cacheEntry.cotacao.ask}";
            }

            // Chamada à API com política de retry
            var response = await _retryPolicy.ExecuteAsync(() => _client.GetAsync($"cotacao/{symbols}"));
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, Cotacao>>();

            // Armazena no cache
            if (result == null)
                return result == null
                    ? "Não foi possível obter as cotações."
                    : string.Join("\n", result.Values.Select(v => $"💱 {v.code} → {v.codein} {v.ask}"));
            foreach (var kvp in result)
            {
                _cotacoesCache[kvp.Key] = (kvp.Value, DateTime.UtcNow);
            }

            return string.Join("\n", result.Values.Select(v => $"💱 {v.code} → {v.codein} {v.ask}"));
        }
        catch(Exception ex)
        {
            return $"Erro ao buscar as cotações. {ex.Message}";
        }
    }
}
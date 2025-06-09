using CurrencyTelegramBot.Enums;

namespace CurrencyTelegramBot.Entity;

public class User
{
    public string Username { get; set; }
    public long ChatId { get; init; }
    public BotState State { get; set; } = BotState.Normal;
    public int MinutesInterval { get; set; }
    public DateTime LastNotify { get; set; } = DateTime.MinValue;
    public bool IsActive { get; set; } = true;
    public List<UserCoins> Coins { get; set; } = [];
}
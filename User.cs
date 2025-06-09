namespace CurrencyTelegramBot;

public class User
{
    public string Username { get; set; }
    public long ChatId { get; init; }
    public BotState State { get; set; } = BotState.Normal;
    public int IntervaloMinutos { get; set; }
    public DateTime UltimoEnvio { get; set; } = DateTime.MinValue;
    public List<UserCoins> Coins { get; set; } = [];
}
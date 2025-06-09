namespace CurrencyTelegramBot;

public class UserCoins
{
    public UserCoins(long chatId, string from, string to)
    {
        ChatId = chatId;
        From = from;
        To = to;
    }

    public int Id { get; set; }
    public long ChatId { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public User User { get; set; }
}
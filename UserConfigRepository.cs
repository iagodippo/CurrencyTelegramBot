using Microsoft.EntityFrameworkCore;

namespace CurrencyTelegramBot;

public class UserConfigRepository(AppDbContext context)
{
    public async Task<User?> GetByIdAsync(long chatId)
    {
        return await context.User
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ChatId == chatId);
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await context.User
            .Include(u => u.Coins)
            .AsNoTracking()
            .ToListAsync();
    }


    public async Task UpsertAsync(User config)
    {
        var existing = await context.User
            .FirstOrDefaultAsync(u => u.ChatId == config.ChatId);

        if (existing == null)
        {
            await context.User.AddAsync(config);
        }
        else
        {
            existing.Coins = config.Coins;
            existing.State = config.State;
            existing.IntervaloMinutos = config.IntervaloMinutos;
            existing.UltimoEnvio = config.UltimoEnvio;
            context.User.Update(existing);
        }

        await context.SaveChangesAsync();
    }

    public async Task<User> GetOrAddAsync(User user)
    {
        var existing = await context.User
            .Include(u => u.Coins)
            .FirstOrDefaultAsync(u => u.ChatId == user.ChatId);

        if (existing != null)
            return existing;

        await context.User.AddAsync(user);
        await context.SaveChangesAsync();

        return await context.User
            .Include(u => u.Coins)
            .FirstOrDefaultAsync(u => u.ChatId == user.ChatId);
    }
}
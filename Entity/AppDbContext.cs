using Microsoft.EntityFrameworkCore;

namespace CurrencyTelegramBot.Entity;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> User { get; set; }
    public DbSet<UserCoins> UserCoins { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasKey(u => u.ChatId);
        modelBuilder.Entity<UserCoins>()
            .HasKey(uc => uc.Id);
        modelBuilder.Entity<UserCoins>()
            .HasOne(uc => uc.User)
            .WithMany(u => u.Coins)
            .HasForeignKey(uc => uc.ChatId);


        
    }
}
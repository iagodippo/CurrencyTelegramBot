using CurrencyTelegramBot;
using CurrencyTelegramBot.Entity;
using CurrencyTelegramBot.HttpHandlers;
using CurrencyTelegramBot.Options;
using CurrencyTelegramBot.Repository;
using CurrencyTelegramBot.Workers;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); // carrega variáveis do ambiente (ex: Railway, Docker)


builder.Services.AddOptions<AwesomeApiOptions>()
    .Bind(builder.Configuration.GetSection("AwesomeApi"));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<ITelegramBotClient>(
    new TelegramBotClient(builder.Configuration["Telegram:Token"] ?? string.Empty));
builder.Services.AddTransient<AwesomeApiQueryHandler>();
builder.Services.AddScoped<UserRepository>();

builder.Services.AddHostedService<CurrencyBotWorker>();
builder.Services.AddHostedService<NotifyCurrencyWorker>();

builder.Services.Configure<HostOptions>(options =>
{
    options.ServicesStartConcurrently = true;
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
    options.ShutdownTimeout = TimeSpan.FromSeconds(5);
    options.ServicesStopConcurrently = true;
});
builder.Services.AddHttpClient("AwesomeApi",
        client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["AwesomeApi:Url"] ?? string.Empty);
            client.Timeout = TimeSpan.FromMinutes(2);
        })
.AddHttpMessageHandler<AwesomeApiQueryHandler>()
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
});


var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();
app.MapGet("/health", () => Results.Ok("OK!"));
app.Run();
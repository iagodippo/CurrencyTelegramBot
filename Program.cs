using CurrencyTelegramBot;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddOptions<AwesomeApiOptions>()
    .Bind(builder.Configuration.GetSection("AwesomeApi"));

builder.Services.AddSingleton<ITelegramBotClient>(
    new TelegramBotClient(builder.Configuration["Telegram:Token"] ?? string.Empty));
builder.Services.AddTransient<AwesomeApiQueryHandler>();

builder.Services.AddHostedService<CurrencyBotWorker>();
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
app.Run();
using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using PersonalAssistant.Infrastructure.Services;
using PersonalAssistant.Infrastracture.Services;
using PersonalAssistant.Infrastructure.Workers;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(SaveJournalSessionCommand).Assembly));


// Connecting DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IJournalRepository, JournalRepository>();

// Connecting Telegram Bot
builder.Services.AddScoped<IBotNotifService, BotNotifService>();
builder.Services.AddScoped<IBotMediaDownloader, BotMediaDownloader>();
builder.Services.AddSingleton<IAudioProcessQueue, AudioProcessingQueue>();

var botToken = builder.Configuration["TelegramBot:Token"];
builder.Services.AddHttpClient("tgwebhook")
    .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(botToken, httpClient));

// Connecting other services
builder.Services.AddSingleton<IJournalSessionManager, JournalSessionManager>();
builder.Services.AddScoped<IUserStateManager, UserStateManager>();


// Background worker (downloads audio/video)
builder.Services.AddHostedService<AudioProcessWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
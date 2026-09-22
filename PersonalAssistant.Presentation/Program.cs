using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Application.Features.Journal.Commands;
using PersonalAssistant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PersonalAssistant.Presentation.Services;
using Telegram.Bot;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(SaveJournalSessionCommand).Assembly));


// Connecting DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IJournalRepository, MsSqlJournalRepository>();

// Connecting Telegram Bot
builder.Services.AddSingleton<JournalSessionManager>();
var botToken = builder.Configuration["TelegramBot:Token"];
builder.Services.AddHttpClient("tgwebhook")
    .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(botToken, httpClient));



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
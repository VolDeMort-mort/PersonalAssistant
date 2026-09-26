using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using PersonalAssistant.Infrastructure.Services;
using PersonalAssistant.Presentation.Services;
using PersonalAssistant.Infrastructure.Workers;
using PersonalAssistant.Application.Features.Journal.Commands;
using PersonalAssistant.Presentation.Bot.Options;
using PersonalAssistant.Presentation.Bot;
using PersonalAssistant.Presentation.Bot.Handlers;
using PersonalAssistant.Presentation.Bot.Finance;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(SaveJournalSessionCommand).Assembly);
}); 


// Connecting DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IJournalRepository, JournalRepository>();
// Same scoped AppDbContext as the repositories, so one SaveChanges commits all of their changes
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IFinanceTransactionRepository, FinanceTransactionRepository>();
builder.Services.AddScoped<IFinanceCatalogRepository, FinanceCatalogRepository>();
builder.Services.AddScoped<IScheduledPaymentRepository, ScheduledPaymentRepository>();

// Clock in the user's time zone (Kyiv)
builder.Services.AddSingleton<TimeProvider, KyivTimeProvider>();

// Connecting Telegram Bot
builder.Services.AddScoped<IBotNotifService, BotNotifService>();
builder.Services.AddScoped<IBotMessenger, BotMessenger>();
builder.Services.AddScoped<IBotMediaDownloader, BotMediaDownloader>();
builder.Services.AddSingleton<IAudioProcessQueue, AudioProcessingQueue>();

var botToken = builder.Configuration["TelegramBot:Token"];
builder.Services.AddHttpClient("tgwebhook")
    .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(botToken, httpClient));
builder.Services.AddOptions<TelegramOptions>()
    .Bind(builder.Configuration.GetSection(TelegramOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();


// Bot pipeline
builder.Services.AddSingleton<IUpdateQueue, UpdateQueue>();
builder.Services.AddSingleton<IScreenTracker, ScreenTracker>();
builder.Services.AddSingleton<IDialogStore, DialogStore>();
builder.Services.AddScoped<IUpdateRouter, UpdateRouter>();
builder.Services.AddHostedService<UpdateProcessingService>();
builder.Services.AddHostedService<WebhookRegistrationService>();

// Handlers: for messages the first match wins, registration order matters
builder.Services.AddScoped<IMessageHandler, MenuCommandHandler>();
builder.Services.AddScoped<IMessageHandler, JournalMessageHandler>();
builder.Services.AddScoped<IMessageHandler, FinanceInputHandler>();
builder.Services.AddScoped<IMessageHandler, PaymentInputHandler>();
builder.Services.AddScoped<ICallbackHandler, NavigationCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, JournalCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, FinanceCallbackHandler>();
builder.Services.AddScoped<ICallbackHandler, PaymentCallbackHandler>();

// Finance UI: scenarios
builder.Services.AddScoped<FinanceFlow>();
builder.Services.AddScoped<PaymentFlow>();

// Payment reminders: the worker (Infrastructure) runs the use case, the notifier (Presentation) sends to Telegram
builder.Services.AddScoped<IPaymentReminderNotifier, TelegramPaymentReminderNotifier>();
builder.Services.AddHostedService<PaymentReminderWorker>();


// Connecting other services
builder.Services.AddSingleton<IJournalSessionManager, JournalSessionManager>();


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
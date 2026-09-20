using PersonalAssistant.Application.Interfaces;
using PersonalAssistant.Application.Features.Journal.Commands;
using PersonalAssistant.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(SaveJournalEntryCommand).Assembly));

builder.Services.AddScoped<IJournalRepository, MockJournalRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
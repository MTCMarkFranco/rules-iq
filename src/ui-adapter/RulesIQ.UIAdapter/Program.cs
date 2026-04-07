using RulesIQ.Infrastructure.Extensions;
using RulesIQ.RuntimeEngine.Services;
using RulesIQ.UIAdapter.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddRulesIQInfrastructure(builder.Configuration);
builder.Services.AddScoped<IRuleEvaluationService, RuleEvaluationService>();
builder.Services.AddScoped<IAgentEvaluationService, AgentEvaluationService>();
builder.Services.AddScoped<IRuleRetrievalService, RuleRetrievalService>();
builder.Services.AddScoped<ITraceabilityService, TraceabilityService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

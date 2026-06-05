using Microsoft.AspNetCore.DataProtection;
using XunyaoAdventure.Battle;
using XunyaoAdventure.Components;
using XunyaoAdventure.Game;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys")))
    .SetApplicationName("XunyaoAdventure");
builder.Services.AddSingleton<GameDatabase>();
builder.Services.AddSingleton<GameConfigStore>();
builder.Services.AddSingleton<GameAccountStore>();
builder.Services.AddSingleton<OperationLogStore>();
builder.Services.AddSingleton<FriendStore>();
builder.Services.AddSingleton<ChatStore>();
builder.Services.AddSingleton<QuestStore>();
builder.Services.AddSingleton<CampaignStore>();
builder.Services.AddSingleton<GuildStore>();
builder.Services.AddSingleton<MailStore>();
builder.Services.AddSingleton<ArenaStore>();
builder.Services.AddScoped<GameSession>();

var app = builder.Build();
BattleDemoSetupFactory.Initialize(app.Services.GetRequiredService<GameConfigStore>());

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<XunyaoAdventure.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();



{
    int a = 1;
    {
        
        a += 2;
    }
}

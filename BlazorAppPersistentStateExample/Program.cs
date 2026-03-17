using BlazorAppPersistentStateExample.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Hybrid;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var hybridCacheBuilder = builder.Services.AddHybridCache(opts =>
{
    opts.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(5),
    };
});

string redisConnectionString = "CONNECTION_STRING_GOES_HERE";
IConnectionMultiplexer redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);

builder.Services.AddSingleton(redisConnection);
builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(redisConnection, $"DataProtection-Keys-{nameof(BlazorAppPersistentStateExample)}")
    .SetApplicationName(nameof(BlazorAppPersistentStateExample));
builder.Services.AddStackExchangeRedisCache(x =>
{
    x.InstanceName = nameof(BlazorAppPersistentStateExample);
    x.ConnectionMultiplexerFactory = () => Task.FromResult(redisConnection);
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var signalRBuilder = builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromMinutes(5);
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
});

signalRBuilder.AddStackExchangeRedis(
    redisConnectionString,
    redisOptions =>
    {
        redisOptions.Configuration.ChannelPrefix = RedisChannel.Literal(nameof(BlazorAppPersistentStateExample));
    }
);

var app = builder.Build();

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
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

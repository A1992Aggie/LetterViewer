using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using FamilyPhotos;
using FamilyPhotos.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Suppress expected info-level auth failures that occur before MSAL redirect completes
builder.Logging.AddFilter("Microsoft.AspNetCore.Authorization", LogLevel.Warning);

// Configure MSAL authentication
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add("https://graph.microsoft.com/Files.ReadWrite");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("https://graph.microsoft.com/User.Read");
    options.ProviderOptions.LoginMode = "redirect";
    options.ProviderOptions.Cache.CacheLocation = "localStorage";
});

// Configure authenticated HttpClient for Graph API
builder.Services.AddScoped(sp =>
{
    var authHandler = sp.GetRequiredService<Microsoft.AspNetCore.Components.WebAssembly.Authentication.AuthorizationMessageHandler>()
        .ConfigureHandler(
            authorizedUrls: ["https://graph.microsoft.com"]);

    authHandler.InnerHandler = new HttpClientHandler();
    return new HttpClient(authHandler)
    {
        BaseAddress = new Uri("https://graph.microsoft.com/v1.0/")
    };
});

// Register services
builder.Services.AddSingleton<ErrorService>();
builder.Services.AddScoped<OneDriveService>();
builder.Services.AddScoped<MetadataService>();
builder.Services.AddScoped<AudioService>();
builder.Services.AddScoped<DateParsingService>();
builder.Services.AddScoped<SearchService>();

await builder.Build().RunAsync();

using Contoso.WebApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// Configure Microsoft Identity Web authentication + token acquisition
var initialScopes = builder.Configuration
    .GetSection("DownstreamApis:MicrosoftGraph:Scopes")
    .Get<IEnumerable<string>>();

builder.Services
    .AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAd")
    .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
    .AddInMemoryTokenCaches();

builder.Services.AddDownstreamApis(builder.Configuration.GetSection("DownstreamApis"));

builder.Services.AddAuthorization();

// Add services to the container (Razor Pages + Microsoft Identity UI) and set default route
builder.Services.AddRazorPages().AddMvcOptions(options =>
{
    // Require authenticated users by default (adjust with [AllowAnonymous] on specific pages if needed)
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
}).AddMicrosoftIdentityUI()
  .AddRazorPagesOptions(options =>
  {
      options.Conventions.AddPageRoute("/Home/Home", "/");
  });
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<AuthHandler>();
builder.Services.AddTransient<LoggingHandler>();

builder.Services.AddHttpClient<IContosoAPI>(client => {
    client.BaseAddress = new Uri(builder.Configuration["BackendUrl"]);
})
// .AddHttpMessageHandler(() => new LoggingHandler())
.AddHttpMessageHandler<AuthHandler>()
.AddTypedClient(client => RestService.For<IContosoAPI>(client));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // Enable PII logging for IdentityModel in development to aid troubleshooting
    Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();

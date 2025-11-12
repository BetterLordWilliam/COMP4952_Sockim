using COMP4952_Sockim.Components;
using COMP4952_Sockim.Hubs;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net;
using COMP4952_Sockim.Data;
using Microsoft.Extensions.DependencyInjection;
using COMP4952_Sockim.Components.Account;
using COMP4952_Sockim.Services;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
// var connectionString = builder.Configuration.GetConnectionString("ChatDbContext") ?? throw new InvalidOperationException("Connection string 'ChatDbContextConnection' not found.");
var connectionString = builder.Configuration.GetConnectionString("ChatDbContextMySql") ?? throw new InvalidOperationException("Connection string was really bad");

// Add logging configuration - disable EF Core verbose logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole()
    .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// builder.Services.AddDbContext<ChatDbContext>(options =>
//     options.UseSqlServer(connectionString));

builder.Services.AddDbContext<ChatDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
})
.AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IdentityUserAccessor>();

builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddIdentityCore<ChatUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ChatDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ChatUser>, IdentityNoOpEmailSender>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.SuppressXFrameOptionsHeader = true;
});

builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ChatUserService>();
builder.Services.AddScoped<MessagesService>();
builder.Services.AddScoped<InvitationsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseAntiforgery();

app.UseStaticFiles();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(options =>
    {
        options.ContentSecurityFrameAncestorsPolicy = "*";
    });

app.MapHub<TestChatHub>("chathubtest");
app.MapHub<ChatHub>("chathub");
app.MapHub<MessageHub>("messagehub");
app.MapHub<InvitationHub>("invitationhub");

app.MapAdditionalIdentityEndpoints();;

app.Run();

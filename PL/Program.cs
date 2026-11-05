using BLL.Engines.Interfaces;
using BLL.Repositories.Interfaces;
using BLL.Services;
using BLL.Services.Interfaces;
using DAL;
using DAL.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using PL.App;
using PL.App.Migrations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthenticationServiceInterface, AuthenticationService>();
builder.Services.AddScoped<IChannelServiceInterface, ChannelService>();
builder.Services.AddScoped<IUserServiceInterface, UserService>();
builder.Services.AddScoped<IChannelRepositoryInterface, ChannelRepository>();
builder.Services.AddScoped<IUserRepositoryInterface, UserRepository>();
builder.Services.AddScoped<ISqlConnectionInterface, SqlConnectionEngine>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();
builder.Services.AddAuthorization();

// fix me at some point
builder.Services.AddTransient<PL.App.Migrations.MigrationCommand>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// This can probably be improved; but running a full MVVM page is overkill for a simple search endpoint
// Then, implementing an API layer is a bit out of the scope of this project, so this is a compromise for now
app.MapGet("/api/users/search", async (string query, ClaimsPrincipal user, IUserServiceInterface userService) =>
{
    if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(await userService.SearchUsersAsync(query, userId));
})
    .RequireAuthorization();

if (await CommandHandler.HandleAsync(app, args) is int exitCode)
{
    Environment.Exit(exitCode);
}

app.Run();

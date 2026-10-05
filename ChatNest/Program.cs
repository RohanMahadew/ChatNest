using ChatNest.Models;
using ChatNest.Repositories;
using ChatNest.Repositories.Interface;
using ChatNest.Service;
using ChatNest.Service.Interface;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// So far every interface has 1 associated service class. I don't have to write keyed-scoped
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents();
builder.Services.AddScoped<ISqlConnectionInterface, SqlConnectionService>();
builder.Services.AddScoped<IAuthenticationInterface, AuthService>();
builder.Services.AddScoped<IAuthTokenInterface, AuthTokenService>();
builder.Services.AddScoped<IUserRepositoryInterface, UserRepository>();

builder.Services.AddHttpContextAccessor();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
    });

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

app.Run();

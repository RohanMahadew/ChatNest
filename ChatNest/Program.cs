using ChatNest.Models;
using Microsoft.Extensions.DependencyInjection;
using ChatNest.Service;
using ChatNest.Service.Interface;
using ChatNest.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// So far every interface has 1 associated service class. I don't have to write keyed-scoped
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents();
builder.Services.AddScoped<AuthTokenService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ISqlConnectionInterface, SqlConnectionService>();
builder.Services.AddScoped<IAuthenticationInterface, AuthService>();

builder.Services.AddHttpContextAccessor();

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

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

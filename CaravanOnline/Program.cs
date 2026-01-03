using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CaravanOnline.Services;
using CaravanOnline.Hubs;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

builder.Services.AddScoped<CardManager>();
builder.Services.AddScoped<LaneManager>();
builder.Services.AddScoped<GameStateHelper>();
builder.Services.AddScoped<PlayerManager>();
builder.Services.AddScoped<PhaseManager>();

builder.Services.AddSingleton<OnlineGameStateService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSignalR();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseSession();

app.MapRazorPages();
app.MapHub<GameHub>("/gameHub");

app.Run();

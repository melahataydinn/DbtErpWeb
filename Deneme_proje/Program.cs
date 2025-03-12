using Deneme_proje.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;

var builder = WebApplication.CreateBuilder(args);

// MVC servisini ekleyin
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthFilter());
});

// Authentication servisleri - Oturum sürekli açık kalacak şekilde ayarlandı
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.LogoutPath = "/Login/Logout";
        options.Cookie.Name = "ERPAuth";
        options.Cookie.HttpOnly = true;
        // Oturum süresini 30 güne çıkarıyoruz
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        // Kalıcı oturum için
        options.Cookie.MaxAge = TimeSpan.FromDays(30);
    });

builder.Services.AddHttpContextAccessor();

// Logging ekleyin
builder.Services.AddLogging(configure =>
{
    configure.AddConsole();
    configure.AddDebug();
    configure.SetMinimumLevel(LogLevel.Information);
});

// Session servisi - Daha uzun süre için ayarlandı
builder.Services.AddSession(options =>
{
    // Session süresini artırıyoruz
    options.IdleTimeout = TimeSpan.FromDays(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
});

// Veritabanı ve Repository servisleri
builder.Services.AddScoped<DatabaseSelectorService>();
builder.Services.AddScoped<CariRepository>();
builder.Services.AddScoped<FaturaRepository>();
builder.Services.AddScoped<DenizlerRepository>();
builder.Services.AddScoped<SirketDurumuRepository>();
builder.Services.AddScoped<GunayRepository>();
builder.Services.AddScoped<DiokiRepository>();
builder.Services.AddScoped<EmailNotificationService>();

// Singleton Configuration
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Middleware sıralaması önemli
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Login}/{action=Index}/{id?}");
});

app.Run();
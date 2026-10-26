using FixCampus.Web.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);

// Cookie Authentication (ASP.NET Core Native Authentication)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "FixCampus.Auth";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

// Keep the week-one demo usable without a database secret.
// Database operations require a configured connection; migrations are applied explicitly.
builder.Services.AddDbContext<FixCampusDbContext>(options =>
{
    var connection = builder.Configuration.GetConnectionString("Supabase");
    if (!string.IsNullOrWhiteSpace(connection))
        options.UseNpgsql(connection);
});
builder.Services.AddScoped<DemoListingReader>();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Demo:Seed"))
{
    if (!app.Environment.IsDevelopment())
        throw new InvalidOperationException("Demo kayıtları yalnızca Development ortamında hazırlanabilir.");
    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Supabase")))
        throw new InvalidOperationException("ConnectionStrings:Supabase eksik. Önce User Secrets ayarını yapın.");
    await using var scope = app.Services.CreateAsyncScope();
    await DemoListingSeeder.SeedAsync(
        scope.ServiceProvider.GetRequiredService<FixCampusDbContext>(), builder.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "fixCampus API v1");
        options.DocumentTitle = "fixCampus API";
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.Run();

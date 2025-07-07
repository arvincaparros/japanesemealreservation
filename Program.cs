using Hangfire;
using Hangfire.PostgreSql;
using JapaneseMealReservation.AppData;
using JapaneseMealReservation.DataTransferObject;
using JapaneseMealReservation.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

builder.Services.AddDbContext<SqlServerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection")));

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<MailService>();

//builder.Services.AddHostedService<AutoCompleteOrderService>();

builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("PostgresConnection")));

builder.Services.AddHangfireServer();
// Register your job class
builder.Services.AddScoped<MealReminderJob>();


// Add authentication and set default scheme
builder.Services.AddAuthentication("MyCookieAuth").AddCookie("MyCookieAuth", options =>
{
    options.LoginPath = "/Home/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Session expires in 30 minutes
    options.SlidingExpiration = true; // Refresh cookie on activity

    // Optional: Automatically redirect to login on expiration
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.Redirect("/Home/Login");
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes("MyCookieAuth")
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddSession();

var app = builder.Build();

app.UseHangfireDashboard("/hangfire");

// Every minute, check if it's 30 mins before any meal time
//RecurringJob.AddOrUpdate<MealReminderJob>(
//    "meal-reminder-job",
//    job => job.Run(),
//    Cron.Minutely);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();  // <-- Show detailed errors when developing
}
else
{
    app.UseExceptionHandler("/Home/Error");  // <-- Use friendly error page in production
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


//Use authentication and authorization
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Login}/{id?}");

app.Run();

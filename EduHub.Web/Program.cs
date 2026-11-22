using EduHub.Core.Entities;
using EduHub.Core.Interfaces;
using EduHub.Core.Patterns.Observer;
using EduHub.Data;
using EduHub.Data.Repositories;
using EduHub.Services.Implementations;
using EduHub.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration (Default SQLite for instant zero-dependency out-of-the-box run, or SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                      ?? "Data Source=eduhub.db";
bool useSqlite = builder.Configuration.GetValue<bool>("UseSqlite", true);

builder.Services.AddDbContext<EduHubDbContext>(options =>
{
    if (useSqlite)
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        var sqlServerConn = builder.Configuration.GetConnectionString("SqlServerConnection") ?? connectionString;
        options.UseSqlServer(sqlServerConn);
    }
});

// 2. Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<EduHubDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// 3. Session & Caching
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4. Repositories and Unit of Work (Repository & Unit of Work Pattern)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// 5. Business Logic Services (NTier Architecture & Dependency Injection)
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// 6. Observer Pattern Registration
builder.Services.AddScoped<ICourseObserver, CertificateGeneratorObserver>();
builder.Services.AddScoped<ICourseObserver, NotificationObserver>();
builder.Services.AddScoped<ICourseProgressSubject, CourseProgressSubject>();

// 7. Large File & Video Upload Configuration (Up to 1 GB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1024L * 1024L * 1024L; // 1 GB
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 1024L * 1024L * 1024L; // 1 GB
});

// 8. Antiforgery Configuration for AJAX Header Support
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// MVC & Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed Database automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<EduHubDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await DbInitializer.SeedAsync(context, userManager, roleManager);
        app.Logger.LogInformation("Database initialized and seeded successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred during database seeding.");
    }
}

// HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

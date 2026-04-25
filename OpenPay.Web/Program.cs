using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenPay.Application.Interfaces;
using OpenPay.Infrastructure.Banking;
using OpenPay.Infrastructure.Persistence;
using OpenPay.Infrastructure.Security;
using OpenPay.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Data Source=openpay-dev.db";

builder.Services.AddDbContext<OpenPayDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<OpenPayDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddScoped<ICounterpartyService, CounterpartyService>();
builder.Services.AddScoped<IOrganizationBankAccountService, OrganizationBankAccountService>();
builder.Services.AddScoped<IPaymentOrderService, PaymentOrderService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IBankProcessingService, BankProcessingService>();
builder.Services.AddScoped<IBankGatewayService, FakeBankGatewayService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<IBankStatusProcessor, BankStatusProcessor>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentOrganizationService, CurrentOrganizationService>();
builder.Services.AddHostedService<BankStatusBackgroundService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IOrganizationManagementService, OrganizationManagementService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();

builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var dbContext = services.GetRequiredService<OpenPayDbContext>();

    await dbContext.Database.MigrateAsync();
    await IdentitySeeder.SeedAsync(userManager, roleManager, dbContext);
}

app.MapRazorPages();

app.Run();
public partial class Program { }
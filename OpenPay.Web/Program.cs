using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using OpenPay.Application.Interfaces;
using OpenPay.Infrastructure.Banking;
using OpenPay.Infrastructure.Persistence;
using OpenPay.Infrastructure.Security;
using OpenPay.Infrastructure.Services;
using OpenPay.Web.Middleware;
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
builder.Services.AddScoped<IApprovalRouteService, ApprovalRouteService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IBankProcessingService, BankProcessingService>();
builder.Services.AddScoped<IBankAdapter, TBankDemoAdapter>();
builder.Services.AddScoped<IBankAdapter, SberDemoAdapter>();
builder.Services.AddScoped<IBankAdapterRegistry, BankAdapterRegistry>();
builder.Services.AddScoped<IBankGatewayService, BankGatewayService>();
builder.Services.AddScoped<IBankConnectionService, BankConnectionService>();
builder.Services.AddScoped<IBankStatementService, BankStatementService>();
builder.Services.AddScoped<ITokenProtectionService, TokenProtectionService>();
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
builder.Services.AddDataProtection();

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

app.UseMiddleware<OrganizationStateMiddleware>();

if (!app.Environment.IsEnvironment("Testing"))
{
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
var dbContext = services.GetRequiredService<OpenPayDbContext>();
var tokenProtectionService = services.GetRequiredService<ITokenProtectionService>();

await dbContext.Database.MigrateAsync();
await IdentitySeeder.SeedAsync(userManager, roleManager, dbContext, tokenProtectionService);
}

app.MapRazorPages();

app.Run();

public partial class Program { }

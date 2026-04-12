using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenPay.Infrastructure.Persistence;

public class OpenPayDbContextFactory : IDesignTimeDbContextFactory<OpenPayDbContext>
{
    public OpenPayDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OpenPayDbContext>();
        optionsBuilder.UseSqlite("Data Source=openpay-dev.db");

        return new OpenPayDbContext(optionsBuilder.Options);
    }
}
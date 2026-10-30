using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PTCenter.Infrastructure.Persistence;

/// <summary>Migration generation never loads the API's environment or production connection strings.</summary>
public class BookingDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=PTCenter_MigrationDesignOnly;Trusted_Connection=True;TrustServerCertificate=True").Options);
}

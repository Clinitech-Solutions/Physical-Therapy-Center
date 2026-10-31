using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PTCenter.Infrastructure.Persistence;
using Xunit;

namespace PTCenter.Tests;

public class BookingMigrationTests
{
    [Fact] public void SqlServer_migration_matches_the_current_model_and_generates_without_a_database_connection()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=PTCenter_MigrationDesignOnly;Trusted_Connection=True;TrustServerCertificate=True").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var sql = db.GetService<IMigrator>().GenerateScript("20261024151158_AddInsuranceDetails", "20261029124607_BookingWorkflow", MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("CREATE TABLE [BookingBatches]", sql);
        Assert.Contains("IX_Payments_IdempotencyKey", sql);
        Assert.Contains("Review legacy SeniorId", sql);
        Assert.Contains("[CompletedAt]", sql);
        var finance = db.GetService<IMigrator>().GenerateScript("20261029124607_BookingWorkflow", "20261030184822_CourseFinancialAgreements", MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("CREATE TABLE [TreatmentCourseAgreements]",finance);
        Assert.Contains("CREATE TABLE [InsuranceClaims]",finance);
        Assert.Contains("ALTER COLUMN [AppointmentId] int NULL",finance);
        Assert.DoesNotContain("DROP TABLE",finance);Assert.DoesNotContain("DROP COLUMN",finance);
        Assert.DoesNotContain("DELETE FROM",finance);Assert.DoesNotContain("UPDATE [",finance);Assert.DoesNotContain("TRUNCATE",finance);
        var audit=db.GetService<IMigrator>().GenerateScript("20261030184822_CourseFinancialAgreements","20261031122754_AppointmentScheduleAudit",MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("CREATE TABLE [AppointmentScheduleChanges]",audit);Assert.Contains("CREATE UNIQUE INDEX [IX_AppointmentScheduleChanges_OperationKey]",audit);
        Assert.DoesNotContain("DROP TABLE",audit);Assert.DoesNotContain("DROP COLUMN",audit);Assert.DoesNotContain("DELETE FROM",audit);Assert.DoesNotContain("UPDATE [",audit);Assert.DoesNotContain("ALTER COLUMN",audit);
    }
}

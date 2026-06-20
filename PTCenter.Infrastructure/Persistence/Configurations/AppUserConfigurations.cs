using PTCenter.Domain.Entities;
using PTCenter.Infrastructure.Identity.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PTCenter.Infrastructure.Persistence.Configurations;

public class AppUserConfigurations : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        var users = new List<ApplicationUser>()
        {
            new ApplicationUser
            {
                Id = DefaultUsers.Admin.Id,
                UserName = DefaultUsers.Admin.UserName,
                FullName = DefaultUsers.Admin.UserName,
                NormalizedUserName = DefaultUsers.Admin.UserName.ToUpper(),
                SecurityStamp = DefaultUsers.Admin.AdminSecurityStamp,
                ConcurrencyStamp = DefaultUsers.Admin.AdminConcurrencyStamp,
                EmailConfirmed = true,
                PasswordHash = DefaultUsers.Admin.AdminPasswordHash,
            }
        };
        
        
        builder.HasData(users);
      
    }
}
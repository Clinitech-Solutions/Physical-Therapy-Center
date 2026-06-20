using PTCenter.Infrastructure.Identity.Seeds;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PTCenter.Infrastructure.Persistence.Configurations;

public class RoleConfigurations : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder)
    {
        
        var roles = new List<IdentityRole>()
        {
            new IdentityRole(){
                Id = DefaultRoles.Admin.Id,
                Name = DefaultRoles.Admin.Name,
                NormalizedName = DefaultRoles.Admin.Name.ToUpper(),
                ConcurrencyStamp = DefaultRoles.Admin.ConcurrencyStamp,
            },
            new IdentityRole(){
                Id = DefaultRoles.Doctor.Id,
                Name = DefaultRoles.Doctor.Name,
                NormalizedName = DefaultRoles.Doctor.Name.ToUpper(),
                ConcurrencyStamp = DefaultRoles.Doctor.ConcurrencyStamp,
            },
            new IdentityRole(){
                Id = DefaultRoles.Patient.Id,
                Name = DefaultRoles.Patient.Name,
                NormalizedName = DefaultRoles.Patient.Name.ToUpper(),
                ConcurrencyStamp = DefaultRoles.Patient.ConcurrencyStamp,
            },
            new IdentityRole(){
                Id = DefaultRoles.Owner.Id,
                Name = DefaultRoles.Owner.Name,
                NormalizedName = DefaultRoles.Owner.Name.ToUpper(),
                ConcurrencyStamp = DefaultRoles.Owner.ConcurrencyStamp,
            },
            
        };
        
        builder.HasData(roles);
    }
}
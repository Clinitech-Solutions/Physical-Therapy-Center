using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace PTCenter.Infrastructure.Persistence.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment> 
    {
        public void Configure(EntityTypeBuilder<Payment> b) 
        {
            b.HasKey(x => x.Id);
            b.HasQueryFilter(x => !x.IsDeleted);
        } 
    }
}

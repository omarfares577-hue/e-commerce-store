using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Store.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Store.Infrastructure.Identity
{
    public class DbIdentityContext : IdentityDbContext
    {
        public DbIdentityContext(DbContextOptions<DbIdentityContext> options) : base(options) { }
        public DbSet<Address> Addresses { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Address>().HasOne(p=>p.AppUser)
                .WithMany(p => p.Address).HasForeignKey(p => p.AppUserId).IsRequired(false);
            base.OnModelCreating(builder);
        } 
    }
}

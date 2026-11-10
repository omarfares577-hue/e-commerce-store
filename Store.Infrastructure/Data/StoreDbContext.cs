using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Store.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Store.Infrastructure.Data
{
    public class StoreDbContext(DbContextOptions<StoreDbContext> options) : DbContext(options)
    {
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; } = null!;


        public DbSet<DeliveryMethod> DeliveryMethods { get; set; } = null!;
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
           base.ConfigureConventions(configurationBuilder);

           configurationBuilder
               .Properties<DateTimeOffset>()
               .HaveConversion<Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter>();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                    .SelectMany(t => t.GetProperties())
                    .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(18,2)");
            }
            modelBuilder.Entity<Product>().Property(p => p.StockQuantity).HasDefaultValue(0);
            modelBuilder.Entity<Product>().Property(p => p.Name).HasMaxLength(100);
            modelBuilder.Entity<Category>().Property(c => c.Name).HasMaxLength(50);
            modelBuilder.Entity<Category>().Property(c => c.Description).HasMaxLength(500);
            modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Order>().OwnsOne(o => o.ShippingAddress, a =>
            {
                a.WithOwner();
            });
            modelBuilder.Entity<Order>().OwnsMany(o => o.OrderItems, b =>
            {
                b.WithOwner();
                b.Property<int>("Id");
                b.HasKey("Id");

                b.OwnsOne(i => i.ItemOrdered, io =>
                {
                    io.WithOwner();
                });
            }); 
            modelBuilder.Entity<Order>().HasOne(o => o.DeliveryMethod)
              .WithMany()
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Store.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
namespace Store.Infrastructure.Data
{
   public static class StoreContextSeed
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static async Task SeedAsync(StoreDbContext context)
        {
            var assembly = typeof(StoreContextSeed).Assembly;

            // ---------- Categories ----------
            if (!await context.Categories.AnyAsync())
            {
                var categories = await ReadSeedAsync<Category>(assembly, "categories.json");
                if (categories.Count > 0)
                {
                    await context.Categories.AddRangeAsync(categories);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"SEED: categories added = {categories.Count}");
                }
            }

            // ---------- Products ----------
            if (!await context.Products.AnyAsync())
            {
                var products = await ReadSeedAsync<Product>(assembly, "products.json");
                if (products.Count > 0)
                {
                    await context.Products.AddRangeAsync(products);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"SEED: products added = {products.Count}");
                }
            }

            // ---------- Delivery Methods ----------
            if (!await context.DeliveryMethods.AnyAsync())
            {
                var methods = await ReadSeedAsync<DeliveryMethod>(assembly, "delivery.json");
                if (methods.Count > 0)
                {
                    await context.DeliveryMethods.AddRangeAsync(methods);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"SEED: delivery methods added = {methods.Count}");
                }
            }

            // ---------- Orders ----------
            if (!await context.Orders.AnyAsync())
            {
                var delivery = await context.DeliveryMethods.FirstOrDefaultAsync();
                var product = await context.Products.FirstOrDefaultAsync();

                if (delivery != null && product != null)
                {
                    var itemOrdered = new ProductItemOrdered(product.Id, product.Name, product.PictureUrl ?? "");
                    var items = new List<OrderItems> { new OrderItems(itemOrdered, product.Price, 1) };

                    var order = new Order(
                        items,
                        "seed@store.com",
                        new ShippingAddress
                        {
                            FirstName = "John",
                            LastName = "Doe",
                            Street = "123 Main St",
                            City = "New York",
                            State = "NY",
                            ZipCode = "10001"
                        },
                        delivery,
                        product.Price,
                        "pi_seed_" + Guid.NewGuid().ToString("N"));

                    await context.Orders.AddAsync(order);
                    await context.SaveChangesAsync();
                    Console.WriteLine("SEED: orders added = 1");
                }
            }
        }
        private static async Task<List<T>> ReadSeedAsync<T>(Assembly assembly, string fileName)
        {
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                ?? throw new FileNotFoundException($"Embedded resource not found: {fileName}");

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
        }
    }
}        
    


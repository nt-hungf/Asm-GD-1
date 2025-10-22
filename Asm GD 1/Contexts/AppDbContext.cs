using Asm_GD_1.Models;
using Microsoft.EntityFrameworkCore;

namespace Asm_GD_1.Contexts
{
public class AppDbContext : DbContext
{

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Food> Foods { get; set; }
    public DbSet<Combo> Combos { get; set; }
    public DbSet<ComboFood> ComboFoods { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ComboFood: dùng khóa chính riêng
        modelBuilder.Entity<ComboFood>()
            .HasKey(cf => cf.ComboFoodId);

        // Food → Category (1-nhiều)
        modelBuilder.Entity<Food>()
            .HasOne(f => f.Category)
            .WithMany(c => c.Foods)
            .HasForeignKey(f => f.CategoryId);

        // Order → User (1-nhiều)
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId);

            // OrderDetail → Order (1-nhiều)
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .IsRequired(false);
        // OrderDetail → Food (1-nhiều)
        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Food)
            .WithMany(f => f.OrderDetails)
            .HasForeignKey(od => od.FoodId)
            .IsRequired(false);
        // ✅ OrderDetail → Combo (1-nhiều)
        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Combo)
            .WithMany(c => c.OrderDetails)
            .HasForeignKey(od => od.ComboId)
            .IsRequired(false); // nếu Combo là tùy chọn

        // ComboFood → Combo (1-nhiều)
        modelBuilder.Entity<ComboFood>()
            .HasOne(cf => cf.Combo)
            .WithMany(c => c.ComboFoods)
            .HasForeignKey(cf => cf.ComboId);

        // ComboFood → Food (1-nhiều)
        modelBuilder.Entity<ComboFood>()
            .HasOne(cf => cf.Food)
            .WithMany(f => f.ComboFoods)
            .HasForeignKey(cf => cf.FoodId);
    }
}
    }
using Microsoft.EntityFrameworkCore;
using BE.Models.Data;
using BE.Models.Entities;

namespace BE.Models.Data
{
    public class AppBookStoreContext : DbContext
    {
        public AppBookStoreContext(DbContextOptions<AppBookStoreContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Publisher> Publishers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<PromotionItem> PromotionItems { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role).HasDefaultValue("customer");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("getdate()");
            });

            // Author Configuration
            modelBuilder.Entity<Author>(entity =>
            {
                entity.ToTable("Authors");
            });

            // Publisher Configuration
            modelBuilder.Entity<Publisher>(entity =>
            {
                entity.ToTable("Publishers");
            });

            // Category Configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories");
            });

            // Book Configuration
            modelBuilder.Entity<Book>(entity =>
            {
                entity.ToTable("Books");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("getdate()");
                entity.Property(e => e.Price).HasPrecision(10, 2);

                entity.HasOne(b => b.Category)
                    .WithMany(c => c.Books)
                    .HasForeignKey(b => b.CategoryId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(b => b.Author)
                    .WithMany(a => a.Books)
                    .HasForeignKey(b => b.AuthorId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(b => b.Publisher)
                    .WithMany(p => p.Books)
                    .HasForeignKey(b => b.PublisherId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Cart Configuration
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.ToTable("Cart");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("getdate()");

                entity.HasOne(c => c.User)
                    .WithMany(u => u.Carts)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // CartItem Configuration
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.ToTable("CartItems");

                entity.HasOne(ci => ci.Cart)
                    .WithMany(c => c.CartItems)
                    .HasForeignKey(ci => ci.CartId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.Book)
                    .WithMany(b => b.CartItems)
                    .HasForeignKey(ci => ci.BookId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Order Configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.Property(e => e.Status).HasDefaultValue("pending");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("getdate()");
                entity.Property(e => e.TotalPrice).HasPrecision(10, 2);

                entity.HasOne(o => o.User)
                    .WithMany(u => u.Orders)
                    .HasForeignKey(o => o.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // OrderDetail Configuration
            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.ToTable("OrderDetails");
                entity.Property(e => e.Price).HasPrecision(10, 2);

                entity.HasOne(od => od.Order)
                    .WithMany(o => o.OrderDetails)
                    .HasForeignKey(od => od.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(od => od.Book)
                    .WithMany(b => b.OrderDetails)
                    .HasForeignKey(od => od.BookId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Promotion Configuration
            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.ToTable("Promotions");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.DiscountValue).HasPrecision(10, 2);
            });

            // PromotionItem Configuration
            modelBuilder.Entity<PromotionItem>(entity =>
            {
                entity.ToTable("PromotionItems");
                entity.Property(e => e.SpecificDiscount).HasPrecision(10, 2);

                entity.HasOne(pi => pi.Promotion)
                    .WithMany(p => p.PromotionItems)
                    .HasForeignKey(pi => pi.PromotionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pi => pi.Book)
                    .WithMany(b => b.PromotionItems)
                    .HasForeignKey(pi => pi.BookId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Contact Configuration
            modelBuilder.Entity<Contact>(entity =>
            {
                entity.ToTable("Contact");
                entity.Property(e => e.Status).HasDefaultValue("pending");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("getdate()");
            });

            // Payment Configuration
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("Payments");
                entity.Property(e => e.PaymentStatus).HasDefaultValue("Pending");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("getdate()");
                entity.Property(e => e.Amount).HasPrecision(10, 2);

                entity.HasOne(p => p.Order)
                    .WithMany(o => o.Payments)
                    .HasForeignKey(p => p.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.User)
                    .WithMany(u => u.Payments)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}

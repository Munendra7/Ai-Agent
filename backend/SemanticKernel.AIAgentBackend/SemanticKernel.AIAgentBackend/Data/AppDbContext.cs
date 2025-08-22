using Microsoft.EntityFrameworkCore;
using SemanticKernel.AIAgentBackend.Models.Domain;

namespace SemanticKernel.AIAgentBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<ChatHistory> ChatHistory { get; set; }
        public DbSet<SessionSummary> SessionSummaries { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatHistory>().HasIndex(x => x.SessionId);
            modelBuilder.Entity<ChatHistory>().HasKey(x => x.Id);
            modelBuilder.Entity<ChatHistory>()
            .Property(u => u.Id)
            .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<SessionSummary>().HasIndex(x => x.SessionId);
            modelBuilder.Entity<SessionSummary>().HasKey(x => x.SessionId);

            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).HasDefaultValueSql("NEWID()");
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => new { u.Provider, u.ProviderId }).IsUnique();
            });

            // Role entity configuration
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).HasDefaultValueSql("NEWID()");
                entity.HasIndex(r => r.Name).IsUnique();
            });

            // UserRole entity configuration
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(ur => ur.Id);
                entity.Property(ur => ur.Id).HasDefaultValueSql("NEWID()");
                entity.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();

                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed default roles
            var adminRoleId = Guid.NewGuid();
            var userRoleId = Guid.NewGuid();
            
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = adminRoleId, Name = "Admin", Description = "Administrator with full access", CreatedAt = DateTime.UtcNow },
                new Role { Id = userRoleId, Name = "User", Description = "Regular user with limited access", CreatedAt = DateTime.UtcNow }
            );
        }
    }
}

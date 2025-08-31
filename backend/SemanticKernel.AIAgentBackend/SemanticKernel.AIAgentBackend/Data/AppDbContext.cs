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
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatHistory>(entity =>
            {
                entity.HasIndex(x => new { x.SessionId, x.UserId });
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id)
                    .HasDefaultValueSql("NEWID()");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.ChatHistories)
                    .HasForeignKey(e => e.UserId);

                entity.HasOne(e => e.SessionSummary)
                    .WithMany(s => s.ChatHistories)
                    .HasForeignKey(e => e.SessionId);
            });

            modelBuilder.Entity<SessionSummary>(entity =>
            {
                entity.HasKey(e => e.SessionId);
                entity.HasOne(s => s.User)
                    .WithMany(user => user.SessionSummaries)
                    .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(256);
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(e => e.UserId);

                entity.HasOne(e => e.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(e => e.RoleId);
            });


            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(e => e.UserId);
            });

            modelBuilder.Entity<Role>().HasData(
                 new Role
                 {
                     Id = new Guid("11111111-1111-1111-1111-111111111111"),
                     Name = "Admin",
                     Description = "Administrator role"
                 },
                 new Role
                 {
                     Id = new Guid("22222222-2222-2222-2222-222222222222"),
                     Name = "User",
                     Description = "Regular user role"
                 }
            );
        }
    }
}

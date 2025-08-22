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
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatHistory>().HasIndex(x => x.SessionId);
            modelBuilder.Entity<ChatHistory>().HasKey(x => x.Id);
            modelBuilder.Entity<ChatHistory>()
            .Property(u => u.Id)
            .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<SessionSummary>().HasIndex(x => x.SessionId);
            modelBuilder.Entity<SessionSummary>().HasKey(x => x.SessionId);

            // User configuration
            modelBuilder.Entity<User>().HasKey(x => x.Id);
            modelBuilder.Entity<User>().Property(u => u.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
            modelBuilder.Entity<User>().HasIndex(x => new { x.Provider, x.ProviderId }).IsUnique();

            // RefreshToken configuration
            modelBuilder.Entity<RefreshToken>().HasKey(x => x.Id);
            modelBuilder.Entity<RefreshToken>().Property(u => u.Id).HasDefaultValueSql("NEWID()");
            modelBuilder.Entity<RefreshToken>().HasIndex(x => x.Token).IsUnique();
            modelBuilder.Entity<RefreshToken>().HasIndex(x => x.UserId);

            // Relationships
            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatHistory>()
                .HasOne<User>()
                .WithMany(u => u.ChatHistories)
                .HasForeignKey(ch => ch.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SessionSummary>()
                .HasOne<User>()
                .WithMany(u => u.SessionSummaries)
                .HasForeignKey(ss => ss.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

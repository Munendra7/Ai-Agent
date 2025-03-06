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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatHistory>().HasIndex(x => x.UserId);
            modelBuilder.Entity<ChatHistory>().HasKey(x => x.Id);
            modelBuilder.Entity<ChatHistory>()
            .Property(u => u.Id)
            .HasDefaultValueSql("NEWID()");
        }
    }
}

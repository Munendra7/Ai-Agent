using AuthDemo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthDemo.Api.Data
{
	public class AppDbContext : DbContext
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
		{
		}

		public DbSet<User> Users => Set<User>();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<User>()
				.HasIndex(u => u.Email)
				.IsUnique();

			modelBuilder.Entity<User>()
				.HasIndex(u => new { u.Provider, u.ProviderUserId })
				.IsUnique();
		}
	}
}
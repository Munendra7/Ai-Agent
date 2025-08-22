using System;
using System.ComponentModel.DataAnnotations;

namespace AuthDemo.Api.Models
{
	public enum AppRole
	{
		User = 0,
		Admin = 1
	}

	public class User
	{
		[Key]
		public Guid Id { get; set; }

		[Required]
		[MaxLength(256)]
		public string Email { get; set; } = string.Empty;

		[MaxLength(200)]
		public string DisplayName { get; set; } = string.Empty;

		[MaxLength(500)]
		public string? AvatarUrl { get; set; }

		[Required]
		public AppRole Role { get; set; } = AppRole.User;

		[MaxLength(100)]
		public string? Provider { get; set; }

		[MaxLength(200)]
		public string? ProviderUserId { get; set; }

		public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
	}
}
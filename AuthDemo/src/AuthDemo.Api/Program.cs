using AuthDemo.Api.Data;
using AuthDemo.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuration
var config = builder.Configuration;

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlite(config.GetConnectionString("Default") ?? "Data Source=app.db"));

// CORS (allow dev origin frontend)
var corsPolicy = "frontend";
builder.Services.AddCors(options =>
{
	options.AddPolicy(corsPolicy, policy =>
	{
		policy
			.WithOrigins(config["Frontend:Origin"] ?? "http://localhost:5173")
			.AllowAnyHeader()
			.AllowAnyMethod()
			.AllowCredentials();
	});
});

// JWT options
var jwtKey = config["Jwt:Key"] ?? "dev_secret_key_change_me";
var jwtIssuer = config["Jwt:Issuer"] ?? "AuthDemo";
var jwtAudience = config["Jwt:Audience"] ?? "AuthDemoClient";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

// Multi-scheme auth: prefer JWT when Authorization header present, else Cookies
builder.Services
	.AddAuthentication(options =>
	{
		options.DefaultScheme = "MultiAuth";
		options.DefaultChallengeScheme = "MultiAuth";
		options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
	})
	.AddPolicyScheme("MultiAuth", "JWT or Cookie", options =>
	{
		options.ForwardDefaultSelector = context =>
		{
			string? authorization = context.Request.Headers["Authorization"];
			if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
			{
				return JwtBearerDefaults.AuthenticationScheme;
			}
			return CookieAuthenticationDefaults.AuthenticationScheme;
		};
	})
	.AddCookie(options =>
	{
		options.Cookie.Name = "authdemo.session";
		options.Cookie.SameSite = SameSiteMode.Lax;
		options.Cookie.HttpOnly = true;
		options.Events.OnSigningIn = ctx => Task.CompletedTask;
	})
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = signingKey,
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidIssuer = jwtIssuer,
			ValidAudience = jwtAudience,
			ClockSkew = TimeSpan.FromMinutes(2)
		};
	})
	.AddGoogle(GoogleDefaults.AuthenticationScheme, o =>
	{
		o.ClientId = config["Authentication:Google:ClientId"] ?? string.Empty;
		o.ClientSecret = config["Authentication:Google:ClientSecret"] ?? string.Empty;
		o.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
	})
	.AddMicrosoftAccount(o =>
	{
		o.ClientId = config["Authentication:Microsoft:ClientId"] ?? string.Empty;
		o.ClientSecret = config["Authentication:Microsoft:ClientSecret"] ?? string.Empty;
	})
	.AddGitHub(o =>
	{
		o.ClientId = config["Authentication:GitHub:ClientId"] ?? string.Empty;
		o.ClientSecret = config["Authentication:GitHub:ClientSecret"] ?? string.Empty;
		o.Scope.Add("user:email");
	});

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(AppRole.Admin)));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(corsPolicy);
app.UseAuthentication();
app.UseAuthorization();

// helper: issue jwt
static string IssueJwtToken(User user, string issuer, string audience, SymmetricSecurityKey signingKey)
{
	var claims = new List<Claim>
	{
		new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
		new Claim(JwtRegisteredClaimNames.Email, user.Email),
		new Claim(ClaimTypes.Name, user.DisplayName ?? string.Empty),
		new Claim(ClaimTypes.Role, user.Role.ToString())
	};
	var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
	var jwt = new JwtSecurityToken(
		issuer: issuer,
		audience: audience,
		claims: claims,
		notBefore: DateTime.UtcNow,
		expires: DateTime.UtcNow.AddHours(8),
		signingCredentials: creds
	);
	return new JwtSecurityTokenHandler().WriteToken(jwt);
}

// Auth endpoints
app.MapGet("/api/me", async (ClaimsPrincipal principal, AppDbContext db) =>
{
	// If authenticated via JWT, use token claims; else use DB lookup by email
	var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue(JwtRegisteredClaimNames.Email);
	if (string.IsNullOrWhiteSpace(email)) return Results.Unauthorized();
	var entity = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
	return entity is null ? Results.NotFound() : Results.Ok(entity);
}).RequireAuthorization();

app.MapGet("/api/login/{provider}", async (string provider, HttpContext http) =>
{
	var props = new AuthenticationProperties
	{
		RedirectUri = "/api/auth/callback"
	};
	return Results.Challenge(props, new[] { provider });
});

app.MapGet("/api/auth/callback", async (HttpContext http, AppDbContext db) =>
{
	var result = await http.AuthenticateAsync();
	if (!result.Succeeded || result.Principal is null)
	{
		return Results.Unauthorized();
	}

	var principal = result.Principal;
	var email = principal.FindFirstValue(ClaimTypes.Email);
	var name = principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
	var avatar = principal.FindFirstValue("urn:github:avatar") ?? principal.FindFirstValue("picture");
	var provider = principal.Identity?.AuthenticationType ?? result.Ticket?.AuthenticationScheme ?? "unknown";
	var providerUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
	if (string.IsNullOrWhiteSpace(email))
	{
		return Results.BadRequest("No email from provider");
	}

	var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
	if (user is null)
	{
		user = new User
		{
			Id = Guid.NewGuid(),
			Email = email,
			DisplayName = name,
			AvatarUrl = avatar,
			Role = AppRole.User,
			Provider = provider,
			ProviderUserId = providerUserId,
			CreatedUtc = DateTime.UtcNow,
			UpdatedUtc = DateTime.UtcNow
		};
		await db.Users.AddAsync(user);
	}
	else
	{
		user.DisplayName = name;
		user.AvatarUrl = avatar;
		user.Provider = provider;
		user.ProviderUserId = providerUserId;
		user.UpdatedUtc = DateTime.UtcNow;
	}
	await db.SaveChangesAsync();

	// Issue JWT for frontend usage
	var token = IssueJwtToken(user, jwtIssuer, jwtAudience, signingKey);
	var origin = app.Configuration["Frontend:Origin"] ?? "http://localhost:5173";
	var redirectUrl = $"{origin}/auth/callback?token={Uri.EscapeDataString(token)}";
	return Results.Redirect(redirectUrl);
});

app.MapPost("/api/logout", async (HttpContext http) =>
{
	await http.SignOutAsync();
	return Results.Ok();
}).RequireAuthorization();

app.MapGet("/api/admin/secret", () => Results.Ok(new { secret = "top-secret" }))
	.RequireAuthorization("AdminOnly");

app.Run();

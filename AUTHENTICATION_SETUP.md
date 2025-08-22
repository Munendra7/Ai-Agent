# OAuth Authentication Setup Guide

This guide will help you set up Google, Microsoft, and GitHub OAuth authentication for your React frontend and .NET backend application.

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Node.js 18+ and npm
- SQL Server (or SQL Server in Docker)
- OAuth apps configured with Google, Microsoft, and GitHub

## 📋 Backend Setup (.NET)

### 1. Database Migration
```bash
cd backend/SemanticKernel.AIAgentBackend/SemanticKernel.AIAgentBackend
dotnet ef migrations add AddAuthenticationTables
dotnet ef database update
```

### 2. Configure OAuth Settings
Update `appsettings.json` with your OAuth credentials:

```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast256BitsLongForSecurityPurposes!",
    "Issuer": "SemanticKernel.AIAgentBackend",
    "Audience": "SemanticKernel.AIAgentBackend.Users",
    "ExpiryMinutes": "60"
  },
  "OAuth": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret",
      "RedirectUri": "http://localhost:3000/auth/google/callback"
    },
    "Microsoft": {
      "ClientId": "your-microsoft-client-id",
      "ClientSecret": "your-microsoft-client-secret", 
      "RedirectUri": "http://localhost:3000/auth/microsoft/callback"
    },
    "GitHub": {
      "ClientId": "your-github-client-id",
      "ClientSecret": "your-github-client-secret",
      "RedirectUri": "http://localhost:3000/auth/github/callback"
    }
  }
}
```

### 3. Run Backend
```bash
cd backend/SemanticKernel.AIAgentBackend/SemanticKernel.AIAgentBackend
dotnet run
```

## 🎨 Frontend Setup (React)

### 1. Install Dependencies
```bash
cd frontend/ai-agent
npm install
```

### 2. Configure Environment
Create `.env` file:
```bash
VITE_API_BASE_URL=https://localhost:7036/api
```

### 3. Run Frontend
```bash
cd frontend/ai-agent
npm run dev
```

## 🔧 OAuth Provider Configuration

### Google OAuth Setup
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Enable Google+ API
4. Go to "Credentials" → "Create Credentials" → "OAuth 2.0 Client IDs"
5. Set application type to "Web application"
6. Add authorized redirect URIs:
   - `http://localhost:3000/auth/google/callback`
   - `https://yourdomain.com/auth/google/callback` (for production)
7. Copy Client ID and Client Secret

### Microsoft OAuth Setup
1. Go to [Azure Portal](https://portal.azure.com/)
2. Navigate to "Azure Active Directory" → "App registrations"
3. Click "New registration"
4. Set redirect URI to:
   - `http://localhost:3000/auth/microsoft/callback`
   - `https://yourdomain.com/auth/microsoft/callback` (for production)
5. Go to "Certificates & secrets" → "New client secret"
6. Copy Application (client) ID and client secret

### GitHub OAuth Setup
1. Go to [GitHub Settings](https://github.com/settings/developers)
2. Click "OAuth Apps" → "New OAuth App"
3. Set Authorization callback URL to:
   - `http://localhost:3000/auth/github/callback`
   - `https://yourdomain.com/auth/github/callback` (for production)
4. Copy Client ID and Client Secret

## 🛡️ Security Features

### JWT Token Management
- Secure token generation with configurable expiry
- Automatic token refresh
- Role-based claims in JWT

### Role-Based Access Control
- Default "User" role for all new users
- Admin role for privileged operations
- Protected routes based on user roles
- Fine-grained permission control

### OAuth Security
- State parameter validation
- Secure token exchange
- Provider-specific user data handling

## 🎯 API Endpoints

### Authentication Endpoints
```
GET  /api/auth/google/login      - Get Google OAuth URL
GET  /api/auth/microsoft/login   - Get Microsoft OAuth URL  
GET  /api/auth/github/login      - Get GitHub OAuth URL

POST /api/auth/google/callback   - Handle Google OAuth callback
POST /api/auth/microsoft/callback - Handle Microsoft OAuth callback
POST /api/auth/github/callback   - Handle GitHub OAuth callback

GET  /api/auth/me               - Get current user info (requires auth)
POST /api/auth/refresh          - Refresh JWT token (requires auth)
POST /api/auth/logout           - Logout user (requires auth)
```

## 🎨 Frontend Components

### Main Components
- `Login.tsx` - Beautiful OAuth login page
- `Dashboard.tsx` - User dashboard with role-based content
- `ProtectedRoute.tsx` - Route protection component
- `OAuthCallback.tsx` - OAuth callback handler
- `NavBar.tsx` - Navigation with user menu

### Authentication Context
- `AuthContext.tsx` - React context for auth state
- `authService.ts` - API service layer
- `types/auth.ts` - TypeScript type definitions

## 🔄 Authentication Flow

1. **User clicks login** → Redirected to OAuth provider
2. **OAuth provider callback** → Backend exchanges code for token
3. **User creation/login** → JWT token generated with user roles
4. **Frontend receives token** → Stored in localStorage
5. **Protected routes** → Token validated on each request
6. **Role-based access** → Components render based on user roles

## 🎭 User Roles

### Default Roles
- **User**: Basic access to dashboard and chat
- **Admin**: Full access including admin panel

### Role Management
Users are automatically assigned the "User" role upon first login. Admin roles must be assigned manually through the database or admin interface.

## 🚨 Troubleshooting

### Common Issues

1. **OAuth Redirect Mismatch**
   - Ensure redirect URIs in OAuth providers match exactly
   - Check for trailing slashes and protocol (http vs https)

2. **JWT Token Issues**
   - Verify JWT secret key is properly configured
   - Check token expiry settings
   - Ensure system clocks are synchronized

3. **CORS Errors**
   - Backend CORS is configured for all origins in development
   - Update CORS settings for production

4. **Database Connection**
   - Verify SQL Server is running
   - Check connection string in appsettings.json
   - Run database migrations

### Debug Tips
- Check browser developer console for errors
- Review backend logs for authentication failures
- Verify OAuth provider settings and credentials
- Test API endpoints directly with tools like Postman

## 🚀 Production Deployment

### Backend
1. Update connection strings for production database
2. Configure secure JWT secret key
3. Update OAuth redirect URIs for production domain
4. Enable HTTPS and update CORS settings
5. Set up proper logging and monitoring

### Frontend
1. Update `VITE_API_BASE_URL` for production backend
2. Build production bundle: `npm run build`
3. Deploy to static hosting service
4. Configure proper domain for OAuth callbacks

## 📚 Additional Resources

- [JWT.io](https://jwt.io/) - JWT token decoder
- [OAuth 2.0 RFC](https://tools.ietf.org/html/rfc6749) - OAuth specification
- [React Router](https://reactrouter.com/) - Frontend routing
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) - Database ORM

## 🤝 Support

If you encounter any issues:
1. Check this documentation
2. Review error logs
3. Verify OAuth provider configurations
4. Test with minimal setup first
# Authentication System Setup Guide

This guide will help you set up Google, Microsoft, and GitHub OAuth authentication for your React frontend and .NET backend application.

## Table of Contents

1. [Backend Setup](#backend-setup)
2. [Frontend Setup](#frontend-setup)
3. [OAuth Provider Configuration](#oauth-provider-configuration)
4. [Database Migration](#database-migration)
5. [Testing the Authentication](#testing-the-authentication)

## Backend Setup

### 1. Install Required NuGet Packages

The following packages have been added to your `.csproj` file:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.Google" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.GitHub" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.0" />
```

### 2. Update Configuration

Add the following to your `appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "your-super-secret-key-with-at-least-32-characters-change-this-in-production",
    "Issuer": "your-app",
    "Audience": "your-app"
  },
  "ExternalAuth": {
    "Google": {
      "ClientId": "",
      "ClientSecret": ""
    },
    "Microsoft": {
      "ClientId": "",
      "ClientSecret": ""
    },
    "GitHub": {
      "ClientId": "",
      "ClientSecret": ""
    }
  }
}
```

### 3. Database Migration

Run the following command to create the database migration:

```bash
cd backend/SemanticKernel.AIAgentBackend
dotnet ef migrations add AddUserAuthentication
dotnet ef database update
```

## Frontend Setup

### 1. Install Dependencies

The following packages have been added to your `package.json`:

```json
{
  "@google-cloud/local-auth": "^2.1.0",
  "googleapis": "^128.0.0",
  "jwt-decode": "^4.0.0"
}
```

### 2. Environment Variables

Create a `.env` file in the `frontend/ai-agent` directory:

```env
# API Configuration
VITE_API_BASE_URL=http://localhost:5000/api

# Google OAuth Configuration
VITE_GOOGLE_CLIENT_ID=your-google-client-id
VITE_GOOGLE_REDIRECT_URI=http://localhost:5173/oauth-redirect

# Microsoft OAuth Configuration (MSAL)
VITE_MSAL_ClientId=your-microsoft-client-id
VITE_MSAL_Authority=https://login.microsoftonline.com/your-tenant-id
VITE_MSAL_RedirectUri=http://localhost:5173/oauth-redirect
VITE_Backend_Scope=api://your-backend-client-id/access_as_user

# GitHub OAuth Configuration
VITE_GITHUB_CLIENT_ID=your-github-client-id
VITE_GITHUB_REDIRECT_URI=http://localhost:5173/oauth-redirect
```

## OAuth Provider Configuration

### Google OAuth Setup

1. Go to the [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Enable the Google+ API
4. Go to "Credentials" → "Create Credentials" → "OAuth 2.0 Client IDs"
5. Configure the OAuth consent screen
6. Set the authorized redirect URIs:
   - `http://localhost:5173/oauth-redirect` (for development)
   - `https://yourdomain.com/oauth-redirect` (for production)
7. Copy the Client ID and Client Secret

### Microsoft OAuth Setup

1. Go to the [Azure Portal](https://portal.azure.com/)
2. Navigate to "Azure Active Directory" → "App registrations"
3. Click "New registration"
4. Configure the app:
   - Name: Your app name
   - Supported account types: Choose based on your needs
   - Redirect URI: `http://localhost:5173/oauth-redirect`
5. After registration, note the Application (client) ID
6. Go to "Certificates & secrets" → "New client secret"
7. Copy the Client ID and Client Secret

### GitHub OAuth Setup

1. Go to [GitHub Developer Settings](https://github.com/settings/developers)
2. Click "New OAuth App"
3. Configure the app:
   - Application name: Your app name
   - Homepage URL: `http://localhost:5173`
   - Authorization callback URL: `http://localhost:5173/oauth-redirect`
4. Copy the Client ID and Client Secret

## Features Implemented

### Backend Features

1. **User Management**
   - User registration and login via OAuth providers
   - User profile storage with role-based access control
   - JWT token generation and validation
   - Refresh token support

2. **Authentication Endpoints**
   - `POST /api/auth/login` - OAuth login
   - `POST /api/auth/refresh` - Token refresh
   - `POST /api/auth/logout` - User logout
   - `GET /api/auth/me` - Get current user

3. **Role-Based Authorization**
   - Admin, Moderator, and User roles
   - Custom authorization attributes
   - Route protection based on user roles

4. **External Provider Integration**
   - Google OAuth validation
   - Microsoft OAuth validation
   - GitHub OAuth validation

### Frontend Features

1. **Authentication Context**
   - Global authentication state management
   - Automatic token refresh
   - Persistent login sessions

2. **OAuth Integration**
   - Google OAuth popup flow
   - Microsoft OAuth via MSAL
   - GitHub OAuth popup flow

3. **User Interface**
   - Beautiful login page with OAuth buttons
   - User profile dropdown with account information
   - Role-based navigation
   - Loading states and error handling

4. **Route Protection**
   - Private route guards
   - Role-based route protection
   - Automatic redirects for unauthenticated users

## Usage Examples

### Protecting Routes by Role

```tsx
import RoleGuard from './components/RoleGuard';

// Admin-only route
<RoleGuard allowedRoles={['Admin']}>
  <AdminDashboard />
</RoleGuard>

// Admin or Moderator route
<RoleGuard allowedRoles={['Admin', 'Moderator']}>
  <ModeratorPanel />
</RoleGuard>
```

### Using Authentication in Components

```tsx
import { useAuth } from '../contexts/AuthContext';

const MyComponent = () => {
  const { user, isAuthenticated, login, logout } = useAuth();

  if (!isAuthenticated) {
    return <div>Please log in</div>;
  }

  return (
    <div>
      <h1>Welcome, {user?.firstName}!</h1>
      <p>Your role: {user?.role}</p>
      <button onClick={logout}>Logout</button>
    </div>
  );
};
```

### Making Authenticated API Calls

```tsx
import authService from '../services/authService';

// The service automatically includes the JWT token
const response = await authService.getCurrentUser();
```

## Security Considerations

1. **JWT Secret**: Use a strong, unique secret key in production
2. **HTTPS**: Always use HTTPS in production
3. **Token Expiration**: JWT tokens expire after 1 hour, refresh tokens after 7 days
4. **CORS**: Configure CORS properly for your domains
5. **Environment Variables**: Never commit sensitive credentials to version control

## Troubleshooting

### Common Issues

1. **CORS Errors**: Ensure your backend CORS configuration includes your frontend domain
2. **OAuth Redirect Issues**: Verify redirect URIs match exactly in OAuth provider settings
3. **Token Validation**: Check that JWT secret is consistent between frontend and backend
4. **Database Connection**: Ensure your database connection string is correct

### Debug Mode

Enable debug logging in your backend by adding:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

## Production Deployment

1. Update all OAuth redirect URIs to your production domain
2. Use strong, unique JWT secrets
3. Enable HTTPS
4. Configure proper CORS settings
5. Set up database backups
6. Monitor authentication logs

## Support

For issues or questions:
1. Check the browser console for frontend errors
2. Check the backend logs for server errors
3. Verify OAuth provider configurations
4. Ensure all environment variables are set correctly
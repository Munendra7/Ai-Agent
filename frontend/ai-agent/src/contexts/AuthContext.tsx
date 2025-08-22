import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { User, AuthContextType } from '../types/auth';
import { authService } from '../services/authService';
import { toast } from 'react-toastify';

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const isAuthenticated = !!user && !!token && !authService.isTokenExpired();

  useEffect(() => {
    initializeAuth();
  }, []);

  const initializeAuth = async () => {
    try {
      const { token: savedToken, user: savedUser } = authService.getAuthData();
      
      if (savedToken && savedUser && !authService.isTokenExpired()) {
        setToken(savedToken);
        setUser(savedUser);
        
        // Verify token with backend
        try {
          const currentUser = await authService.getCurrentUser();
          setUser(currentUser);
        } catch (error) {
          // Token is invalid, clear auth data
          authService.clearAuthData();
          setToken(null);
          setUser(null);
        }
      }
    } catch (error) {
      console.error('Failed to initialize auth:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (provider: 'google' | 'microsoft' | 'github') => {
    try {
      setIsLoading(true);
      let authUrl: string;

      switch (provider) {
        case 'google':
          authUrl = await authService.getGoogleAuthUrl();
          break;
        case 'microsoft':
          authUrl = await authService.getMicrosoftAuthUrl();
          break;
        case 'github':
          authUrl = await authService.getGitHubAuthUrl();
          break;
        default:
          throw new Error('Invalid provider');
      }

      // Store the provider in session storage for the callback
      sessionStorage.setItem('authProvider', provider);
      
      // Redirect to OAuth provider
      window.location.href = authUrl;
    } catch (error) {
      console.error('Login failed:', error);
      toast.error('Failed to initiate login. Please try again.');
      setIsLoading(false);
    }
  };

  const handleOAuthCallback = async (code: string, state: string, provider: string, error?: string) => {
    try {
      setIsLoading(true);

      if (error) {
        toast.error(`Authentication failed: ${error}`);
        return;
      }

      const callbackData = { code, state, provider, error };
      let authResponse;

      switch (provider) {
        case 'google':
          authResponse = await authService.handleGoogleCallback(callbackData);
          break;
        case 'microsoft':
          authResponse = await authService.handleMicrosoftCallback(callbackData);
          break;
        case 'github':
          authResponse = await authService.handleGitHubCallback(callbackData);
          break;
        default:
          throw new Error('Invalid provider');
      }

      // Save auth data
      authService.saveAuthData(authResponse);
      setToken(authResponse.token);
      setUser(authResponse.user);

      toast.success(`Successfully logged in with ${provider}!`);
      
      // Clean up session storage
      sessionStorage.removeItem('authProvider');
      
      // Redirect to dashboard or intended page
      const redirectUrl = sessionStorage.getItem('redirectAfterLogin') || '/dashboard';
      sessionStorage.removeItem('redirectAfterLogin');
      window.location.href = redirectUrl;
      
    } catch (error: any) {
      console.error('OAuth callback failed:', error);
      toast.error(error.response?.data?.message || 'Authentication failed. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async () => {
    try {
      setIsLoading(true);
      await authService.logout();
      setToken(null);
      setUser(null);
      toast.success('Successfully logged out');
      window.location.href = '/login';
    } catch (error) {
      console.error('Logout failed:', error);
      // Clear local data even if API call fails
      authService.clearAuthData();
      setToken(null);
      setUser(null);
      window.location.href = '/login';
    } finally {
      setIsLoading(false);
    }
  };

  const refreshToken = async () => {
    try {
      const authResponse = await authService.refreshToken();
      authService.saveAuthData(authResponse);
      setToken(authResponse.token);
      setUser(authResponse.user);
    } catch (error) {
      console.error('Token refresh failed:', error);
      logout();
    }
  };

  const value: AuthContextType = {
    user,
    token,
    isAuthenticated,
    isLoading,
    login,
    logout,
    refreshToken,
  };

  // Expose handleOAuthCallback globally for callback pages
  (window as any).handleOAuthCallback = handleOAuthCallback;

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
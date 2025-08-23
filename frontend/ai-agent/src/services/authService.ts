import api from './api';
import { msalInstance } from './msalService';
import { loginRequest } from '../config/msalConfig';
import { AuthenticationResult } from '@azure/msal-browser';

export interface RegisterData {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface LoginData {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

class AuthService {
  async register(data: RegisterData): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/register', data);
    return response.data;
  }

  async login(data: LoginData): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/login', data);
    return response.data;
  }

  async logout(): Promise<void> {
    await api.post('/auth/logout');
  }

  async refreshToken(): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/refresh-token');
    return response.data;
  }

  async googleLogin(code: string, redirectUri: string): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/google', {
      code,
      redirectUri,
    });
    return response.data;
  }

  // UPDATED: Microsoft login using MSAL
  async microsoftLoginWithMSAL(): Promise<AuthenticationResult | null> {
    try {
      // Try popup first
      const response = await msalInstance.loginPopup(loginRequest);
      return response;
    } catch (popupError) {
      console.error("Popup failed, trying redirect:", popupError);
      // Fallback to redirect
      await msalInstance.loginRedirect(loginRequest);
      return null;
    }
  }

  // UPDATED: Exchange MSAL token for your backend JWT
  async exchangeMicrosoftToken(msalToken: string): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/microsoft/token', {
      idToken: msalToken,
    });
    return response.data;
  }

  // Keep Google OAuth URL generation as is
  getGoogleAuthUrl(): string {
    const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;
    const redirectUri = `${window.location.origin}/auth/google/callback`;
    const scope = 'openid email profile';
    
    return `https://accounts.google.com/o/oauth2/v2/auth?` +
      `client_id=${clientId}&` +
      `redirect_uri=${encodeURIComponent(redirectUri)}&` +
      `response_type=code&` +
      `scope=${encodeURIComponent(scope)}&` +
      `access_type=offline&` +
      `prompt=consent`;
  }

  // Get current MSAL account
  getCurrentMsalAccount() {
    return msalInstance.getActiveAccount();
  }

  // Sign out from MSAL
  async msalLogout() {
    const account = msalInstance.getActiveAccount();
    if (account) {
      await msalInstance.logoutPopup({
        account: account,
      });
    }
  }
}

export default new AuthService();
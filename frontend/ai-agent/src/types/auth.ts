export interface User {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  profilePictureUrl?: string;
  provider: string;
  role: string;
  createdAt: string;
  lastLoginAt: string;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface LoginRequest {
  provider: string;
  accessToken: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export type AuthProvider = 'Google' | 'Microsoft' | 'GitHub';

export interface AuthState {
  user: User | null;
  token: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}
export interface User {
  id: string;
  email: string;
  name: string;
  profilePictureUrl?: string;
  provider: 'Google' | 'Microsoft' | 'GitHub';
  createdAt: string;
  lastLoginAt: string;
  roles: string[];
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface AuthContextType {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (provider: 'google' | 'microsoft' | 'github') => Promise<void>;
  logout: () => void;
  refreshToken: () => Promise<void>;
}

export interface OAuthCallbackData {
  code: string;
  state: string;
  provider: string;
  error?: string;
  errorDescription?: string;
}
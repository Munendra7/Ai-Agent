import axios, { AxiosInstance } from 'axios';
import { AuthResponse, User, OAuthCallbackData } from '../types/auth';

const API_BASE_URL = process.env.VITE_API_BASE_URL || 'https://localhost:7036/api';

class AuthService {
  private api: AxiosInstance;

  constructor() {
    this.api = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Add request interceptor to include auth token
    this.api.interceptors.request.use((config) => {
      const token = localStorage.getItem('authToken');
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    });

    // Add response interceptor to handle token expiration
    this.api.interceptors.response.use(
      (response) => response,
      async (error) => {
        if (error.response?.status === 401) {
          // Token expired, clear local storage
          this.clearAuthData();
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  async getGoogleAuthUrl(): Promise<string> {
    const response = await this.api.get('/auth/google/login');
    return response.data.authUrl;
  }

  async getMicrosoftAuthUrl(): Promise<string> {
    const response = await this.api.get('/auth/microsoft/login');
    return response.data.authUrl;
  }

  async getGitHubAuthUrl(): Promise<string> {
    const response = await this.api.get('/auth/github/login');
    return response.data.authUrl;
  }

  async handleGoogleCallback(callbackData: OAuthCallbackData): Promise<AuthResponse> {
    const response = await this.api.post('/auth/google/callback', callbackData);
    return response.data;
  }

  async handleMicrosoftCallback(callbackData: OAuthCallbackData): Promise<AuthResponse> {
    const response = await this.api.post('/auth/microsoft/callback', callbackData);
    return response.data;
  }

  async handleGitHubCallback(callbackData: OAuthCallbackData): Promise<AuthResponse> {
    const response = await this.api.post('/auth/github/callback', callbackData);
    return response.data;
  }

  async getCurrentUser(): Promise<User> {
    const response = await this.api.get('/auth/me');
    return response.data;
  }

  async refreshToken(): Promise<AuthResponse> {
    const response = await this.api.post('/auth/refresh');
    return response.data;
  }

  async logout(): Promise<void> {
    await this.api.post('/auth/logout');
    this.clearAuthData();
  }

  saveAuthData(authResponse: AuthResponse): void {
    localStorage.setItem('authToken', authResponse.token);
    localStorage.setItem('user', JSON.stringify(authResponse.user));
    localStorage.setItem('tokenExpiry', authResponse.expiresAt);
  }

  getAuthData(): { token: string | null; user: User | null } {
    const token = localStorage.getItem('authToken');
    const userStr = localStorage.getItem('user');
    const user = userStr ? JSON.parse(userStr) : null;
    return { token, user };
  }

  clearAuthData(): void {
    localStorage.removeItem('authToken');
    localStorage.removeItem('user');
    localStorage.removeItem('tokenExpiry');
  }

  isTokenExpired(): boolean {
    const expiry = localStorage.getItem('tokenExpiry');
    if (!expiry) return true;
    return new Date() >= new Date(expiry);
  }

  hasRole(role: string): boolean {
    const { user } = this.getAuthData();
    return user?.roles?.includes(role) || false;
  }

  hasAnyRole(roles: string[]): boolean {
    const { user } = this.getAuthData();
    return roles.some(role => user?.roles?.includes(role)) || false;
  }
}

export const authService = new AuthService();
export default authService;
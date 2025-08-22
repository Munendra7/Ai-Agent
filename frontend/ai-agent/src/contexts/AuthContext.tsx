import React, { createContext, useContext, useReducer, useEffect, ReactNode } from 'react';
import { AuthState, User, AuthProvider } from '../types/auth';
import authService from '../services/authService';

// Action types
type AuthAction =
  | { type: 'LOGIN_START' }
  | { type: 'LOGIN_SUCCESS'; payload: { user: User; token: string; refreshToken: string } }
  | { type: 'LOGIN_FAILURE'; payload: string }
  | { type: 'LOGOUT' }
  | { type: 'CLEAR_ERROR' }
  | { type: 'SET_LOADING'; payload: boolean };

// Initial state
const initialState: AuthState = {
  user: null,
  token: null,
  refreshToken: null,
  isAuthenticated: false,
  isLoading: true,
  error: null,
};

// Reducer
function authReducer(state: AuthState, action: AuthAction): AuthState {
  switch (action.type) {
    case 'LOGIN_START':
      return {
        ...state,
        isLoading: true,
        error: null,
      };
    case 'LOGIN_SUCCESS':
      return {
        ...state,
        user: action.payload.user,
        token: action.payload.token,
        refreshToken: action.payload.refreshToken,
        isAuthenticated: true,
        isLoading: false,
        error: null,
      };
    case 'LOGIN_FAILURE':
      return {
        ...state,
        user: null,
        token: null,
        refreshToken: null,
        isAuthenticated: false,
        isLoading: false,
        error: action.payload,
      };
    case 'LOGOUT':
      return {
        ...state,
        user: null,
        token: null,
        refreshToken: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
      };
    case 'CLEAR_ERROR':
      return {
        ...state,
        error: null,
      };
    case 'SET_LOADING':
      return {
        ...state,
        isLoading: action.payload,
      };
    default:
      return state;
  }
}

// Context interface
interface AuthContextType extends AuthState {
  login: (provider: AuthProvider, accessToken: string) => Promise<void>;
  logout: () => Promise<void>;
  clearError: () => void;
  checkAuth: () => Promise<void>;
}

// Create context
const AuthContext = createContext<AuthContextType | undefined>(undefined);

// Provider component
interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [state, dispatch] = useReducer(authReducer, initialState);

  // Check authentication status on mount
  useEffect(() => {
    checkAuth();
  }, []);

  const checkAuth = async () => {
    try {
      const token = authService.getStoredToken();
      const user = authService.getStoredUser();

      if (token && user && !authService.isTokenExpired()) {
        // Try to get fresh user data
        try {
          const currentUser = await authService.getCurrentUser();
          dispatch({
            type: 'LOGIN_SUCCESS',
            payload: {
              user: currentUser,
              token,
              refreshToken: authService.getStoredRefreshToken() || '',
            },
          });
        } catch (error) {
          // Token might be invalid, try refresh
          const refreshToken = authService.getStoredRefreshToken();
          if (refreshToken) {
            try {
              const authResponse = await authService.refreshToken(refreshToken);
              authService.setStoredAuth(authResponse);
              dispatch({
                type: 'LOGIN_SUCCESS',
                payload: {
                  user: authResponse.user,
                  token: authResponse.token,
                  refreshToken: authResponse.refreshToken,
                },
              });
            } catch (refreshError) {
              // Refresh failed, logout
              authService.clearStoredAuth();
              dispatch({ type: 'LOGOUT' });
            }
          } else {
            authService.clearStoredAuth();
            dispatch({ type: 'LOGOUT' });
          }
        }
      } else {
        authService.clearStoredAuth();
        dispatch({ type: 'LOGOUT' });
      }
    } catch (error) {
      console.error('Auth check error:', error);
      dispatch({ type: 'LOGOUT' });
    }
  };

  const login = async (provider: AuthProvider, accessToken: string) => {
    try {
      dispatch({ type: 'LOGIN_START' });
      
      const authResponse = await authService.login(provider, accessToken);
      authService.setStoredAuth(authResponse);
      
      dispatch({
        type: 'LOGIN_SUCCESS',
        payload: {
          user: authResponse.user,
          token: authResponse.token,
          refreshToken: authResponse.refreshToken,
        },
      });
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 'Login failed';
      dispatch({ type: 'LOGIN_FAILURE', payload: errorMessage });
      throw error;
    }
  };

  const logout = async () => {
    try {
      await authService.logout();
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      dispatch({ type: 'LOGOUT' });
    }
  };

  const clearError = () => {
    dispatch({ type: 'CLEAR_ERROR' });
  };

  const value: AuthContextType = {
    ...state,
    login,
    logout,
    clearError,
    checkAuth,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

// Hook to use auth context
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
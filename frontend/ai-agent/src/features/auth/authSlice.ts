import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import authService, { User, LoginData, RegisterData } from '../../services/authService';

interface AuthState {
  user: User | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

const initialState: AuthState = {
  user: null,
  accessToken: localStorage.getItem('accessToken'),
  isAuthenticated: false,
  isLoading: false,
  error: null,
};

export const register = createAsyncThunk(
  'auth/register',
  async (data: RegisterData) => {
    const response = await authService.register(data);
    localStorage.setItem('accessToken', response.accessToken);
    return response;
  }
);

export const login = createAsyncThunk(
  'auth/login',
  async (data: LoginData) => {
    const response = await authService.login(data);
    localStorage.setItem('accessToken', response.accessToken);
    return response;
  }
);

export const logoutUser = createAsyncThunk(
  'auth/logout',
  async () => {
    await authService.logout();
    await authService.msalLogout(); // Also logout from MSAL if logged in
    localStorage.removeItem('accessToken');
  }
);

export const googleLogin = createAsyncThunk(
  'auth/googleLogin',
  async ({ code, redirectUri }: { code: string; redirectUri: string }) => {
    const response = await authService.googleLogin(code, redirectUri);
    localStorage.setItem('accessToken', response.accessToken);
    return response;
  }
);

// NEW: Microsoft login with MSAL
export const microsoftLoginMSAL = createAsyncThunk(
  'auth/microsoftLoginMSAL',
  async () => {
    const msalResponse = await authService.microsoftLoginWithMSAL();
    if (!msalResponse) {
      throw new Error('MSAL login failed');
    }
    
    // Exchange MSAL token for your backend JWT
    const response = await authService.exchangeMicrosoftToken(msalResponse.idToken);
    localStorage.setItem('accessToken', response.accessToken);
    return response;
  }
);

export const setUserProfile = createAsyncThunk(
  'auth/setUserProfile',
  async () => {
    const user = await authService.getUserProfile();
    return user;
  }
);

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    setCredentials: (state, action: PayloadAction<{ accessToken: string; user: User }>) => {
      state.accessToken = action.payload.accessToken;
      state.user = action.payload.user;
      state.isAuthenticated = true;
      localStorage.setItem('accessToken', action.payload.accessToken);
    },
    logout: (state) => {
      state.user = null;
      state.accessToken = null;
      state.isAuthenticated = false;
      localStorage.removeItem('accessToken');
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      // Register
      .addCase(register.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(register.fulfilled, (state, action) => {
        state.isLoading = false;
        state.isAuthenticated = true;
        state.user = action.payload.user;
        state.accessToken = action.payload.accessToken;
      })
      .addCase(register.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || 'Registration failed';
      })
      // Login
      .addCase(login.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(login.fulfilled, (state, action) => {
        state.isLoading = false;
        state.isAuthenticated = true;
        state.user = action.payload.user;
        state.accessToken = action.payload.accessToken;
      })
      .addCase(login.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || 'Login failed';
      })
      // Logout
      .addCase(logoutUser.fulfilled, (state) => {
        state.user = null;
        state.accessToken = null;
        state.isAuthenticated = false;
      })
      // Google Login
      .addCase(googleLogin.fulfilled, (state, action) => {
        state.isLoading = false;
        state.isAuthenticated = true;
        state.user = action.payload.user;
        state.accessToken = action.payload.accessToken;
      })
      // Microsoft Login with MSAL
      .addCase(microsoftLoginMSAL.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(microsoftLoginMSAL.fulfilled, (state, action) => {
        state.isLoading = false;
        state.isAuthenticated = true;
        state.user = action.payload.user;
        state.accessToken = action.payload.accessToken;
      })
      .addCase(microsoftLoginMSAL.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message || 'Microsoft login failed';
      })
      .addCase(setUserProfile.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(setUserProfile.fulfilled, (state, action) => {
        state.isLoading = false;
        state.user = action.payload;
        state.isAuthenticated = true;
      })
      .addCase(setUserProfile.rejected, (state) => {
        state.isLoading = false;
      });
  },
});

export const { setCredentials, logout, clearError } = authSlice.actions;
export default authSlice.reducer;
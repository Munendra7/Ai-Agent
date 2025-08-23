import axios from 'axios';
import { store } from '../app/store';
import { logout, setCredentials } from '../features/auth/authSlice';

const API_URL = import.meta.env.VITE_AIAgent_URL;

const api = axios.create({
  baseURL: API_URL+'/api',
  withCredentials: true,
});

// Request interceptor
api.interceptors.request.use(
  (config) => {
    const token = store.getState().auth.accessToken;
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const response = await api.post('/auth/refresh-token');
        const { accessToken, user } = response.data;
        
        store.dispatch(setCredentials({ accessToken, user }));
        originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        
        return api(originalRequest);
      } catch (refreshError) {
        store.dispatch(logout());
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);

export default api;
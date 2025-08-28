import axios from 'axios';

const API_URL = import.meta.env.VITE_AIAgent_URL;

const api = axios.create({
  baseURL: API_URL+'/api',
  withCredentials: true,
});


// Interceptor setup function to inject getToken and dispatch
export function setupApiInterceptors(accesstoken:string | null,dispatch: (action: { type: string; payload?: unknown }) => void) {
  api.interceptors.request.use(
    (config) => {
      const token = accesstoken;
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    },
    (error) => Promise.reject(error)
  );

  api.interceptors.response.use(
    (response) => response,
    async (error) => {
      const originalRequest = error.config;

      if (error.response?.status === 401 && !originalRequest._retry) {
        originalRequest._retry = true;

        try {
          const response = await api.post('/auth/refresh-token');
          const { accessToken, user } = response.data;
          dispatch({ type: 'auth/setCredentials', payload: { accessToken, user } });
          originalRequest.headers.Authorization = `Bearer ${accessToken}`;
          return api(originalRequest);
        } catch (refreshError) {
          dispatch({ type: 'auth/logout' });    
          return Promise.reject(refreshError);
        }
      }
      return Promise.reject(error);
    }
  );
}

export default api;
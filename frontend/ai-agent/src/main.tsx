import { createRoot } from 'react-dom/client'
import './index.css'
import {App} from './App.tsx'
import { createBrowserRouter, createRoutesFromElements, Route, RouterProvider } from 'react-router-dom';
import NotFoundPage from '../pages/NotFoundPage.tsx';
import HomePage from '../pages/HomePage.tsx';
import AgentPage from '../pages/AgentPage.tsx'
import { ToastContainer } from 'react-toastify'
import 'react-toastify/dist/ReactToastify.css'
import ErrorPage from '../pages/ErrorPage.tsx'
import { AuthProvider } from './contexts/AuthContext.tsx';
import ProtectedRoute from './components/auth/ProtectedRoute.tsx';
import Login from './components/Login.tsx';
import Dashboard from './components/Dashboard.tsx';
import OAuthCallback from './components/auth/OAuthCallback.tsx';

const router  = createBrowserRouter(
  createRoutesFromElements(
    <Route path="/" element={<App />} errorElement={<ErrorPage />}>
      {/* Public Routes */}
      <Route path="/" element={<HomePage />} />
      <Route path="/login" element={<Login />} />
      
      {/* OAuth Callback Routes */}
      <Route path="/auth/google/callback" element={<OAuthCallback />} />
      <Route path="/auth/microsoft/callback" element={<OAuthCallback />} />
      <Route path="/auth/github/callback" element={<OAuthCallback />} />
      
      {/* Protected Routes */}
      <Route path="/dashboard" element={
        <ProtectedRoute>
          <Dashboard />
        </ProtectedRoute>
      } />
      
      <Route path="/chat" element={
        <ProtectedRoute>
          <AgentPage />
        </ProtectedRoute>
      } />
      
      {/* Admin Only Routes */}
      <Route path="/admin" element={
        <ProtectedRoute requiredRoles={['Admin']}>
          <div className="p-8">
            <h1 className="text-2xl font-bold">Admin Panel</h1>
            <p>This is an admin-only page.</p>
          </div>
        </ProtectedRoute>
      } />
      
      <Route path="*" element={<NotFoundPage />} />
    </Route>
  )
)

createRoot(document.getElementById('root')!).render(
  <AuthProvider>
    <ToastContainer 
      position="top-right" 
      autoClose={3000}
      hideProgressBar={false}
      newestOnTop={false}
      closeOnClick
      rtl={false}
      pauseOnFocusLoss
      draggable
      pauseOnHover
      theme="light"
    />
    <RouterProvider router={router} />
  </AuthProvider>,
)

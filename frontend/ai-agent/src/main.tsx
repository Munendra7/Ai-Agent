import { createRoot } from 'react-dom/client'
import './index.css'
import {App} from './App.tsx'
import { createBrowserRouter, createRoutesFromElements, Route, RouterProvider } from 'react-router-dom';
import NotFoundPage from '../pages/NotFoundPage.tsx';
import HomePage from '../pages/HomePage.tsx';
import PublicRoutes from '../utils/PublicRoutes.tsx';
import PrivateRoutes from '../utils/PrivateRoutes.tsx';
import AgentPage from '../pages/AgentPage.tsx'
import LoginPage from './components/LoginPage.tsx'
import OAuthRedirect from './components/OAuthRedirect.tsx'
import { ToastContainer } from 'react-toastify'
import ErrorPage from '../pages/ErrorPage.tsx'
import { AuthProvider } from './contexts/AuthContext'

const router  = createBrowserRouter(
  createRoutesFromElements(
    <Route path="/" element={<App />} errorElement={<ErrorPage />}>
      <Route path="/" element={<PublicRoutes />} >
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/oauth-redirect" element={<OAuthRedirect />} />
      </Route>
      <Route path="/chat" element={<PrivateRoutes />}>
        <Route path="/chat" element={<AgentPage />} />
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Route>
  )
)

createRoot(document.getElementById('root')!).render(
  <AuthProvider>
    <ToastContainer position="top-right" autoClose={3000} />
    <RouterProvider router={router} />
  </AuthProvider>,
)

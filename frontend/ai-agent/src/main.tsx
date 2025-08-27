import { createRoot } from 'react-dom/client'
import './index.css'
import {App} from './App.tsx';
import { createBrowserRouter, createRoutesFromElements, Route, RouterProvider } from 'react-router-dom';
import NotFoundPage from '../pages/NotFoundPage.tsx';
import HomePage from '../pages/HomePage.tsx';
import PublicRoutes from '../utils/PublicRoutes.tsx';
import ProtectedRoute from '../utils/ProtectedRoute.tsx';
import { ToastContainer } from 'react-toastify'
import ErrorPage from '../pages/ErrorPage.tsx'
import { store } from './app/store.ts';
import { Provider } from 'react-redux';
import Login from './components/Login.tsx';
import OAuthCallback from './components/OAuthCallback.tsx';
import ChatPlayground from './components/ChatPlayground.tsx';

const router  = createBrowserRouter(
  createRoutesFromElements(
    <Route path="/" element={<App />} errorElement={<ErrorPage />}>
      <Route path="/" element={<PublicRoutes />} >
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<Login />} />
        <Route path="/auth/:provider/callback" element={<OAuthCallback />} />
      </Route>
      <Route path="/chat" element={<ProtectedRoute />}>
        <Route path="/chat" element={<ChatPlayground />} />
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Route>
  )
)

createRoot(document.getElementById('root')!).render(
  <Provider store={store}>
    <ToastContainer position="top-right" autoClose={3000} />
    <RouterProvider router={router} />
  </Provider>,
)

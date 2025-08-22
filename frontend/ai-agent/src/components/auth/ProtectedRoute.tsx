import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { authService } from '../../services/authService';
import { Loader2, Lock } from 'lucide-react';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRoles?: string[];
  requireAuth?: boolean;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ 
  children, 
  requiredRoles = [], 
  requireAuth = true 
}) => {
  const { isAuthenticated, isLoading, user } = useAuth();
  const location = useLocation();

  // Show loading spinner while checking authentication
  if (isLoading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50 flex items-center justify-center">
        <div className="text-center">
          <Loader2 className="h-12 w-12 animate-spin text-blue-500 mx-auto mb-4" />
          <p className="text-gray-600">Loading...</p>
        </div>
      </div>
    );
  }

  // If authentication is required but user is not authenticated
  if (requireAuth && !isAuthenticated) {
    // Store the intended destination for redirect after login
    sessionStorage.setItem('redirectAfterLogin', location.pathname + location.search);
    return <Navigate to="/login" replace />;
  }

  // If specific roles are required, check if user has them
  if (requiredRoles.length > 0 && user) {
    const hasRequiredRole = authService.hasAnyRole(requiredRoles);
    
    if (!hasRequiredRole) {
      return (
        <div className="min-h-screen bg-gradient-to-br from-red-50 via-white to-orange-50 flex items-center justify-center">
          <div className="text-center max-w-md">
            <div className="mb-8">
              <Lock className="h-16 w-16 text-red-500 mx-auto" />
            </div>
            <h2 className="text-2xl font-semibold text-gray-900 mb-4">
              Access Denied
            </h2>
            <p className="text-gray-600 mb-6">
              You don't have the required permissions to access this page.
            </p>
            <div className="bg-white rounded-lg p-4 shadow-md">
              <p className="text-sm text-gray-500 mb-2">Required roles:</p>
              <div className="flex flex-wrap gap-2 justify-center">
                {requiredRoles.map((role) => (
                  <span 
                    key={role}
                    className="px-3 py-1 bg-red-100 text-red-800 rounded-full text-sm font-medium"
                  >
                    {role}
                  </span>
                ))}
              </div>
              {user.roles.length > 0 && (
                <>
                  <p className="text-sm text-gray-500 mb-2 mt-4">Your roles:</p>
                  <div className="flex flex-wrap gap-2 justify-center">
                    {user.roles.map((role) => (
                      <span 
                        key={role}
                        className="px-3 py-1 bg-blue-100 text-blue-800 rounded-full text-sm font-medium"
                      >
                        {role}
                      </span>
                    ))}
                  </div>
                </>
              )}
            </div>
            <button
              onClick={() => window.history.back()}
              className="mt-6 px-6 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
            >
              Go Back
            </button>
          </div>
        </div>
      );
    }
  }

  // User is authenticated and has required roles, render the protected content
  return <>{children}</>;
};

export default ProtectedRoute;
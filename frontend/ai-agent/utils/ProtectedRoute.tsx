import { Navigate, Outlet } from 'react-router-dom';
import React from 'react';
import SideNavPanel from "../src/components/SideNavPanel";
import { useAppSelector } from '../src/app/hooks';

interface ProtectedRouteProps {
  allowedRoles?: string[];
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ allowedRoles }) => {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && user) {
    // const hasRequiredRole = user.roles.some((role) => allowedRoles.includes(role));
    // if (!hasRequiredRole) {
    //   return <Navigate to="/unauthorized" replace />;
    // }
  }

  return (<>
    <SideNavPanel />
    <Outlet />
    </>);
};

export default ProtectedRoute;
import { useIsAuthenticated } from "@azure/msal-react";
import { Navigate, Outlet } from "react-router-dom";

const PublicRoutes = () => {
  const isAuthenticated = useIsAuthenticated();
  return isAuthenticated ? 
  (
    <Navigate to="/chat" />) 
    : 
    <Outlet />;
};

export default PublicRoutes;
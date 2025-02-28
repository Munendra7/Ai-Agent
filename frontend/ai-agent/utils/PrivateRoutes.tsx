import { useIsAuthenticated } from "@azure/msal-react";
import { Navigate, Outlet } from "react-router-dom";

const PrivateRoutes = () => {
  const isAuthenticated = useIsAuthenticated();
  return isAuthenticated ? 
    (<>
        <Outlet />
    </>) : 
    <Navigate to="/" />;
};

export default PrivateRoutes;
import { useIsAuthenticated } from "@azure/msal-react";
import { Navigate, Outlet } from "react-router-dom";
import SideNavPanel from "../src/components/SideNavPanel";

const PrivateRoutes = () => {
  const isAuthenticated = useIsAuthenticated();
  return isAuthenticated ? 
    (<>
        <SideNavPanel />
        <Outlet />
    </>) : 
    <Navigate to="/" />;
};

export default PrivateRoutes;
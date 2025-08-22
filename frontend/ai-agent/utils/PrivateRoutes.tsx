import { Navigate, Outlet } from "react-router-dom";
import SideNavPanel from "../src/components/SideNavPanel";
import { useAuth } from "../src/contexts/AuthContext";

const PrivateRoutes = () => {
  const { isAuthenticated, isLoading } = useAuth();
  
  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    );
  }
  
  return isAuthenticated ? 
    (<>
        <SideNavPanel />
        <Outlet />
    </>) : 
    <Navigate to="/login" />;
};

export default PrivateRoutes;
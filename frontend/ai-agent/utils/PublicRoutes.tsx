import { Navigate, Outlet } from "react-router-dom";
import { useAppSelector } from "../src/app/hooks";

const PublicRoutes = () => {
  const isAuthenticated = useAppSelector(state=> state.auth.isAuthenticated);
  return isAuthenticated ? 
  (
    <Navigate to="/chat" />) 
    : 
    <Outlet />;
};

export default PublicRoutes;
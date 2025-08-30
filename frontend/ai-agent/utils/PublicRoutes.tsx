import { Navigate, Outlet, useParams } from "react-router-dom";
import { useAppSelector } from "../src/app/hooks";

const PublicRoutes = () => {
  const isAuthenticated = useAppSelector(state=> state.auth.isAuthenticated);
  const sessionId = useParams<{ sessionid: string }>().sessionid;
  return isAuthenticated ? 
  (
    <Navigate to={"/chat/"+ sessionId} />) 
    : 
    <Outlet />;
};

export default PublicRoutes;
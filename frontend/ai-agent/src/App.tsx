import React, { useEffect, useState } from "react";
import { Outlet } from "react-router-dom";
import NavBar from "./components/NavBar";
import { initializeMsal } from "./services/msalService";
import { setUserProfile } from "./features/auth/authSlice";
import { useAppDispatch, useAppSelector } from "./app/hooks";
import { setupApiInterceptors } from "./services/api";
import { setupFetchInterceptors } from "./services/fetchClient";

export const App: React.FC = () => {
  const [msalInitialized, setMsalInitialized] = useState(false);
  const dispatch = useAppDispatch();
  const accessToken = useAppSelector(state => state.auth.accessToken);

  useEffect(() => {
    const init = async () => {
      await initializeMsal();
      setMsalInitialized(true);
    };
    init();    
  }, []);

  useEffect(() => { 
    setupApiInterceptors(accessToken, dispatch);
    setupFetchInterceptors(accessToken, dispatch);
    dispatch(setUserProfile()).unwrap();
  }, [accessToken, dispatch]);

  if (!msalInitialized) {
    return <div>Loading...</div>;
  }

  return (
    <div>
      <NavBar/>
      <Outlet />
    </div>
  );
}
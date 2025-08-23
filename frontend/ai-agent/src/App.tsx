import React, { useEffect, useState } from "react";
import { Outlet } from "react-router-dom";
import NavBar from "./components/NavBar";
import { initializeMsal } from "./services/msalService";

export const App: React.FC = () => {
  const [msalInitialized, setMsalInitialized] = useState(false);

  useEffect(() => {
    const init = async () => {
      await initializeMsal();
      setMsalInitialized(true);
    };
    init();
  }, []);

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
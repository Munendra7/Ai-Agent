import React from "react";
import { useMsal } from "@azure/msal-react";
import { loginRequest } from "../../authConfig";

export const SignInButton: React.FC = () => {
  const { instance } = useMsal();

  const handleLogin = () => {
    instance.loginPopup(loginRequest).catch((e) => console.log(e));
  };

  return (
    <button
      onClick={handleLogin}
      className="px-6 py-2 bg-blue-600 text-white rounded-lg shadow-lg hover:bg-blue-700 transition duration-300"
      >
      Sign In
    </button>
  );
};
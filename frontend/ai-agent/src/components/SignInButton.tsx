import React from "react";
import { useNavigate } from "react-router-dom";

const SignInButton: React.FC = () => {
  const navigate = useNavigate();

  const handleSignIn = () => {
    navigate('/login');
  };

  return (
    <button onClick={handleSignIn} className="px-4 py-2 rounded-lg shadow">
      {"Sign In"}
    </button>
  );
};

export default SignInButton;
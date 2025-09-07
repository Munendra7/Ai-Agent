import React from "react";
import { useNavigate } from "react-router-dom";

interface SignInButtonProps {
  buttonText?: string;
}

const SignInButton: React.FC<SignInButtonProps> = ({ buttonText }) => {
  const navigate = useNavigate();

  const handleSignIn = () => {
    navigate("/login");
  };

  return (
    <button
      onClick={handleSignIn}
      className="cursor-pointer px-5 py-2 rounded-xl bg-gradient-to-r from-blue-500 to-indigo-600 text-white font-medium shadow-md hover:shadow-lg hover:from-blue-600 hover:to-indigo-700 transition-all duration-300"
    >
      {buttonText ?? "Sign In"}
    </button>
  );
};

export default SignInButton;
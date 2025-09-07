import React from "react";
import { logoutUser } from "../features/auth/authSlice"; 
import { useAppDispatch } from "../app/hooks";
import { useNavigate } from "react-router-dom";

const SignOutButton: React.FC = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const handleSignOut = async () => {
    await dispatch(logoutUser()).unwrap();
    navigate("/");
  };

  return (
    <button
      onClick={handleSignOut}
      className="cursor-pointer px-5 py-2 rounded-xl bg-gradient-to-r from-red-500 to-pink-600 text-white font-medium shadow-md hover:shadow-lg hover:from-red-600 hover:to-pink-700 transition-all duration-300"
    >
      Sign Out
    </button>
  );
};

export default SignOutButton;
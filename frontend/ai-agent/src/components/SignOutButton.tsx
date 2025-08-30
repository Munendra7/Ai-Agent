import React from "react";
import { logoutUser } from "../features/auth/authSlice"; // adjust path
import { useAppDispatch } from "../app/hooks";
import { useNavigate } from "react-router-dom";

const SignOutButton: React.FC = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const handleSignOut = async () => {
    await dispatch(logoutUser()).unwrap();
    navigate('/');
  };

  return (
    <button onClick={handleSignOut} className="px-4 py-2 rounded-lg shadow">
      {"Sign Out"}
    </button>
  );
};

export default SignOutButton;

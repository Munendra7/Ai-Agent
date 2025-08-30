import React, { useState } from "react";
import Copilot from "../assets/AIAgent.svg";
import { Menu, X } from "lucide-react";
import SignOutButton from "./SignOutButton";
import SignInButton from "./SignInButton";
import { useAppSelector } from "../app/hooks";
import { useLocation, useNavigate } from "react-router-dom";

const NavBar: React.FC = () => {
  const [isOpen, setIsOpen] = useState(false);
  const {user, isAuthenticated} = useAppSelector(state => state.auth);
  const navigate = useNavigate();

  const location = useLocation();

  const hideSignIn = location.pathname === "/login" || location.pathname === "/register";

  return (
    <nav className="fixed top-0 left-0 w-full bg-black/80 backdrop-blur-lg text-white z-50 border-b border-gray-700 shadow-md">
      <div className="flex items-center justify-between px-6 py-3">
        {/* Logo */}
        <div className="flex items-center cursor-pointer" onClick={()=>{navigate("/")}}>
          <img src={Copilot} alt="AI Agent Logo" className="h-10 w-10 mr-2" />
          <h1 className="text-xl font-bold text-white tracking-wide">
            AI Agent
          </h1>
        </div>

        {/* Mobile Menu Toggle */}
        <button
          className="sm:hidden text-white focus:outline-none"
          onClick={() => setIsOpen(!isOpen)}
        >
          {isOpen ? <X size={26} /> : <Menu size={26} />}
        </button>

        {/* Desktop Nav */}
        <div className="hidden sm:flex items-center gap-4">
          {isAuthenticated ? (
            <>
              <span className="text-sm text-gray-300">
                Welcome, {user?.firstName}
              </span>
              <SignOutButton/>
            </>
          ) : (
            !hideSignIn && <SignInButton />
          )}
        </div>
      </div>

      {/* Mobile Menu Dropdown */}
      {isOpen && (
        <div className="sm:hidden flex flex-col items-center bg-gray-900 text-white py-4 border-t border-gray-700">
          {isAuthenticated ? (
            <>
              <span className="text-sm mb-2 text-gray-300">
                Welcome, {user?.firstName}
              </span>
              <SignOutButton/>
            </>
          ) : (
            !hideSignIn && <SignInButton />
          )}
        </div>
      )}
    </nav>
  );
};

export default NavBar;
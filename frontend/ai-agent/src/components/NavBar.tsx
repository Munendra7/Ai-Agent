import React, { useState } from "react";
import { SignOutButton } from "./MSAuthentication/SignOutButton";
import Copilot from "../assets/AIAgent.svg";
import { Menu, X } from "lucide-react"; // For mobile menu icons
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { SignInButton } from "./MSAuthentication/SignInButton";

const NavBar: React.FC<{}> = () => {
  const [isOpen, setIsOpen] = useState(false);
  const isAuthenticated = useIsAuthenticated();
  const{ instance } = useMsal();
  const activeAccount = instance.getActiveAccount();

  return (
    <nav className="fixed top-0 left-0 w-full bg-gray-900 text-white shadow-lg z-50">
      <div className="flex items-center justify-between px-6 py-4">
        {/* Logo Section */}
        <div className="flex items-center">
          <img src={Copilot} alt="AI Agent Logo" className="h-10 w-10 mr-2" />
          <h1 className="text-xl font-bold">AI Agent</h1>
        </div>

        {/* Mobile Menu Toggle */}
        <button
          className="sm:hidden text-white focus:outline-none"
          onClick={() => setIsOpen(!isOpen)}
        >
          {isOpen ? <X size={28} /> : <Menu size={28} />}
        </button>

        {/* Desktop Navigation */}
        <div className="hidden sm:flex items-center gap-4">
          {isAuthenticated?<><span className="text-sm md:text-base">Welcome, {activeAccount?.name}</span>
          <SignOutButton /></>:<><SignInButton/></>}
        </div>
      </div>

      {/* Mobile Dropdown Menu */}
      {isOpen && (
        <div className="sm:hidden flex flex-col items-center bg-gray-800 py-4">
          {isAuthenticated?<><span className="text-sm mb-2">Welcome, {activeAccount?.name}</span>
          <SignOutButton /></>:<><SignInButton/></>}
        </div>
      )}
    </nav>
  );
};

export default NavBar;
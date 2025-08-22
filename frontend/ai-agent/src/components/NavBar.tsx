import React, { useState } from "react";
import Copilot from "../assets/AIAgent.svg";
import { Menu, X, User, LogOut, Settings, Home } from "lucide-react";
import { useAuth } from "../contexts/AuthContext";
import { Link, useLocation } from "react-router-dom";

const NavBar: React.FC = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const { user, isAuthenticated, logout } = useAuth();
  const location = useLocation();

  const handleLogout = () => {
    logout();
    setIsUserMenuOpen(false);
    setIsOpen(false);
  };

  const isActive = (path: string) => location.pathname === path;

  return (
    <nav className="fixed top-0 left-0 w-full bg-black/80 backdrop-blur-lg text-white z-50 border-b border-gray-700 shadow-md">
      <div className="flex items-center justify-between px-6 py-3">
        {/* Logo */}
        <Link to="/" className="flex items-center hover:opacity-80 transition-opacity">
          <img src={Copilot} alt="AI Agent Logo" className="h-10 w-10 mr-2" />
          <h1 className="text-xl font-bold text-white tracking-wide">
            AI Agent
          </h1>
        </Link>

        {/* Desktop Navigation */}
        <div className="hidden sm:flex items-center gap-6">
          {isAuthenticated && (
            <>
              <Link 
                to="/dashboard" 
                className={`flex items-center gap-2 px-3 py-2 rounded-lg transition-colors ${
                  isActive('/dashboard') ? 'bg-blue-600' : 'hover:bg-gray-700'
                }`}
              >
                <Home size={18} />
                Dashboard
              </Link>
              
              <Link 
                to="/chat" 
                className={`flex items-center gap-2 px-3 py-2 rounded-lg transition-colors ${
                  isActive('/chat') ? 'bg-blue-600' : 'hover:bg-gray-700'
                }`}
              >
                Chat
              </Link>

              {user?.roles.includes('Admin') && (
                <Link 
                  to="/admin" 
                  className={`flex items-center gap-2 px-3 py-2 rounded-lg transition-colors ${
                    isActive('/admin') ? 'bg-purple-600' : 'hover:bg-gray-700'
                  }`}
                >
                  <Settings size={18} />
                  Admin
                </Link>
              )}
            </>
          )}

          {/* User Menu or Login */}
          {isAuthenticated ? (
            <div className="relative">
              <button
                onClick={() => setIsUserMenuOpen(!isUserMenuOpen)}
                className="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-gray-700 transition-colors"
              >
                {user?.profilePictureUrl ? (
                  <img
                    src={user.profilePictureUrl}
                    alt={user.name}
                    className="w-6 h-6 rounded-full object-cover"
                  />
                ) : (
                  <div className="w-6 h-6 rounded-full bg-blue-500 flex items-center justify-center">
                    <User size={14} />
                  </div>
                )}
                <span className="text-sm">{user?.name}</span>
              </button>

              {/* User Dropdown Menu */}
              {isUserMenuOpen && (
                <div className="absolute right-0 mt-2 w-48 bg-gray-800 rounded-lg shadow-lg border border-gray-700">
                  <div className="p-3 border-b border-gray-700">
                    <p className="text-sm font-medium">{user?.name}</p>
                    <p className="text-xs text-gray-400">{user?.email}</p>
                    <div className="flex flex-wrap gap-1 mt-2">
                      {user?.roles.map((role) => (
                        <span 
                          key={role}
                          className="px-2 py-1 bg-blue-600 text-xs rounded-full"
                        >
                          {role}
                        </span>
                      ))}
                    </div>
                  </div>
                  
                  <button
                    onClick={handleLogout}
                    className="w-full flex items-center gap-2 px-3 py-2 text-left hover:bg-gray-700 transition-colors text-red-400"
                  >
                    <LogOut size={16} />
                    Sign Out
                  </button>
                </div>
              )}
            </div>
          ) : (
            <Link
              to="/login"
              className="px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors font-medium"
            >
              Sign In
            </Link>
          )}
        </div>

        {/* Mobile Menu Toggle */}
        <button
          className="sm:hidden text-white focus:outline-none"
          onClick={() => setIsOpen(!isOpen)}
        >
          {isOpen ? <X size={26} /> : <Menu size={26} />}
        </button>
      </div>

      {/* Mobile Menu Dropdown */}
      {isOpen && (
        <div className="sm:hidden flex flex-col bg-gray-900 text-white py-4 border-t border-gray-700">
          {isAuthenticated ? (
            <>
              <div className="px-6 py-3 border-b border-gray-700">
                <div className="flex items-center gap-3">
                  {user?.profilePictureUrl ? (
                    <img
                      src={user.profilePictureUrl}
                      alt={user.name}
                      className="w-8 h-8 rounded-full object-cover"
                    />
                  ) : (
                    <div className="w-8 h-8 rounded-full bg-blue-500 flex items-center justify-center">
                      <User size={16} />
                    </div>
                  )}
                  <div>
                    <p className="text-sm font-medium">{user?.name}</p>
                    <p className="text-xs text-gray-400">{user?.email}</p>
                  </div>
                </div>
                <div className="flex flex-wrap gap-1 mt-2">
                  {user?.roles.map((role) => (
                    <span 
                      key={role}
                      className="px-2 py-1 bg-blue-600 text-xs rounded-full"
                    >
                      {role}
                    </span>
                  ))}
                </div>
              </div>

              <Link 
                to="/dashboard" 
                className="flex items-center gap-3 px-6 py-3 hover:bg-gray-700 transition-colors"
                onClick={() => setIsOpen(false)}
              >
                <Home size={18} />
                Dashboard
              </Link>
              
              <Link 
                to="/chat" 
                className="flex items-center gap-3 px-6 py-3 hover:bg-gray-700 transition-colors"
                onClick={() => setIsOpen(false)}
              >
                Chat
              </Link>

              {user?.roles.includes('Admin') && (
                <Link 
                  to="/admin" 
                  className="flex items-center gap-3 px-6 py-3 hover:bg-gray-700 transition-colors"
                  onClick={() => setIsOpen(false)}
                >
                  <Settings size={18} />
                  Admin
                </Link>
              )}

              <button
                onClick={handleLogout}
                className="flex items-center gap-3 px-6 py-3 hover:bg-gray-700 transition-colors text-red-400"
              >
                <LogOut size={18} />
                Sign Out
              </button>
            </>
          ) : (
            <Link
              to="/login"
              className="mx-6 px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors font-medium text-center"
              onClick={() => setIsOpen(false)}
            >
              Sign In
            </Link>
          )}
        </div>
      )}

      {/* Click outside to close user menu */}
      {isUserMenuOpen && (
        <div 
          className="fixed inset-0 z-40"
          onClick={() => setIsUserMenuOpen(false)}
        />
      )}
    </nav>
  );
};

export default NavBar;
import React, { useState } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { useNavigate } from 'react-router-dom';
import { 
  User as UserIcon, 
  LogOut, 
  Settings, 
  ChevronDown,
  Shield,
  Mail,
  Calendar
} from 'lucide-react';
import { toast } from 'react-toastify';

const UserProfile: React.FC = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [isDropdownOpen, setIsDropdownOpen] = useState(false);

  const handleLogout = async () => {
    try {
      await logout();
      toast.success('Successfully logged out');
      navigate('/');
    } catch (error) {
      toast.error('Failed to logout');
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  };

  const getProviderIcon = (provider: string) => {
    switch (provider.toLowerCase()) {
      case 'google':
        return '🔵';
      case 'microsoft':
        return '🟢';
      case 'github':
        return '⚫';
      default:
        return '👤';
    }
  };

  const getRoleBadge = (role: string) => {
    const roleConfig = {
      admin: { color: 'bg-red-100 text-red-800', icon: '👑' },
      moderator: { color: 'bg-yellow-100 text-yellow-800', icon: '🛡️' },
      user: { color: 'bg-blue-100 text-blue-800', icon: '👤' }
    };

    const config = roleConfig[role.toLowerCase() as keyof typeof roleConfig] || roleConfig.user;

    return (
      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${config.color}`}>
        <span className="mr-1">{config.icon}</span>
        {role.charAt(0).toUpperCase() + role.slice(1)}
      </span>
    );
  };

  if (!user) {
    return null;
  }

  return (
    <div className="relative">
      {/* User Button */}
      <button
        onClick={() => setIsDropdownOpen(!isDropdownOpen)}
        className="flex items-center space-x-3 p-2 rounded-lg hover:bg-gray-100 transition-colors duration-200"
      >
        <div className="relative">
          {user.profilePictureUrl ? (
            <img
              src={user.profilePictureUrl}
              alt={user.firstName || user.email}
              className="w-8 h-8 rounded-full object-cover"
            />
          ) : (
            <div className="w-8 h-8 bg-gradient-to-r from-blue-500 to-purple-500 rounded-full flex items-center justify-center">
              <UserIcon className="w-4 h-4 text-white" />
            </div>
          )}
          <div className="absolute -bottom-1 -right-1 w-3 h-3 bg-green-400 border-2 border-white rounded-full"></div>
        </div>
        
        <div className="hidden md:block text-left">
          <p className="text-sm font-medium text-gray-900 truncate max-w-32">
            {user.firstName ? `${user.firstName} ${user.lastName || ''}`.trim() : user.email}
          </p>
          <p className="text-xs text-gray-500 truncate max-w-32">
            {getProviderIcon(user.provider)} {user.provider}
          </p>
        </div>
        
        <ChevronDown className={`w-4 h-4 text-gray-500 transition-transform duration-200 ${isDropdownOpen ? 'rotate-180' : ''}`} />
      </button>

      {/* Dropdown Menu */}
      {isDropdownOpen && (
        <div className="absolute right-0 mt-2 w-80 bg-white rounded-lg shadow-lg border border-gray-200 z-50">
          {/* User Info Section */}
          <div className="p-4 border-b border-gray-200">
            <div className="flex items-center space-x-3">
              {user.profilePictureUrl ? (
                <img
                  src={user.profilePictureUrl}
                  alt={user.firstName || user.email}
                  className="w-12 h-12 rounded-full object-cover"
                />
              ) : (
                <div className="w-12 h-12 bg-gradient-to-r from-blue-500 to-purple-500 rounded-full flex items-center justify-center">
                  <UserIcon className="w-6 h-6 text-white" />
                </div>
              )}
              
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold text-gray-900 truncate">
                  {user.firstName ? `${user.firstName} ${user.lastName || ''}`.trim() : 'User'}
                </p>
                <p className="text-sm text-gray-500 truncate">{user.email}</p>
                <div className="mt-1">
                  {getRoleBadge(user.role)}
                </div>
              </div>
            </div>
          </div>

          {/* User Details */}
          <div className="p-4 space-y-3">
            <div className="flex items-center space-x-3 text-sm">
              <Mail className="w-4 h-4 text-gray-400" />
              <span className="text-gray-600">{user.email}</span>
            </div>
            
            <div className="flex items-center space-x-3 text-sm">
              <Shield className="w-4 h-4 text-gray-400" />
              <span className="text-gray-600">
                {getProviderIcon(user.provider)} {user.provider} Account
              </span>
            </div>
            
            <div className="flex items-center space-x-3 text-sm">
              <Calendar className="w-4 h-4 text-gray-400" />
              <span className="text-gray-600">
                Member since {formatDate(user.createdAt)}
              </span>
            </div>
            
            <div className="flex items-center space-x-3 text-sm">
              <Calendar className="w-4 h-4 text-gray-400" />
              <span className="text-gray-600">
                Last login {formatDate(user.lastLoginAt)}
              </span>
            </div>
          </div>

          {/* Actions */}
          <div className="p-2 border-t border-gray-200">
            <button
              onClick={() => {
                setIsDropdownOpen(false);
                // Add settings navigation here
              }}
              className="w-full flex items-center space-x-3 px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-md transition-colors duration-200"
            >
              <Settings className="w-4 h-4" />
              <span>Settings</span>
            </button>
            
            <button
              onClick={handleLogout}
              className="w-full flex items-center space-x-3 px-3 py-2 text-sm text-red-600 hover:bg-red-50 rounded-md transition-colors duration-200"
            >
              <LogOut className="w-4 h-4" />
              <span>Sign Out</span>
            </button>
          </div>
        </div>
      )}

      {/* Backdrop */}
      {isDropdownOpen && (
        <div
          className="fixed inset-0 z-40"
          onClick={() => setIsDropdownOpen(false)}
        />
      )}
    </div>
  );
};

export default UserProfile;
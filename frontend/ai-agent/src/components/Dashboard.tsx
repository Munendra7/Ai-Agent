import React from 'react';
import { useAuth } from '../contexts/AuthContext';
import { User, Shield, Calendar, Settings, LogOut } from 'lucide-react';

const Dashboard: React.FC = () => {
  const { user, logout } = useAuth();

  if (!user) return null;

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const getProviderIcon = (provider: string) => {
    switch (provider.toLowerCase()) {
      case 'google':
        return '🔍';
      case 'microsoft':
        return '🟦';
      case 'github':
        return '🐙';
      default:
        return '👤';
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900">Dashboard</h1>
          <p className="text-gray-600 mt-2">Welcome back, {user.name}!</p>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* User Profile Card */}
          <div className="lg:col-span-1">
            <div className="bg-white rounded-xl shadow-sm border p-6">
              <div className="text-center">
                {user.profilePictureUrl ? (
                  <img
                    src={user.profilePictureUrl}
                    alt={user.name}
                    className="w-20 h-20 rounded-full mx-auto mb-4 object-cover"
                  />
                ) : (
                  <div className="w-20 h-20 rounded-full bg-gradient-to-r from-blue-500 to-purple-600 flex items-center justify-center mx-auto mb-4">
                    <User className="w-10 h-10 text-white" />
                  </div>
                )}
                
                <h2 className="text-xl font-semibold text-gray-900 mb-1">
                  {user.name}
                </h2>
                <p className="text-gray-600 mb-4">{user.email}</p>
                
                <div className="flex items-center justify-center space-x-2 mb-4">
                  <span className="text-2xl">{getProviderIcon(user.provider)}</span>
                  <span className="text-sm text-gray-500">
                    Signed in with {user.provider}
                  </span>
                </div>

                {/* Roles */}
                <div className="mb-6">
                  <p className="text-sm text-gray-500 mb-2">Roles</p>
                  <div className="flex flex-wrap gap-2 justify-center">
                    {user.roles.map((role) => (
                      <span 
                        key={role}
                        className="px-3 py-1 bg-blue-100 text-blue-800 rounded-full text-sm font-medium flex items-center"
                      >
                        <Shield className="w-3 h-3 mr-1" />
                        {role}
                      </span>
                    ))}
                  </div>
                </div>

                <button
                  onClick={logout}
                  className="w-full flex items-center justify-center px-4 py-2 bg-red-500 text-white rounded-lg hover:bg-red-600 transition-colors"
                >
                  <LogOut className="w-4 h-4 mr-2" />
                  Sign Out
                </button>
              </div>
            </div>
          </div>

          {/* Main Content */}
          <div className="lg:col-span-2 space-y-6">
            {/* Account Information */}
            <div className="bg-white rounded-xl shadow-sm border p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
                <User className="w-5 h-5 mr-2" />
                Account Information
              </h3>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    User ID
                  </label>
                  <p className="text-sm text-gray-600 bg-gray-50 p-2 rounded font-mono">
                    {user.id}
                  </p>
                </div>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Provider
                  </label>
                  <p className="text-sm text-gray-600 bg-gray-50 p-2 rounded">
                    {user.provider}
                  </p>
                </div>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1 flex items-center">
                    <Calendar className="w-4 h-4 mr-1" />
                    Account Created
                  </label>
                  <p className="text-sm text-gray-600 bg-gray-50 p-2 rounded">
                    {formatDate(user.createdAt)}
                  </p>
                </div>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1 flex items-center">
                    <Calendar className="w-4 h-4 mr-1" />
                    Last Login
                  </label>
                  <p className="text-sm text-gray-600 bg-gray-50 p-2 rounded">
                    {formatDate(user.lastLoginAt)}
                  </p>
                </div>
              </div>
            </div>

            {/* Role-based Content */}
            {user.roles.includes('Admin') && (
              <div className="bg-gradient-to-r from-purple-500 to-pink-500 rounded-xl shadow-sm p-6 text-white">
                <h3 className="text-lg font-semibold mb-4 flex items-center">
                  <Settings className="w-5 h-5 mr-2" />
                  Admin Panel
                </h3>
                <p className="mb-4">
                  You have administrator privileges. You can access advanced features and manage users.
                </p>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="bg-white/20 rounded-lg p-4">
                    <h4 className="font-medium mb-2">User Management</h4>
                    <p className="text-sm opacity-90">Manage user accounts and roles</p>
                  </div>
                  <div className="bg-white/20 rounded-lg p-4">
                    <h4 className="font-medium mb-2">System Settings</h4>
                    <p className="text-sm opacity-90">Configure system-wide settings</p>
                  </div>
                </div>
              </div>
            )}

            {/* Regular User Content */}
            <div className="bg-white rounded-xl shadow-sm border p-6">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">
                Quick Actions
              </h3>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <button className="p-4 border border-gray-200 rounded-lg hover:border-blue-300 hover:bg-blue-50 transition-colors text-left">
                  <h4 className="font-medium text-gray-900 mb-2">AI Chat</h4>
                  <p className="text-sm text-gray-600">Start a conversation with AI</p>
                </button>
                
                <button className="p-4 border border-gray-200 rounded-lg hover:border-blue-300 hover:bg-blue-50 transition-colors text-left">
                  <h4 className="font-medium text-gray-900 mb-2">Settings</h4>
                  <p className="text-sm text-gray-600">Manage your preferences</p>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
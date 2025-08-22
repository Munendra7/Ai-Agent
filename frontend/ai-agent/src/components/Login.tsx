import React, { useState } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { Chrome, Github, User } from 'lucide-react';

const Login: React.FC = () => {
  const { login, isLoading } = useAuth();
  const [loadingProvider, setLoadingProvider] = useState<string | null>(null);

  const handleProviderLogin = async (provider: 'google' | 'microsoft' | 'github') => {
    setLoadingProvider(provider);
    try {
      await login(provider);
    } catch (error) {
      console.error('Login error:', error);
      setLoadingProvider(null);
    }
  };

  const providerConfig = {
    google: {
      name: 'Google',
      icon: Chrome,
      color: 'bg-red-500 hover:bg-red-600',
      textColor: 'text-white'
    },
    microsoft: {
      name: 'Microsoft',
      icon: User,
      color: 'bg-blue-500 hover:bg-blue-600',
      textColor: 'text-white'
    },
    github: {
      name: 'GitHub',
      icon: Github,
      color: 'bg-gray-800 hover:bg-gray-900',
      textColor: 'text-white'
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50 flex items-center justify-center p-4">
      <div className="max-w-md w-full space-y-8">
        {/* Header */}
        <div className="text-center">
          <div className="mx-auto h-16 w-16 bg-gradient-to-r from-blue-500 to-purple-600 rounded-full flex items-center justify-center mb-6">
            <User className="h-8 w-8 text-white" />
          </div>
          <h2 className="text-3xl font-bold text-gray-900 mb-2">
            Welcome Back
          </h2>
          <p className="text-gray-600">
            Sign in to your account using your preferred provider
          </p>
        </div>

        {/* Login Card */}
        <div className="bg-white rounded-2xl shadow-xl p-8 space-y-6">
          <div className="space-y-4">
            {Object.entries(providerConfig).map(([key, config]) => {
              const Icon = config.icon;
              const isProviderLoading = loadingProvider === key;
              
              return (
                <button
                  key={key}
                  onClick={() => handleProviderLogin(key as 'google' | 'microsoft' | 'github')}
                  disabled={isLoading}
                  className={`
                    w-full flex items-center justify-center px-6 py-4 rounded-xl font-medium
                    transition-all duration-200 transform hover:scale-[1.02] active:scale-[0.98]
                    ${config.color} ${config.textColor}
                    disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none
                    shadow-lg hover:shadow-xl
                  `}
                >
                  {isProviderLoading ? (
                    <div className="flex items-center space-x-3">
                      <div className="animate-spin rounded-full h-5 w-5 border-2 border-white border-t-transparent"></div>
                      <span>Connecting to {config.name}...</span>
                    </div>
                  ) : (
                    <div className="flex items-center space-x-3">
                      <Icon className="h-5 w-5" />
                      <span>Continue with {config.name}</span>
                    </div>
                  )}
                </button>
              );
            })}
          </div>

          {/* Divider */}
          <div className="relative">
            <div className="absolute inset-0 flex items-center">
              <div className="w-full border-t border-gray-200"></div>
            </div>
            <div className="relative flex justify-center text-sm">
              <span className="px-4 bg-white text-gray-500">Secure OAuth Authentication</span>
            </div>
          </div>

          {/* Features */}
          <div className="grid grid-cols-2 gap-4 text-sm text-gray-600">
            <div className="flex items-center space-x-2">
              <div className="w-2 h-2 bg-green-400 rounded-full"></div>
              <span>Secure Login</span>
            </div>
            <div className="flex items-center space-x-2">
              <div className="w-2 h-2 bg-blue-400 rounded-full"></div>
              <span>Role-based Access</span>
            </div>
            <div className="flex items-center space-x-2">
              <div className="w-2 h-2 bg-purple-400 rounded-full"></div>
              <span>Single Sign-On</span>
            </div>
            <div className="flex items-center space-x-2">
              <div className="w-2 h-2 bg-orange-400 rounded-full"></div>
              <span>Multi-Provider</span>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="text-center text-sm text-gray-500">
          <p>
            By signing in, you agree to our{' '}
            <a href="#" className="text-blue-500 hover:text-blue-600">
              Terms of Service
            </a>{' '}
            and{' '}
            <a href="#" className="text-blue-500 hover:text-blue-600">
              Privacy Policy
            </a>
          </p>
        </div>
      </div>
    </div>
  );
};

export default Login;
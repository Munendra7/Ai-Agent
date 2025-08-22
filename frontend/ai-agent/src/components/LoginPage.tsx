import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import OAuthService from '../services/oauthService';
import { AuthProvider } from '../types/auth';
import { toast } from 'react-toastify';
import { 
  FcGoogle, 
  FcMicrosoft, 
  FcGithub,
  Eye,
  EyeOff,
  Loader2
} from 'lucide-react';

const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const { login, isLoading, error, clearError } = useAuth();
  const [isAuthenticating, setIsAuthenticating] = useState(false);

  const handleOAuthLogin = async (provider: AuthProvider) => {
    try {
      setIsAuthenticating(true);
      clearError();

      let accessToken: string;

      switch (provider) {
        case 'Google':
          accessToken = await OAuthService.loginWithGoogle();
          break;
        case 'Microsoft':
          accessToken = await OAuthService.loginWithMicrosoft();
          break;
        case 'GitHub':
          accessToken = await OAuthService.loginWithGitHub();
          break;
        default:
          throw new Error('Unsupported provider');
      }

      await login(provider, accessToken);
      toast.success(`Successfully logged in with ${provider}!`);
      navigate('/chat');
    } catch (error: any) {
      console.error(`${provider} login error:`, error);
      toast.error(error.message || `Failed to login with ${provider}`);
    } finally {
      setIsAuthenticating(false);
    }
  };

  const OAuthButton: React.FC<{
    provider: AuthProvider;
    icon: React.ReactNode;
    text: string;
    bgColor: string;
    hoverColor: string;
    textColor: string;
  }> = ({ provider, icon, text, bgColor, hoverColor, textColor }) => (
    <button
      onClick={() => handleOAuthLogin(provider)}
      disabled={isAuthenticating}
      className={`
        w-full flex items-center justify-center gap-3 px-6 py-3 rounded-lg
        font-medium transition-all duration-200 transform hover:scale-105
        disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none
        ${bgColor} ${hoverColor} ${textColor}
        border border-gray-200 hover:border-gray-300
        shadow-sm hover:shadow-md
      `}
    >
      {isAuthenticating ? (
        <Loader2 className="w-5 h-5 animate-spin" />
      ) : (
        icon
      )}
      <span>{isAuthenticating ? 'Authenticating...' : text}</span>
    </button>
  );

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50 flex items-center justify-center p-4">
      <div className="max-w-md w-full space-y-8">
        {/* Header */}
        <div className="text-center">
          <div className="mx-auto h-16 w-16 bg-gradient-to-r from-blue-600 to-purple-600 rounded-full flex items-center justify-center mb-6">
            <svg className="h-8 w-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
            </svg>
          </div>
          <h2 className="text-3xl font-bold text-gray-900 mb-2">
            Welcome Back
          </h2>
          <p className="text-gray-600">
            Sign in to continue to your AI Assistant
          </p>
        </div>

        {/* Error Display */}
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <p className="text-sm text-red-800">{error}</p>
              </div>
            </div>
          </div>
        )}

        {/* OAuth Buttons */}
        <div className="space-y-4">
          <OAuthButton
            provider="Google"
            icon={<FcGoogle className="w-5 h-5" />}
            text="Continue with Google"
            bgColor="bg-white"
            hoverColor="hover:bg-gray-50"
            textColor="text-gray-700"
          />

          <OAuthButton
            provider="Microsoft"
            icon={<FcMicrosoft className="w-5 h-5" />}
            text="Continue with Microsoft"
            bgColor="bg-white"
            hoverColor="hover:bg-gray-50"
            textColor="text-gray-700"
          />

          <OAuthButton
            provider="GitHub"
            icon={<FcGithub className="w-5 h-5" />}
            text="Continue with GitHub"
            bgColor="bg-gray-900"
            hoverColor="hover:bg-gray-800"
            textColor="text-white"
          />
        </div>

        {/* Divider */}
        <div className="relative">
          <div className="absolute inset-0 flex items-center">
            <div className="w-full border-t border-gray-300" />
          </div>
          <div className="relative flex justify-center text-sm">
            <span className="px-2 bg-gradient-to-br from-blue-50 via-white to-purple-50 text-gray-500">
              Secure authentication powered by OAuth 2.0
            </span>
          </div>
        </div>

        {/* Features */}
        <div className="bg-white rounded-lg p-6 shadow-sm border border-gray-200">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">
            Why choose OAuth?
          </h3>
          <div className="space-y-3">
            <div className="flex items-start">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-green-500 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                </svg>
              </div>
              <p className="ml-3 text-sm text-gray-600">
                No passwords to remember or manage
              </p>
            </div>
            <div className="flex items-start">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-green-500 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                </svg>
              </div>
              <p className="ml-3 text-sm text-gray-600">
                Enhanced security with industry standards
              </p>
            </div>
            <div className="flex items-start">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-green-500 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                </svg>
              </div>
              <p className="ml-3 text-sm text-gray-600">
                Quick and seamless login experience
              </p>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="text-center">
          <p className="text-xs text-gray-500">
            By signing in, you agree to our{' '}
            <a href="#" className="text-blue-600 hover:text-blue-500 underline">
              Terms of Service
            </a>{' '}
            and{' '}
            <a href="#" className="text-blue-600 hover:text-blue-500 underline">
              Privacy Policy
            </a>
          </p>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;
import React, { useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { Loader2 } from 'lucide-react';

const OAuthCallback: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  useEffect(() => {
    const handleCallback = async () => {
      const code = searchParams.get('code');
      const state = searchParams.get('state');
      const error = searchParams.get('error');
      const errorDescription = searchParams.get('error_description');
      
      // Get provider from session storage or URL path
      const provider = sessionStorage.getItem('authProvider') || 
                      window.location.pathname.split('/')[2]; // e.g., /auth/google/callback

      if (error) {
        console.error('OAuth error:', error, errorDescription);
        navigate('/login?error=' + encodeURIComponent(error));
        return;
      }

      if (!code || !provider) {
        console.error('Missing code or provider');
        navigate('/login?error=invalid_callback');
        return;
      }

      // Call the global callback handler
      if ((window as any).handleOAuthCallback) {
        await (window as any).handleOAuthCallback(code, state || '', provider, error || undefined);
      } else {
        console.error('OAuth callback handler not found');
        navigate('/login?error=callback_handler_missing');
      }
    };

    handleCallback();
  }, [searchParams, navigate]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50 flex items-center justify-center">
      <div className="text-center">
        <div className="mb-8">
          <Loader2 className="h-12 w-12 animate-spin text-blue-500 mx-auto" />
        </div>
        <h2 className="text-2xl font-semibold text-gray-900 mb-2">
          Completing Sign In...
        </h2>
        <p className="text-gray-600">
          Please wait while we finish setting up your account.
        </p>
      </div>
    </div>
  );
};

export default OAuthCallback;
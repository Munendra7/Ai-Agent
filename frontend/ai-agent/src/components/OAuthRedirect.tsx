import React, { useEffect } from 'react';
import { handleOAuthRedirect } from '../services/oauthService';
import { Loader2 } from 'lucide-react';

const OAuthRedirect: React.FC = () => {
  useEffect(() => {
    handleOAuthRedirect();
  }, []);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 via-white to-purple-50">
      <div className="text-center">
        <Loader2 className="w-12 h-12 text-blue-600 animate-spin mx-auto mb-4" />
        <h2 className="text-xl font-semibold text-gray-900 mb-2">
          Completing Authentication
        </h2>
        <p className="text-gray-600">
          Please wait while we complete your sign-in...
        </p>
      </div>
    </div>
  );
};

export default OAuthRedirect;
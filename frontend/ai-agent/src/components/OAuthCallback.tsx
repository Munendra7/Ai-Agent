import React, { useEffect } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { googleLogin, microsoftLoginMSAL } from '../features/auth/authSlice';
import { useAppDispatch } from '../app/hooks';

const OAuthCallback: React.FC = () => {
  const { provider } = useParams<{ provider: string }>();
  const location = useLocation();
  const navigate = useNavigate();
  const dispatch = useAppDispatch();

  useEffect(() => {
    const urlParams = new URLSearchParams(location.search);
    const code = urlParams.get('code');
    const redirectUri = `${window.location.origin}/auth/${provider}/callback`;

    if (code) {
      if (provider === 'google') {
        dispatch(googleLogin({ code, redirectUri }))
          .unwrap()
          .then(() => navigate('/chat'))
          .catch(() => navigate('/login'));
      } else if (provider === 'microsoft') {
        dispatch(microsoftLoginMSAL())
          .unwrap()
          .then(() => navigate('/chat'))
          .catch(() => navigate('/login'));
      }
    } else {
      navigate('/login');
    }
  }, [provider, location, navigate, dispatch]);

  return (
    <div className="min-h-screen flex items-center justify-center">
      <div className="text-center">
        <h2 className="text-2xl font-semibold mb-2">Authenticating...</h2>
        <p className="text-gray-600">Please wait while we complete your login.</p>
      </div>
    </div>
  );
};

export default OAuthCallback;
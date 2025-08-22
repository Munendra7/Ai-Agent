import { PublicClientApplication, AuthenticationResult } from '@azure/msal-browser';
import { msalInstance, loginRequest } from '../authConfig';

// Google OAuth configuration
const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID;
const GOOGLE_REDIRECT_URI = import.meta.env.VITE_GOOGLE_REDIRECT_URI || window.location.origin;

// GitHub OAuth configuration
const GITHUB_CLIENT_ID = import.meta.env.VITE_GITHUB_CLIENT_ID;
const GITHUB_REDIRECT_URI = import.meta.env.VITE_GITHUB_REDIRECT_URI || window.location.origin;

// Microsoft OAuth configuration (using MSAL)
const MICROSOFT_CLIENT_ID = import.meta.env.VITE_MSAL_ClientId;

export class OAuthService {
  // Google OAuth
  static async loginWithGoogle(): Promise<string> {
    return new Promise((resolve, reject) => {
      if (!GOOGLE_CLIENT_ID) {
        reject(new Error('Google Client ID not configured'));
        return;
      }

      const googleAuthUrl = `https://accounts.google.com/o/oauth2/v2/auth?` +
        `client_id=${GOOGLE_CLIENT_ID}&` +
        `redirect_uri=${encodeURIComponent(GOOGLE_REDIRECT_URI)}&` +
        `response_type=token&` +
        `scope=${encodeURIComponent('openid email profile')}&` +
        `state=${Math.random().toString(36).substring(7)}`;

      // Open popup window
      const popup = window.open(
        googleAuthUrl,
        'googleAuth',
        'width=500,height=600,scrollbars=yes,resizable=yes'
      );

      if (!popup) {
        reject(new Error('Popup blocked by browser'));
        return;
      }

      // Listen for message from popup
      const messageListener = (event: MessageEvent) => {
        if (event.origin !== window.location.origin) return;

        if (event.data.type === 'GOOGLE_AUTH_SUCCESS') {
          window.removeEventListener('message', messageListener);
          popup.close();
          resolve(event.data.accessToken);
        } else if (event.data.type === 'GOOGLE_AUTH_ERROR') {
          window.removeEventListener('message', messageListener);
          popup.close();
          reject(new Error(event.data.error || 'Google authentication failed'));
        }
      };

      window.addEventListener('message', messageListener);

      // Check if popup was closed
      const checkClosed = setInterval(() => {
        if (popup.closed) {
          clearInterval(checkClosed);
          window.removeEventListener('message', messageListener);
          reject(new Error('Authentication cancelled'));
        }
      }, 1000);
    });
  }

  // Microsoft OAuth (using MSAL)
  static async loginWithMicrosoft(): Promise<string> {
    try {
      const result: AuthenticationResult = await msalInstance.acquireTokenSilent(loginRequest);
      return result.accessToken;
    } catch (error) {
      try {
        const result: AuthenticationResult = await msalInstance.acquireTokenPopup(loginRequest);
        return result.accessToken;
      } catch (popupError) {
        throw new Error('Microsoft authentication failed');
      }
    }
  }

  // GitHub OAuth
  static async loginWithGitHub(): Promise<string> {
    return new Promise((resolve, reject) => {
      if (!GITHUB_CLIENT_ID) {
        reject(new Error('GitHub Client ID not configured'));
        return;
      }

      const githubAuthUrl = `https://github.com/login/oauth/authorize?` +
        `client_id=${GITHUB_CLIENT_ID}&` +
        `redirect_uri=${encodeURIComponent(GITHUB_REDIRECT_URI)}&` +
        `scope=${encodeURIComponent('read:user user:email')}&` +
        `state=${Math.random().toString(36).substring(7)}`;

      // Open popup window
      const popup = window.open(
        githubAuthUrl,
        'githubAuth',
        'width=500,height=600,scrollbars=yes,resizable=yes'
      );

      if (!popup) {
        reject(new Error('Popup blocked by browser'));
        return;
      }

      // Listen for message from popup
      const messageListener = (event: MessageEvent) => {
        if (event.origin !== window.location.origin) return;

        if (event.data.type === 'GITHUB_AUTH_SUCCESS') {
          window.removeEventListener('message', messageListener);
          popup.close();
          resolve(event.data.accessToken);
        } else if (event.data.type === 'GITHUB_AUTH_ERROR') {
          window.removeEventListener('message', messageListener);
          popup.close();
          reject(new Error(event.data.error || 'GitHub authentication failed'));
        }
      };

      window.addEventListener('message', messageListener);

      // Check if popup was closed
      const checkClosed = setInterval(() => {
        if (popup.closed) {
          clearInterval(checkClosed);
          window.removeEventListener('message', messageListener);
          reject(new Error('Authentication cancelled'));
        }
      }, 1000);
    });
  }
}

// Helper function to handle OAuth redirects
export const handleOAuthRedirect = () => {
  const urlParams = new URLSearchParams(window.location.search);
  const hashParams = new URLSearchParams(window.location.hash.substring(1));

  // Handle Google OAuth redirect
  if (hashParams.has('access_token')) {
    const accessToken = hashParams.get('access_token');
    const state = hashParams.get('state');
    
    if (accessToken) {
      window.opener?.postMessage({
        type: 'GOOGLE_AUTH_SUCCESS',
        accessToken,
        state
      }, window.location.origin);
      window.close();
    }
  }

  // Handle GitHub OAuth redirect
  if (urlParams.has('code')) {
    const code = urlParams.get('code');
    const state = urlParams.get('state');
    
    if (code) {
      // Exchange code for access token
      exchangeGitHubCode(code, state);
    }
  }
};

// Exchange GitHub authorization code for access token
const exchangeGitHubCode = async (code: string, state: string | null) => {
  try {
    const response = await fetch('/api/github/exchange-code', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ code, state }),
    });

    if (response.ok) {
      const { access_token } = await response.json();
      window.opener?.postMessage({
        type: 'GITHUB_AUTH_SUCCESS',
        accessToken: access_token,
        state
      }, window.location.origin);
    } else {
      window.opener?.postMessage({
        type: 'GITHUB_AUTH_ERROR',
        error: 'Failed to exchange code for token'
      }, window.location.origin);
    }
  } catch (error) {
    window.opener?.postMessage({
      type: 'GITHUB_AUTH_ERROR',
      error: 'Network error'
    }, window.location.origin);
  }
  
  window.close();
};

export default OAuthService;
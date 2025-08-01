import { PublicClientApplication, Configuration, EventType, AuthenticationResult } from "@azure/msal-browser";

export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_MSAL_ClientId,
    authority: import.meta.env.VITE_MSAL_Authority,
    redirectUri: import.meta.env.VITE_MSAL_RedirectUri,
  },
  cache: {
    cacheLocation: "sessionStorage",
    storeAuthStateInCookie: true
  },
};

/**
 * Scopes you add here will be prompted for user consent during sign-in.
 * By default, MSAL.js will add OIDC scopes (openid, profile, email) to any login request.
 * For more information about OIDC scopes, visit: 
 * https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-permissions-and-consent#openid-connect-scopes
 */
export const loginRequest = {
    scopes: ["User.Read"]
};

export const backendAPILoginRequest = {
  scopes: [import.meta.env.VITE_Backend_Scope]
};

/**
 * Add here the scopes to request when obtaining an access token for MS Graph API. For more information, see:
 * https://github.com/AzureAD/microsoft-authentication-library-for-js/blob/dev/lib/msal-browser/docs/resources-and-scopes.md
 */
export const graphConfig = {
    graphMeEndpoint: "https://graph.microsoft.com/v1.0/me",
};

export const msalInstance = new PublicClientApplication(msalConfig);
if(!msalInstance.getActiveAccount() && msalInstance.getAllAccounts().length > 0) {
  msalInstance.setActiveAccount(msalInstance.getAllAccounts()[0]);
}

msalInstance.addEventCallback((event) => {
  if(event.eventType === EventType.LOGIN_SUCCESS && event.payload && (event.payload as AuthenticationResult).account) {
    const account = (event.payload as AuthenticationResult)?.account;
    msalInstance.setActiveAccount(account);
  }
});
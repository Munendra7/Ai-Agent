import { PublicClientApplication, EventType, EventMessage, AuthenticationResult } from "@azure/msal-browser";
import { msalConfig } from "../config/msalConfig";

export const msalInstance = new PublicClientApplication(msalConfig);

// Account selection logic
msalInstance.addEventCallback((event: EventMessage) => {
  if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
    const payload = event.payload as AuthenticationResult;
    const account = payload.account;
    msalInstance.setActiveAccount(account);
  }
});

// Initialize MSAL
export const initializeMsal = async () => {
  await msalInstance.initialize();
  
  // Handle redirect promise
  try {
    const response = await msalInstance.handleRedirectPromise();
    if (response) {
      msalInstance.setActiveAccount(response.account);
      return response;
    }
  } catch (error) {
    console.error("Error handling redirect:", error);
  }
  
  // Set active account if available
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length > 0) {
    msalInstance.setActiveAccount(accounts[0]);
  }
};
import { graphConfig } from "./authConfig";

/**
 * Makes a call to the Microsoft Graph API with the provided access token.
 * @param accessToken - The access token for authorization.
 */
export async function callMsGraph(accessToken: string): Promise<any> {
  const headers = new Headers();
  headers.append("Authorization", `Bearer ${accessToken}`);

  const options = {
    method: "GET",
    headers: headers,
  };

  try {
    const response = await fetch(graphConfig.graphMeEndpoint, options);
    return await response.json();
  } catch (error) {
    console.error("Error calling Microsoft Graph API:", error);
    throw error;
  }
}
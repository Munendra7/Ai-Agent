// fetchClient.ts
const API_URL = import.meta.env.VITE_AIAgent_URL + "/api";

let accessToken: string | null = null;
let dispatchFn: ((action: { type: string; payload?: unknown }) => void) | null = null;

export function setupFetchInterceptors(token: string | null,dispatch: (action: { type: string; payload?: unknown }) => void){
  accessToken = token;
  dispatchFn = dispatch;
}

type FetchOptions = RequestInit & {
  body?: unknown;       // auto-JSON stringified
  stream?: boolean; // if true → return response.body instead of parsed JSON
};

export async function fetchWithInterceptors<T>(url: string, options: FetchOptions = {}): Promise<T> {
  const { headers, body, stream, ...rest } = options;

  const finalHeaders: HeadersInit = {
    "Content-Type": "application/json",
    ...(headers || {}),
  };

  if (accessToken) {
    (finalHeaders as Record<string, string>).Authorization = `Bearer ${accessToken}`;
  }

  const response = await fetch(API_URL + url, {
    ...rest,
    headers: finalHeaders,
    body: body && typeof body !== "string" ? JSON.stringify(body) : body,
    credentials: "include",
  });

  if (response.status === 401 && dispatchFn) {
    try {
      const refreshRes = await fetch(API_URL + "/auth/refresh-token", {
        method: "POST",
        credentials: "include",
      });

      if (!refreshRes.ok) throw new Error("Refresh failed");

      const data = await refreshRes.json();
      accessToken = data.accessToken;

      dispatchFn({
        type: "auth/setCredentials",
        payload: { accessToken: data.accessToken, user: data.user },
      });

      // retry original request with updated token
      return fetchWithInterceptors<T>(url, options);
    } catch (err) {
      dispatchFn({ type: "auth/logout" });
      throw err;
    }
  }

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }
  
  if (stream) {
    return response.body as unknown as T;
  }

  return (await response.json()) as T;
}

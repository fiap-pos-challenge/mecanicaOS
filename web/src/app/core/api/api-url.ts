export function isApiUrl(url: string, origin: string, apiBaseUrl: string): boolean {
  return url.startsWith(`${origin}${apiBaseUrl}/`) || url === `${origin}${apiBaseUrl}`;
}

import { InjectionToken } from '@angular/core';

/**
 * Origin of the Companies API. Empty means same origin: in development the dev server proxies
 * `/api` to the backend (see `proxy.conf.json`).
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '',
});

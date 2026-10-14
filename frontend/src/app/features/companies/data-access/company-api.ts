import { HttpClient, HttpResourceRequest } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE_URL } from '../../../core/http/api-base-url';
import { Company, CompanyQuery, CreateCompanyRequest } from './company.models';

/** Typed client for `/api/companies`. The only place in the feature that knows URLs. */
@Injectable({ providedIn: 'root' })
export class CompanyApi {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}/api/companies`;

  create(request: CreateCompanyRequest): Observable<Company> {
    return this.http.post<Company>(this.url, request);
  }

  getById(id: string): Observable<Company> {
    return this.http.get<Company>(`${this.url}/${encodeURIComponent(id)}`);
  }

  /** Request description for `httpResource`; unset or blank filters are left out of the query string. */
  listRequest(query: CompanyQuery): HttpResourceRequest {
    const params: Record<string, string | number> = {};

    for (const [key, value] of Object.entries(query)) {
      if (typeof value === 'number') {
        params[key] = value;
      } else if (typeof value === 'string' && value.trim() !== '') {
        params[key] = value.trim();
      }
    }

    return { url: this.url, params };
  }
}

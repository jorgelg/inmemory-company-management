import { HttpTestingController, TestRequest } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Company, PagedResponse } from '../app/features/companies/data-access/company.models';

export function makeCompany(overrides: Partial<Company> = {}): Company {
  return {
    id: crypto.randomUUID(),
    name: 'Verus LLC',
    website: 'https://www.verus.com',
    domain: 'verus.com',
    createdAt: '2026-10-14T14:10:58Z',
    ...overrides,
  };
}

export function makePage(items: Company[], page = 1, pageSize = 10): PagedResponse<Company> {
  return {
    items,
    page,
    pageSize,
    totalCount: items.length,
    totalPages: items.length === 0 ? 0 : Math.ceil(items.length / pageSize),
  };
}

/** The single pending `GET /api/companies` list request. */
export function expectListRequest(http: HttpTestingController): TestRequest {
  return http.expectOne((request) => request.method === 'GET' && request.url === '/api/companies');
}

/** Answers every pending list request with the given page. */
export function flushListRequests(http: HttpTestingController, page: PagedResponse<Company>): void {
  for (const request of http.match((r) => r.method === 'GET' && r.url === '/api/companies')) {
    request.flush(page);
  }
}

/**
 * Runs pending effects and lets queued promises continue. Unlike `whenStable()`, it does not wait
 * for in-flight HTTP requests, which `httpResource` counts as pending work.
 */
export async function tick(): Promise<void> {
  TestBed.tick();
  await new Promise((resolve) => setTimeout(resolve));
  TestBed.tick();
}

import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../../../core/http/api-base-url';
import { CompanyApi } from './company-api';
import { Company } from './company.models';

const verus: Company = {
  id: '01a13ac0-863a-71d0-9592-9751dbf2fbd0',
  name: 'Verus LLC',
  website: 'https://www.verus.com',
  domain: 'verus.com',
  createdAt: '2026-10-14T14:10:58Z',
};

describe('CompanyApi', () => {
  let api: CompanyApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(CompanyApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts a new company as JSON', async () => {
    const created = firstValueFrom(
      api.create({ name: 'Verus LLC', website: 'https://www.verus.com' }),
    );

    const request = http.expectOne('/api/companies');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Verus LLC', website: 'https://www.verus.com' });
    request.flush(verus, { status: 201, statusText: 'Created' });

    expect(await created).toEqual(verus);
  });

  it('gets a company by id', async () => {
    const fetched = firstValueFrom(api.getById(verus.id));

    http.expectOne(`/api/companies/${verus.id}`).flush(verus);

    expect(await fetched).toEqual(verus);
  });

  it('builds a list request with only the filters that are set', () => {
    const request = api.listRequest({
      page: 2,
      pageSize: 10,
      q: ' verus ',
      name: '',
      domain: '   ',
    });

    expect(request).toEqual({
      url: '/api/companies',
      params: { page: 2, pageSize: 10, q: 'verus' },
    });
  });
});

describe('CompanyApi with a base URL', () => {
  it('prefixes every request', () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: 'https://api.example.com' },
      ],
    });

    const request = TestBed.inject(CompanyApi).listRequest({ page: 1, pageSize: 10 });

    expect(request.url).toBe('https://api.example.com/api/companies');
  });
});

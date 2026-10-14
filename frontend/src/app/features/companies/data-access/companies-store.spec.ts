import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { expectListRequest, makeCompany, makePage } from '../../../../testing/companies';
import { CompaniesStore } from './companies-store';

describe('CompaniesStore', () => {
  let store: CompaniesStore;
  let http: HttpTestingController;

  const settle = () => TestBed.inject(ApplicationRef).whenStable();

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), CompaniesStore],
    });
    store = TestBed.inject(CompaniesStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the first page on start', async () => {
    TestBed.tick();
    const request = expectListRequest(http);
    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('pageSize')).toBe('10');

    request.flush(makePage([makeCompany()]));
    await settle();

    expect(store.companies().map((c) => c.name)).toEqual(['Verus LLC']);
    expect(store.filtered()).toBe(false);
  });

  it('applies filters from the first page and reloads', async () => {
    TestBed.tick();
    expectListRequest(http).flush(makePage([]));
    store.goToPage(3);
    TestBed.tick();
    expectListRequest(http).flush(makePage([]));

    store.applyFilters({ q: 'verus', sort: 'relevance' });
    TestBed.tick();

    const request = expectListRequest(http);
    expect(request.request.params.get('q')).toBe('verus');
    expect(request.request.params.get('sort')).toBe('relevance');
    expect(request.request.params.get('page')).toBe('1');
    request.flush(makePage([]));
    await settle();
    expect(store.filtered()).toBe(true);
  });

  it('exposes list failures as an API problem', async () => {
    TestBed.tick();
    expectListRequest(http).flush(
      {
        title: 'One or more validation errors occurred.',
        detail: 'Page size must be between 1 and 100.',
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await settle();

    expect(store.error()?.message).toBe('Page size must be between 1 and 100.');
    expect(store.companies()).toEqual([]);
  });

  it('creates a company and refreshes the list', async () => {
    TestBed.tick();
    expectListRequest(http).flush(makePage([]));

    const created = store.create({ name: 'Verus LLC', website: 'https://www.verus.com' });
    const company = makeCompany();
    http
      .expectOne({ method: 'POST', url: '/api/companies' })
      .flush(company, { status: 201, statusText: 'Created' });

    expect(await created).toEqual(company);
    TestBed.tick();
    expectListRequest(http).flush(makePage([company]));
    await settle();
    expect(store.companies()).toEqual([company]);
  });
});

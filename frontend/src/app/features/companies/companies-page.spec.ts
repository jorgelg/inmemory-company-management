import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import {
  expectListRequest,
  flushListRequests,
  makeCompany,
  makePage,
  tick,
} from '../../../testing/companies';
import { CompaniesPage } from './companies-page';

describe('CompaniesPage', () => {
  let fixture: ComponentFixture<CompaniesPage>;
  let http: HttpTestingController;
  let element: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [CompaniesPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(CompaniesPage);
    http = TestBed.inject(HttpTestingController);
    element = fixture.nativeElement as HTMLElement;
    await tick();
    flushListRequests(http, makePage([makeCompany()]));
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  async function search(term: string): Promise<void> {
    const input = element.querySelector<HTMLInputElement>('#search-q')!;
    input.value = term;
    input.dispatchEvent(new Event('input'));
    element.querySelector('form.search')!.dispatchEvent(new Event('submit', { cancelable: true }));
    await tick();
    // Pressing Enter in <input type="search"> also fires a bubbling native "search" event, which
    // arrives after the (async) form submission. It must not reach the page's filter listener.
    input.dispatchEvent(new Event('search', { bubbles: true }));
    await tick();
  }

  it('loads the list with the search term when the search form is submitted', async () => {
    await search('verus');

    const request = expectListRequest(http);
    expect(request.request.params.get('q')).toBe('verus');
    expect(request.request.params.get('page')).toBe('1');
    request.flush(makePage([makeCompany({ relevanceScore: 1 })]));
    await fixture.whenStable();

    expect(element.querySelectorAll('[data-testid="company-row"]')).toHaveLength(1);
    expect(element.textContent).toContain('Relevance');
  });

  it('shows a retryable error when the list cannot be loaded', async () => {
    await search('verus');
    expectListRequest(http).error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown' });
    await fixture.whenStable();

    expect(element.querySelector('[data-testid="list-error"]')?.textContent).toContain(
      'Cannot reach the Companies API',
    );

    element.querySelector<HTMLButtonElement>('[data-testid="list-error"] button')!.click();
    await tick();
    flushListRequests(http, makePage([]));
    await fixture.whenStable();

    expect(element.querySelector('[data-testid="list-error"]')).toBeNull();
  });
});

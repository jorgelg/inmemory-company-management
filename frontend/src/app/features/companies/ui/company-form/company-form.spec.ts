import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { flushListRequests, makeCompany, makePage, tick } from '../../../../../testing/companies';
import { CompaniesStore } from '../../data-access/companies-store';
import { CompanyForm } from './company-form';

describe('CompanyForm', () => {
  let fixture: ComponentFixture<CompanyForm>;
  let http: HttpTestingController;
  let element: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [CompanyForm],
      providers: [provideHttpClient(), provideHttpClientTesting(), CompaniesStore],
    });
    fixture = TestBed.createComponent(CompanyForm);
    http = TestBed.inject(HttpTestingController);
    element = fixture.nativeElement as HTMLElement;
    await tick();
    flushListRequests(http, makePage([]));
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  const text = (testId: string) =>
    element.querySelector(`[data-testid="${testId}"]`)?.textContent?.trim() ?? '';

  async function type(id: string, value: string): Promise<void> {
    const input = element.querySelector<HTMLInputElement>(`#${id}`)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  /** Submits without waiting for the API: the test answers the POST itself. */
  async function submit(): Promise<void> {
    element.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    await tick();
  }

  /** Lets the response settle; a successful create also reloads the list, which is answered here. */
  async function settle(listItems = [makeCompany()]): Promise<void> {
    await tick();
    flushListRequests(http, makePage(listItems));
    await fixture.whenStable();
  }

  function postRequest() {
    return http.expectOne({ method: 'POST', url: '/api/companies' });
  }

  it('shows client-side errors and does not call the API when the form is invalid', async () => {
    await type('company-name', 'A');
    await type('company-website', 'not a url');

    await submit();

    expect(text('name-errors')).toBe('Company name must be at least 3 characters.');
    expect(text('website-errors')).toContain('well-formed absolute URL');
    http.expectNone({ method: 'POST' });
  });

  it('requires both fields', async () => {
    await submit();

    expect(text('name-errors')).toBe('Company name is required.');
    expect(text('website-errors')).toBe('Website URL is required.');
  });

  it('submits a valid company, shows success and clears the form', async () => {
    await type('company-name', 'Verus LLC');
    await type('company-website', 'https://www.verus.com');

    await submit();
    const request = postRequest();
    expect(request.request.body).toEqual({ name: 'Verus LLC', website: 'https://www.verus.com' });
    request.flush(makeCompany(), { status: 201, statusText: 'Created' });
    await settle();

    expect(text('form-success')).toBe('Added Verus LLC (verus.com).');
    expect(element.querySelector<HTMLInputElement>('#company-name')!.value).toBe('');
    expect(text('name-errors')).toBe('');
  });

  it('shows a relevance rejection from the API as a form-level error', async () => {
    await type('company-name', 'Acme');
    await type('company-website', 'https://www.example.com');

    await submit();
    postRequest().flush(
      {
        title: 'One or more validation errors occurred.',
        detail: "Company name 'Acme' does not appear to be related to website 'example.com'.",
        code: 'company.not_relevant',
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await settle();

    expect(text('form-error')).toContain('does not appear to be related');
    expect(text('form-success')).toBe('');
  });

  it('puts a duplicate-website conflict on the website field', async () => {
    await type('company-name', 'Verus');
    await type('company-website', 'https://shop.verus.com');

    await submit();
    postRequest().flush(
      {
        title: 'The resource already exists.',
        detail: "A company with website domain 'verus.com' already exists.",
        code: 'company.duplicate_website',
        errors: { website: ["A company with website domain 'verus.com' already exists."] },
      },
      { status: 409, statusText: 'Conflict' },
    );
    await settle();

    expect(text('website-errors')).toBe(
      "A company with website domain 'verus.com' already exists.",
    );
    expect(text('form-error')).toBe('');
  });

  it('explains when the API cannot be reached', async () => {
    await type('company-name', 'Verus LLC');
    await type('company-website', 'https://www.verus.com');

    await submit();
    postRequest().error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });
    await settle();

    expect(text('form-error')).toContain('Cannot reach the Companies API');
  });
});

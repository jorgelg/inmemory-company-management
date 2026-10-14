import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { flushListRequests, makeCompany, makePage, tick } from '../testing/companies';
import { App } from './app';

describe('App', () => {
  it('renders the registry with the form and the loaded companies', async () => {
    TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const fixture = TestBed.createComponent(App);
    const http = TestBed.inject(HttpTestingController);
    await tick();

    flushListRequests(http, makePage([makeCompany()]));
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('h1')?.textContent).toBe('Company registry');
    expect(element.querySelector('#company-name')).not.toBeNull();
    expect(element.querySelectorAll('[data-testid="company-row"]')).toHaveLength(1);
    http.verify();
  });
});

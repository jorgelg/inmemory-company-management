import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CompanyFilters } from '../../data-access/company.models';
import { CompanySearch } from './company-search';

describe('CompanySearch', () => {
  let fixture: ComponentFixture<CompanySearch>;
  let element: HTMLElement;
  let emitted: CompanyFilters[];

  beforeEach(async () => {
    fixture = TestBed.createComponent(CompanySearch);
    element = fixture.nativeElement as HTMLElement;
    emitted = [];
    fixture.componentInstance.filtersChange.subscribe((filters) => emitted.push(filters));
    await fixture.whenStable();
  });

  async function set(id: string, value: string): Promise<void> {
    const control = element.querySelector<HTMLInputElement | HTMLSelectElement>(`#${id}`)!;
    control.value = value;
    control.dispatchEvent(new Event('input'));
    control.dispatchEvent(new Event('change'));
    await fixture.whenStable();
  }

  async function submit(): Promise<void> {
    element.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    await fixture.whenStable();
  }

  it('emits only the filters that are set, trimmed', async () => {
    await set('search-q', '  verus ');
    await set('search-domain', 'verus.com');

    await submit();

    expect(emitted).toEqual([{ q: 'verus', domain: 'verus.com' }]);
  });

  it('emits the chosen sort order', async () => {
    await set('search-sort', '-createdAt');

    await submit();

    expect(emitted).toEqual([{ sort: '-createdAt' }]);
  });

  it('clears every filter', async () => {
    await set('search-name', 'acme');

    element.querySelector<HTMLButtonElement>('button.secondary')!.click();
    await fixture.whenStable();

    expect(emitted).toEqual([{}]);
    expect(element.querySelector<HTMLInputElement>('#search-name')!.value).toBe('');
  });

  it('labels the default sort as relevance only when there is a search term', async () => {
    const defaultOption = () => element.querySelector('#search-sort option')!.textContent?.trim();
    expect(defaultOption()).toBe('Name (A–Z)');

    await set('search-q', 'verus');

    expect(defaultOption()).toBe('Relevance');
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';

import { makeCompany, makePage } from '../../../../../testing/companies';
import { Company } from '../../data-access/company.models';
import { CompanyList } from './company-list';

describe('CompanyList', () => {
  let fixture: ComponentFixture<CompanyList>;
  let element: HTMLElement;

  beforeEach(() => {
    fixture = TestBed.createComponent(CompanyList);
    element = fixture.nativeElement as HTMLElement;
  });

  async function render(companies: Company[], extra: Record<string, unknown> = {}): Promise<void> {
    fixture.componentRef.setInput('companies', companies);
    fixture.componentRef.setInput('page', makePage(companies));
    for (const [name, value] of Object.entries(extra)) {
      fixture.componentRef.setInput(name, value);
    }
    await fixture.whenStable();
  }

  const headers = () => [...element.querySelectorAll('th')].map((th) => th.textContent?.trim());
  const rows = () => [...element.querySelectorAll('[data-testid="company-row"]')];

  it('renders one row per company with a link to its website', async () => {
    await render([
      makeCompany(),
      makeCompany({ name: 'Acme Rockets', website: 'https://acmerockets.com' }),
    ]);

    expect(rows()).toHaveLength(2);
    expect(headers()).toEqual(['Name', 'Website', 'Domain', 'Added']);
    const link = rows()[1].querySelector('a')!;
    expect(link.getAttribute('href')).toBe('https://acmerockets.com');
    expect(link.getAttribute('rel')).toContain('noopener');
  });

  it.each([
    [1, '1 company'],
    [2, '2 companies'],
  ])('pluralizes the total for %i result(s)', async (count, expected) => {
    await render(Array.from({ length: count }, () => makeCompany()));

    const info = element.querySelector('[data-testid="page-info"]')?.textContent ?? '';
    expect(info.replace(/\s+/g, ' ')).toContain(expected);
  });

  it('shows the relevance column for search results', async () => {
    await render([
      makeCompany({ relevanceScore: 1 }),
      makeCompany({ name: 'Verus Capital', relevanceScore: 0.8 }),
    ]);

    expect(headers()).toContain('Relevance');
    expect(rows()[1].textContent).toContain('80%');
  });

  it('shows an empty state for an empty registry', async () => {
    await render([]);

    expect(element.querySelector('[data-testid="empty-state"]')?.textContent?.trim()).toBe(
      'No companies yet. Add the first one above.',
    );
  });

  it('shows a different empty state when filters match nothing', async () => {
    await render([], { filtered: true });

    expect(element.querySelector('[data-testid="empty-state"]')?.textContent?.trim()).toBe(
      'No companies match your search.',
    );
  });

  it('pages forward and disables Previous on the first page', async () => {
    const pages: number[] = [];
    fixture.componentInstance.pageChange.subscribe((page) => pages.push(page));
    await render([makeCompany()]);
    fixture.componentRef.setInput('page', {
      items: [makeCompany()],
      page: 1,
      pageSize: 1,
      totalCount: 3,
      totalPages: 3,
    });
    await fixture.whenStable();

    const [previous, next] = [...element.querySelectorAll<HTMLButtonElement>('.pager button')];
    expect(previous.disabled).toBe(true);
    next.click();

    expect(pages).toEqual([2]);
    expect(element.querySelector('[data-testid="page-info"]')?.textContent).toContain(
      'Page 1 of 3',
    );
  });
});

import { httpResource } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { toApiProblem } from '../../../core/http/api-problem';
import { CompanyApi } from './company-api';
import {
  Company,
  CompanyFilters,
  CompanyQuery,
  CreateCompanyRequest,
  PagedResponse,
} from './company.models';

export const DEFAULT_PAGE_SIZE = 10;

/**
 * State of the companies page: the current query and the page of results it loads. The list is an
 * `httpResource`, so it reloads by itself whenever {@link query} changes. Provided by the page component.
 */
@Injectable()
export class CompaniesStore {
  private readonly api = inject(CompanyApi);

  readonly query = signal<CompanyQuery>({ page: 1, pageSize: DEFAULT_PAGE_SIZE });

  private readonly result = httpResource<PagedResponse<Company>>(() =>
    this.api.listRequest(this.query()),
  );

  readonly page = computed(() => (this.result.hasValue() ? this.result.value() : undefined));
  readonly companies = computed(() => this.page()?.items ?? []);
  readonly loading = this.result.isLoading;
  readonly error = computed(() => {
    const error = this.result.error();
    return error ? toApiProblem(error) : undefined;
  });
  readonly filtered = computed(() => {
    const { q, name, domain } = this.query();
    return [q, name, domain].some((value) => !!value?.trim());
  });

  /** Replaces the filters and goes back to the first page. */
  applyFilters(filters: CompanyFilters): void {
    this.query.update(({ pageSize }) => ({ ...filters, page: 1, pageSize }));
  }

  goToPage(page: number): void {
    this.query.update((query) => ({ ...query, page }));
  }

  reload(): void {
    this.result.reload();
  }

  /** Creates a company and refreshes the list. Rejects with the `HttpErrorResponse` on failure. */
  async create(request: CreateCompanyRequest): Promise<Company> {
    const company = await firstValueFrom(this.api.create(request));
    this.result.reload();
    return company;
  }
}

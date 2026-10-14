import { DatePipe, PercentPipe } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';

import { Company, PagedResponse } from '../../data-access/company.models';

/** Table of companies with empty state and pager. Purely presentational. */
@Component({
  selector: 'app-company-list',
  imports: [DatePipe, PercentPipe],
  templateUrl: './company-list.html',
  styleUrl: './company-list.css',
})
export class CompanyList {
  readonly companies = input.required<readonly Company[]>();
  readonly page = input<PagedResponse<Company>>();
  readonly loading = input(false);
  /** Whether filters are active, which changes the empty-state message. */
  readonly filtered = input(false);

  readonly pageChange = output<number>();

  protected readonly showRelevance = computed(() =>
    this.companies().some((company) => company.relevanceScore !== undefined),
  );
}

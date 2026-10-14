import { Component, inject } from '@angular/core';

import { CompaniesStore } from './data-access/companies-store';
import { CompanyForm } from './ui/company-form/company-form';
import { CompanyList } from './ui/company-list/company-list';
import { CompanySearch } from './ui/company-search/company-search';

/** Composes the feature: owns the store and wires it to the form, search and list. */
@Component({
  selector: 'app-companies-page',
  imports: [CompanyForm, CompanySearch, CompanyList],
  providers: [CompaniesStore],
  template: `
    <app-company-form />

    <section class="card" aria-labelledby="companies-title">
      <h2 id="companies-title">Companies</h2>

      <app-company-search (filtersChange)="store.applyFilters($event)" />

      @if (store.error(); as error) {
        <div class="banner banner-error" role="alert" data-testid="list-error">
          {{ error.message }}
          <button type="button" class="secondary" (click)="store.reload()">Retry</button>
        </div>
      }

      <app-company-list
        [companies]="store.companies()"
        [page]="store.page()"
        [loading]="store.loading()"
        [filtered]="store.filtered()"
        (pageChange)="store.goToPage($event)"
      />
    </section>
  `,
})
export class CompaniesPage {
  protected readonly store = inject(CompaniesStore);
}

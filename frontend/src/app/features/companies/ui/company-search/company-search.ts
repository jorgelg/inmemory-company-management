import { Component, computed, output, signal } from '@angular/core';
import { form, FormField, FormRoot } from '@angular/forms/signals';

import { CompanyFilters, CompanySort } from '../../data-access/company.models';

interface SearchModel {
  q: string;
  name: string;
  domain: string;
  sort: CompanySort | '';
}

const emptySearch = (): SearchModel => ({ q: '', name: '', domain: '', sort: '' });

/** Search and filter controls. Emits the filters on submit; knows nothing about HTTP. */
@Component({
  selector: 'app-company-search',
  imports: [FormField, FormRoot],
  templateUrl: './company-search.html',
  styleUrl: './company-search.css',
})
export class CompanySearch {
  /** Not named `search`: that is a native DOM event (fired by `<input type="search">` on Enter) and would bubble into the same listener. */
  readonly filtersChange = output<CompanyFilters>();

  protected readonly model = signal<SearchModel>(emptySearch());

  protected readonly searchForm = form(this.model, {
    submission: {
      action: async (field) => {
        this.filtersChange.emit(toFilters(field().value()));
        return undefined;
      },
    },
  });

  /** Relevance ordering only makes sense with a search term (the API rejects it otherwise). */
  protected readonly canSortByRelevance = computed(() => this.model().q.trim() !== '');

  protected clear(): void {
    this.searchForm().reset(emptySearch());
    this.filtersChange.emit({});
  }
}

function toFilters({ q, name, domain, sort }: SearchModel): CompanyFilters {
  const effectiveSort = sort === 'relevance' && q.trim() === '' ? '' : sort;

  return {
    ...(q.trim() && { q: q.trim() }),
    ...(name.trim() && { name: name.trim() }),
    ...(domain.trim() && { domain: domain.trim() }),
    ...(effectiveSort && { sort: effectiveSort }),
  };
}

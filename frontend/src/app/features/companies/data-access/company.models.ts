/** Mirrors `CompanyResponse` from the API. */
export interface Company {
  readonly id: string;
  readonly name: string;
  readonly website: string;
  readonly domain: string;
  readonly createdAt: string;
  /** Present only in `q` search results, in [0, 1]. */
  readonly relevanceScore?: number;
}

/** Mirrors `PagedResponse<T>` from the API. */
export interface PagedResponse<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
}

/** Mirrors `CreateCompanyRequest` from the API. */
export interface CreateCompanyRequest {
  name: string;
  website: string;
}

export type CompanySort = 'name' | '-name' | 'createdAt' | '-createdAt' | 'relevance';

/** The filters a user can set; empty strings mean "not set". */
export interface CompanyFilters {
  readonly q?: string;
  readonly name?: string;
  readonly domain?: string;
  readonly sort?: CompanySort;
}

/** Query string of `GET /api/companies`. */
export interface CompanyQuery extends CompanyFilters {
  readonly page: number;
  readonly pageSize: number;
}

import { HttpErrorResponse } from '@angular/common/http';

/** RFC 9457 problem details as returned by the API. */
export interface ProblemDetails {
  readonly type?: string;
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;
  readonly code?: string;
  readonly traceId?: string;
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}

/** Any failed API call, reduced to what the UI needs to show. */
export interface ApiProblem {
  /** HTTP status; 0 when the API could not be reached. */
  readonly status: number;
  /** One concise, user-facing message. */
  readonly message: string;
  /** Machine-readable error code from the API, e.g. `company.not_relevant`. */
  readonly code?: string;
  /** Field-level messages keyed by JSON field name (`name`, `website`, …). */
  readonly fieldErrors: Readonly<Record<string, readonly string[]>>;
}

export const UNREACHABLE_MESSAGE =
  'Cannot reach the Companies API. Make sure the backend is running on http://localhost:5000.';

const UNEXPECTED_MESSAGE = 'Something went wrong. Please try again.';

/** Parses an error thrown by `HttpClient` (or anything else) into an {@link ApiProblem}. */
export function toApiProblem(error: unknown): ApiProblem {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: -1, message: UNEXPECTED_MESSAGE, fieldErrors: {} };
  }

  if (error.status === 0) {
    return { status: 0, message: UNREACHABLE_MESSAGE, fieldErrors: {} };
  }

  const problem = isProblemDetails(error.error) ? error.error : undefined;

  return {
    status: error.status,
    message: problem?.detail ?? problem?.title ?? `Request failed with status ${error.status}.`,
    code: problem?.code,
    fieldErrors: problem?.errors ?? {},
  };
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null && ('title' in value || 'detail' in value);
}

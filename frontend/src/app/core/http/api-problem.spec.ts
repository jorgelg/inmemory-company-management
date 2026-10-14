import { HttpErrorResponse } from '@angular/common/http';

import { toApiProblem, UNREACHABLE_MESSAGE } from './api-problem';

describe('toApiProblem', () => {
  it('reads detail, code and field errors from a validation problem', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'One or more validation errors occurred.',
        status: 400,
        detail: 'Website URL is required.',
        code: 'request.invalid',
        errors: { website: ['Website URL is required.'] },
      },
    });

    expect(toApiProblem(error)).toEqual({
      status: 400,
      message: 'Website URL is required.',
      code: 'request.invalid',
      fieldErrors: { website: ['Website URL is required.'] },
    });
  });

  it('falls back to the title when there is no detail', () => {
    const error = new HttpErrorResponse({
      status: 404,
      error: { title: 'The resource was not found.' },
    });

    expect(toApiProblem(error).message).toBe('The resource was not found.');
  });

  it('describes an unreachable API', () => {
    const error = new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') });

    expect(toApiProblem(error)).toEqual({
      status: 0,
      message: UNREACHABLE_MESSAGE,
      fieldErrors: {},
    });
  });

  it('handles a non-problem error body', () => {
    const error = new HttpErrorResponse({ status: 502, error: '<html>Bad gateway</html>' });

    expect(toApiProblem(error)).toEqual({
      status: 502,
      message: 'Request failed with status 502.',
      code: undefined,
      fieldErrors: {},
    });
  });

  it('handles errors that are not HTTP errors', () => {
    expect(toApiProblem(new Error('boom')).status).toBe(-1);
  });
});

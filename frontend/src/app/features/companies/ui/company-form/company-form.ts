import { Component, inject, signal } from '@angular/core';
import {
  FieldTree,
  form,
  FormField,
  FormRoot,
  maxLength,
  minLength,
  pattern,
  required,
  schema,
  TreeValidationResult,
  ValidationError,
} from '@angular/forms/signals';

import { ApiProblem, toApiProblem } from '../../../../core/http/api-problem';
import { CompaniesStore } from '../../data-access/companies-store';
import { Company, CreateCompanyRequest } from '../../data-access/company.models';

/** Loose client-side check (scheme + dotted host); the API remains the source of truth. */
const WEBSITE_PATTERN = /^https?:\/\/[^\s/?#]+\.[^\s/?#]+(?:[/?#]\S*)?$/i;

/** Mirrors the API's rules so most mistakes are caught before a round trip. */
const companySchema = schema<CreateCompanyRequest>((company) => {
  required(company.name, { message: 'Company name is required.' });
  minLength(company.name, 3, { message: 'Company name must be at least 3 characters.' });
  maxLength(company.name, 200, { message: 'Company name must be at most 200 characters.' });
  required(company.website, { message: 'Website URL is required.' });
  pattern(company.website, WEBSITE_PATTERN, {
    message: 'Website URL must be a well-formed absolute URL, e.g. https://www.example.com.',
  });
});

const emptyCompany = (): CreateCompanyRequest => ({ name: '', website: '' });

@Component({
  selector: 'app-company-form',
  imports: [FormField, FormRoot],
  templateUrl: './company-form.html',
  styleUrl: './company-form.css',
})
export class CompanyForm {
  private readonly store = inject(CompaniesStore);

  protected readonly model = signal<CreateCompanyRequest>(emptyCompany());
  protected readonly created = signal<Company | undefined>(undefined);

  protected readonly companyForm = form(this.model, companySchema, {
    submission: { action: (field) => this.save(field) },
  });

  private async save(field: FieldTree<CreateCompanyRequest>): Promise<TreeValidationResult> {
    this.created.set(undefined);

    try {
      const company = await this.store.create(field().value());
      this.created.set(company);
      field().reset(emptyCompany());
      return undefined;
    } catch (error) {
      return toFormErrors(toApiProblem(error), field);
    }
  }
}

/**
 * Puts the API's field errors on the matching fields; anything else (e.g. "name not relevant to
 * the website", a network failure) becomes a form-level error.
 */
function toFormErrors(
  problem: ApiProblem,
  field: FieldTree<CreateCompanyRequest>,
): ValidationError.WithOptionalFieldTree[] {
  const fields: Record<string, FieldTree<string>> = { name: field.name, website: field.website };
  const errors: ValidationError.WithOptionalFieldTree[] = [];

  for (const [key, messages] of Object.entries(problem.fieldErrors)) {
    for (const message of messages) {
      errors.push({ kind: 'server', message, fieldTree: fields[key] ?? field });
    }
  }

  return errors.length > 0
    ? errors
    : [{ kind: 'server', message: problem.message, fieldTree: field }];
}

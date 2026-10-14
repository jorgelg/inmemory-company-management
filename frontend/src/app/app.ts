import { Component } from '@angular/core';

import { CompaniesPage } from './features/companies/companies-page';

@Component({
  selector: 'app-root',
  imports: [CompaniesPage],
  template: `
    <main class="container">
      <header>
        <h1>Company registry</h1>
        <p class="muted">
          Register a company with its website. The API checks that the name is relevant to the
          website before storing it.
        </p>
      </header>
      <app-companies-page />
    </main>
  `,
})
export class App {}

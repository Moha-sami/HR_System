import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-docs-home',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="min-h-screen bg-neutral-50">
      <div class="max-w-4xl mx-auto px-8 py-12">
        <h1 class="text-3xl font-bold text-neutral-900 mb-2">Component Documentation</h1>
        <p class="text-neutral-500 mb-8">Dev-only reference for shared components.</p>

        <div class="grid gap-4">
          @for (component of components; track component.name) {
            <a
              [routerLink]="component.route"
              class="block bg-white rounded-lg p-6 border border-neutral-200 hover:border-primary-400 hover:shadow-md transition-all"
            >
              <div class="flex items-center justify-between">
                <div>
                  <h2 class="text-lg font-semibold text-neutral-800">{{ component.name }}</h2>
                  <p class="text-sm text-neutral-500 mt-1">{{ component.description }}</p>
                </div>
                <span class="text-neutral-400">→</span>
              </div>
              <div class="flex gap-2 mt-3">
                @for (tag of component.tags; track tag) {
                  <span class="text-xs bg-primary-50 text-primary-700 px-2 py-1 rounded-full">{{
                    tag
                  }}</span>
                }
              </div>
            </a>
          }
        </div>

        <div class="mt-12 p-4 bg-warning-50 border border-warning-200 rounded-lg">
          <p class="text-sm text-warning-800">
            <strong>⚠ Dev Only</strong> — This page is only available in development mode (<code
              >ng serve</code
            >). It will not be accessible in production builds.
          </p>
        </div>
      </div>
    </div>
  `,
})
export class DocsHomeComponent {
  components = [
    {
      name: 'Button',
      route: 'button',
      description: 'Reusable button with variants, sizes, shapes, icons, and loading state.',
      tags: ['primary', 'secondary', 'danger', 'ghost', 'icons'],
    },
    {
      name: 'Table',
      route: 'table',
      description:
        'Data table with variants, sizes, sorting, alignment, and custom cell templates.',
      tags: ['data', 'sorting', 'templates', 'grid'],
    },
    // Add more components here as they are documented
    // {
    //   name: 'Input',
    //   route: 'input',
    //   description: 'Form input with validation states, labels, and icons.',
    //   tags: ['text', 'password', 'error', 'disabled'],
    // },
  ];
}

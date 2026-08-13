import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';

import { LanguageService } from './language.service';

describe('LanguageService', () => {
  const translateService = { use: (language: string) => of(language) };

  let service: LanguageService;
  let document: Document;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [{ provide: TranslateService, useValue: translateService }],
    });

    service = TestBed.inject(LanguageService);
    document = TestBed.inject(DOCUMENT);
  });

  it('defaults to English when no language is persisted', () => {
    service.initialize().subscribe();

    expect(service.currentLanguage()).toBe('en');
    expect(document.documentElement.lang).toBe('en');
  });

  it('uses a persisted supported language at startup', () => {
    localStorage.setItem('buy2-language', 'ar');

    service.initialize().subscribe();

    expect(service.currentLanguage()).toBe('ar');
    expect(document.documentElement.dir).toBe('rtl');
  });

  it('changes the language and persists the selection', () => {
    service.changeLanguage('ar').subscribe();

    expect(service.currentLanguage()).toBe('ar');
    expect(localStorage.getItem('buy2-language')).toBe('ar');
  });

  it('sets document direction for Arabic and English', () => {
    service.changeLanguage('ar').subscribe();
    expect(document.documentElement.lang).toBe('ar');
    expect(document.documentElement.dir).toBe('rtl');

    service.changeLanguage('en').subscribe();
    expect(document.documentElement.lang).toBe('en');
    expect(document.documentElement.dir).toBe('ltr');
  });
});

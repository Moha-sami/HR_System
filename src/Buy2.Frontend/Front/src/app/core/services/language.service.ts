import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';

export type AppLanguage = 'en' | 'ar';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);
  private readonly document = inject(DOCUMENT);
  private readonly storageKey = 'buy2-language';

  readonly supportedLanguages: readonly AppLanguage[] = ['en', 'ar'];
  readonly currentLanguage = signal<AppLanguage>('en');

  initialize(): Observable<unknown> {
    return this.changeLanguage(this.getPersistedLanguage());
  }

  changeLanguage(language: AppLanguage): Observable<unknown> {
    this.currentLanguage.set(language);
    this.persistLanguage(language);
    this.updateDocumentLanguage(language);

    return this.translate.use(language);
  }

  private getPersistedLanguage(): AppLanguage {
    try {
      const language = localStorage.getItem(this.storageKey);
      return this.isSupportedLanguage(language) ? language : 'en';
    } catch {
      return 'en';
    }
  }

  private persistLanguage(language: AppLanguage): void {
    try {
      localStorage.setItem(this.storageKey, language);
    } catch {
      // Language switching remains available when browser storage is unavailable.
    }
  }

  private updateDocumentLanguage(language: AppLanguage): void {
    this.document.documentElement.lang = language;
    this.document.documentElement.dir = language === 'ar' ? 'rtl' : 'ltr';
  }

  private isSupportedLanguage(language: string | null): language is AppLanguage {
    return this.supportedLanguages.includes(language as AppLanguage);
  }
}

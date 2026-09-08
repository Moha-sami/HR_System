import { Component, inject, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '../../../../core/services/language.service';
import type { Recognition } from '../../models/recognition.models';
import { displayDate } from '../../utils/recognition.utils';

@Component({
  selector: 'app-recognition-content', standalone: true, imports: [TranslatePipe],
  templateUrl: './recognition-content.component.html', styleUrl: '../../recognitions.css',
})
export class RecognitionContentComponent {
  readonly recognition = input.required<Recognition>();
  readonly employeeName = input('');
  readonly language = inject(LanguageService).currentLanguage;
  date(value: string | null, time = false) { return displayDate(value, this.language(), time); }
  points() { const value = this.recognition().points; return value === null ? '—' : new Intl.NumberFormat(this.language()).format(value); }
  initials() { return this.employeeName().split(' ').filter(Boolean).slice(0, 2).map(v => v[0]).join(''); }
  image() { return this.recognition().attachmentUrl?.startsWith('data:image/') ?? false; }
}

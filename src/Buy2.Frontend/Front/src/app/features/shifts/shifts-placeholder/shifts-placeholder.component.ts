import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * Temporary shell for scheduling areas without pages yet.
 * Each route supplies its title via route data; ticket #326+ replaces
 * the shift-templates mapping with the real list page.
 */
@Component({
  selector: 'app-shifts-placeholder',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './shifts-placeholder.component.html',
  styleUrls: ['./shifts-placeholder.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShiftsPlaceholderComponent {
  titleKey = 'SCHEDULING.COMING_SOON';

  constructor(route: ActivatedRoute) {
    const dataKey = route.snapshot.data['titleKey'];
    if (typeof dataKey === 'string' && dataKey.length > 0) {
      this.titleKey = dataKey;
    }
  }
}

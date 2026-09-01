import { Component, OnInit, inject, input } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { RewardDetailsContext } from '../../services/reward-details.context';

@Component({
  selector: 'app-reward-details',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './reward-details.component.html',
  styleUrl: './reward-details.component.css',
})
export class RewardDetailsComponent implements OnInit {
  readonly id = input<string>();
  readonly ctx = inject(RewardDetailsContext);

  ngOnInit(): void {
    const rewardId = this.id();
    if (rewardId) {
      this.ctx.load(rewardId);
    }
  }
}

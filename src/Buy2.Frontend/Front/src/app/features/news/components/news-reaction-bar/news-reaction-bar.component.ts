import { Component, input, output } from '@angular/core';
import {
  NEWS_REACTIONS,
  type NewsReaction,
  type NewsReactionCounts,
} from '../../models/news.models';
import { formatLikeCount } from '../../utils/news.utils';

@Component({
  selector: 'app-news-reaction-bar',
  standalone: true,
  templateUrl: './news-reaction-bar.component.html',
  styleUrl: './news-reaction-bar.component.css',
})
export class NewsReactionBarComponent {
  readonly counts = input.required<NewsReactionCounts>();
  readonly myReaction = input<NewsReaction | null>(null);
  readonly picked = output<NewsReaction>();

  readonly reactions = NEWS_REACTIONS;

  countLabel(key: NewsReaction): string {
    return formatLikeCount(this.counts()?.[key] ?? 0);
  }

  select(event: Event, key: NewsReaction): void {
    event.stopPropagation();
    this.picked.emit(key);
  }
}

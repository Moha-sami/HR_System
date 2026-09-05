import { Component, computed, inject, signal, type OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FormsModule } from '@angular/forms';
import type { NewsPost, NewsPostStatus, NewsReaction } from '../../models/news.models';
import { toggleReaction } from '../../models/news.models';
import { NewsService } from '../../services/news.service';
import { formatLikeCount, formatNewsDateTime, isImageAttachment } from '../../utils/news.utils';
import { NewsReactionBarComponent } from '../news-reaction-bar/news-reaction-bar.component';

@Component({
  selector: 'app-news-list',
  standalone: true,
  imports: [TranslatePipe, FormsModule, NewsReactionBarComponent],
  templateUrl: './news-list.component.html',
  styleUrl: './news-list.component.css',
})
export class NewsListComponent implements OnInit {
  private readonly newsService = inject(NewsService);
  private readonly router = inject(Router);

  readonly posts = signal<NewsPost[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly searchQuery = signal('');
  readonly selectedStatus = signal<NewsPostStatus | ''>('');

  readonly filteredPosts = computed(() => {
    const query = this.searchQuery().trim().toLowerCase();
    const status = this.selectedStatus();
    return this.posts().filter((post) => {
      const matchesQuery = !query || post.title.toLowerCase().includes(query);
      const matchesStatus = !status || post.status === status;
      return matchesQuery && matchesStatus;
    });
  });

  ngOnInit(): void {
    this.loadPosts();
  }

  loadPosts(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.newsService.getPosts().subscribe({
      next: (posts) => {
        this.posts.set(posts);
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set(true);
        this.loading.set(false);
      },
    });
  }

  navigateToCreate(): void {
    this.router.navigate(['/news/create']);
  }

  openPost(post: NewsPost): void {
    this.router.navigate(['/news', post.id]);
  }

  statusKey(status: NewsPostStatus): string {
    return `NEWS.STATUS_${status.toUpperCase()}`;
  }

  addedLabel(post: NewsPost): { date: string; time: string } {
    return formatNewsDateTime(post.createdAt);
  }

  commentsLabel(post: NewsPost): string {
    return formatLikeCount(post.commentsCount ?? 0);
  }

  setPostReaction(post: NewsPost, key: NewsReaction): void {
    const next = toggleReaction(post.reactionCounts, post.myReaction, key);
    this.newsService.updatePost(post.id, next).subscribe({
      next: () => {
        this.posts.update((posts) =>
          posts.map((item) => (item.id === post.id ? { ...item, ...next } : item)),
        );
      },
    });
  }

  thumbnail(post: NewsPost): string {
    if (post.attachmentUrl && isImageAttachment(post.attachmentUrl, post.attachmentName)) {
      return post.attachmentUrl;
    }
    return '';
  }
}

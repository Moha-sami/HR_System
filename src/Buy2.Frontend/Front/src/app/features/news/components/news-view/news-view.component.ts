import { Component, inject, input, signal, type OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import { CURRENT_NEWS_AUTHOR, toggleReaction, type NewsPost, type NewsReaction } from '../../models/news.models';
import { NewsService } from '../../services/news.service';
import { formatLikeCount, formatNewsDateTime, isImageAttachment } from '../../utils/news.utils';
import { NewsCommentsModalComponent } from '../news-comments-modal/news-comments-modal.component';
import { NewsReactionBarComponent } from '../news-reaction-bar/news-reaction-bar.component';

@Component({
  selector: 'app-news-view',
  standalone: true,
  imports: [TranslatePipe, ModalComponent, ModalBodyComponent, NewsCommentsModalComponent, NewsReactionBarComponent],
  templateUrl: './news-view.component.html',
  styleUrls: ['../../styles/news-dialog.css', './news-view.component.css'],
})
export class NewsViewComponent implements OnInit {
  private readonly newsService = inject(NewsService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  readonly id = input<string>();
  readonly post = signal<NewsPost | null>(null);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly showDeleteModal = signal(false);
  readonly showArchiveModal = signal(false);
  readonly showSuccessModal = signal(false);
  readonly isDeleting = signal(false);
  readonly isArchiving = signal(false);
  readonly actionError = signal<string | null>(null);
  readonly successKind = signal<'delete' | 'archive'>('delete');
  readonly showComments = signal(false);

  ngOnInit(): void {
    this.loadPost();
  }

  loadPost(): void {
    const postId = this.id();
    if (!postId) {
      this.loadError.set(true);
      return;
    }
    this.loading.set(true);
    this.newsService.getPost(postId).subscribe({
      next: (post) => {
        this.post.set(post);
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set(true);
        this.loading.set(false);
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/news']);
  }

  editPost(): void {
    const postId = this.id();
    if (postId) {
      this.router.navigate(['/news/edit', postId]);
    }
  }

  openDeleteModal(): void {
    this.actionError.set(null);
    this.showDeleteModal.set(true);
  }

  closeDeleteModal(): void {
    if (!this.isDeleting()) {
      this.showDeleteModal.set(false);
    }
  }

  confirmDelete(): void {
    const postId = this.id();
    if (!postId || this.isDeleting()) {
      return;
    }
    this.isDeleting.set(true);
    this.actionError.set(null);
    this.newsService.deletePost(postId).subscribe({
      next: () => {
        this.isDeleting.set(false);
        this.showDeleteModal.set(false);
        this.successKind.set('delete');
        this.showSuccessModal.set(true);
      },
      error: () => {
        this.isDeleting.set(false);
        this.actionError.set(this.translate.instant('NEWS.DELETE_ERROR'));
      },
    });
  }

  confirmSuccess(): void {
    this.showSuccessModal.set(false);
    if (this.successKind() === 'delete') {
      this.router.navigate(['/news']);
    }
  }

  openArchiveModal(): void {
    this.actionError.set(null);
    this.showArchiveModal.set(true);
  }

  closeArchiveModal(): void {
    if (!this.isArchiving()) {
      this.showArchiveModal.set(false);
    }
  }

  confirmArchive(): void {
    const current = this.post();
    if (!current || current.status === 'archived' || this.isArchiving()) {
      return;
    }
    this.isArchiving.set(true);
    this.actionError.set(null);
    this.newsService
      .updatePost(current.id, {
        status: 'archived',
        updatedAt: new Date().toISOString(),
        updatedBy: CURRENT_NEWS_AUTHOR,
      })
      .subscribe({
        next: (updated) => {
          this.post.set(updated);
          this.isArchiving.set(false);
          this.showArchiveModal.set(false);
          this.successKind.set('archive');
          this.showSuccessModal.set(true);
        },
        error: () => {
          this.isArchiving.set(false);
          this.actionError.set(this.translate.instant('NEWS.ARCHIVE_ERROR'));
        },
      });
  }

  successTitleKey(): string {
    return this.successKind() === 'archive' ? 'NEWS.ARCHIVE_SUCCESS_TITLE' : 'NEWS.DELETE_SUCCESS_TITLE';
  }

  successMsgKey(): string {
    return this.successKind() === 'archive' ? 'NEWS.ARCHIVE_SUCCESS_MSG' : 'NEWS.DELETE_SUCCESS_MSG';
  }

  createdMeta(post: NewsPost): { date: string; time: string } {
    return formatNewsDateTime(post.createdAt);
  }

  updatedMeta(post: NewsPost): { date: string; time: string } {
    return formatNewsDateTime(post.updatedAt);
  }

  wasUpdated(post: NewsPost): boolean {
    return post.updatedAt !== post.createdAt;
  }

  commentsLabel(post: NewsPost): string {
    return formatLikeCount(post.commentsCount ?? 0);
  }

  setPostReaction(key: NewsReaction): void {
    const current = this.post();
    if (!current) {
      return;
    }
    const next = toggleReaction(current.reactionCounts, current.myReaction, key);
    this.newsService.updatePost(current.id, next).subscribe({
      next: (updated) => this.post.set({ ...current, ...updated, ...next }),
    });
  }

  openComments(): void {
    this.showComments.set(true);
  }

  onCommentsCount(count: number): void {
    this.post.update((current) => (current ? { ...current, commentsCount: count } : current));
  }

  hasImage(post: NewsPost): boolean {
    return !!post.attachmentUrl && isImageAttachment(post.attachmentUrl, post.attachmentName);
  }

  statusKey(status: NewsPost['status']): string {
    return `NEWS.STATUS_${status.toUpperCase()}`;
  }
}

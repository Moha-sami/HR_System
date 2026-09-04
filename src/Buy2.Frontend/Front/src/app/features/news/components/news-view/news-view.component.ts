import { Component, inject, input, signal, type OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import { CURRENT_NEWS_AUTHOR, type NewsPost } from '../../models/news.models';
import { NewsService } from '../../services/news.service';
import { formatLikeCount, formatNewsDateTime, isImageAttachment } from '../../utils/news.utils';

@Component({
  selector: 'app-news-view',
  standalone: true,
  imports: [TranslatePipe, ModalComponent, ModalBodyComponent],
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

  likesLabel(post: NewsPost): string {
    return formatLikeCount(post.likesCount);
  }

  hasImage(post: NewsPost): boolean {
    return !!post.attachmentUrl && isImageAttachment(post.attachmentUrl, post.attachmentName);
  }

  statusKey(status: NewsPost['status']): string {
    return `NEWS.STATUS_${status.toUpperCase()}`;
  }
}

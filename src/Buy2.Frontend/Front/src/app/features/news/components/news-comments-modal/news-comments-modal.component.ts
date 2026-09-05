import { NgTemplateOutlet } from '@angular/common';
import { Component, HostListener, type OnInit, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { concat } from 'rxjs';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import {
  CURRENT_NEWS_AUTHOR,
  CURRENT_NEWS_AVATAR,
  emptyReactionCounts,
  toggleReaction,
  type CreateNewsCommentDto,
  type NewsComment,
  type NewsReaction,
} from '../../models/news.models';
import { NewsService } from '../../services/news.service';
import { formatRelativeTime } from '../../utils/news.utils';
import { NewsReactionBarComponent } from '../news-reaction-bar/news-reaction-bar.component';

@Component({
  selector: 'app-news-comments-modal',
  standalone: true,
  imports: [NgTemplateOutlet, FormsModule, TranslatePipe, ModalComponent, ModalBodyComponent, NewsReactionBarComponent],
  templateUrl: './news-comments-modal.component.html',
  styleUrls: ['../../styles/news-dialog.css', './news-comments-modal.component.css'],
})
export class NewsCommentsModalComponent implements OnInit {
  private readonly newsService = inject(NewsService);

  readonly postId = input.required<string>();
  readonly closed = output<void>();
  readonly countChanged = output<number>();

  readonly comments = signal<NewsComment[]>([]);
  readonly loading = signal(false);
  readonly view = signal<'list' | 'replies'>('list');
  readonly parentComment = signal<NewsComment | null>(null);
  readonly openMenuId = signal<string | null>(null);
  readonly editingId = signal<string | null>(null);
  readonly editDraft = signal('');
  readonly composerText = signal('');
  readonly repliesVisible = signal(false);
  readonly showDeleteModal = signal(false);
  readonly showSuccessModal = signal(false);
  readonly isDeleting = signal(false);
  readonly pendingDelete = signal<NewsComment | null>(null);
  readonly deleteKind = signal<'remove' | 'permanent'>('remove');
  readonly successKind = signal<'remove' | 'permanent' | 'restore'>('remove');

  readonly topLevelComments = computed(() =>
    this.comments()
      .filter((c) => !c.parentId)
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()),
  );

  readonly threadReplies = computed(() => {
    const parent = this.parentComment();
    if (!parent) {
      return [];
    }
    return this.comments()
      .filter((c) => c.parentId === parent.id)
      .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
  });

  ngOnInit(): void {
    this.loadComments();
  }

  @HostListener('document:click')
  closeMenus(): void {
    this.openMenuId.set(null);
  }

  loadComments(): void {
    this.loading.set(true);
    this.newsService.getComments(this.postId()).subscribe({
      next: (comments) => {
        this.comments.set(comments);
        this.loading.set(false);
        this.countChanged.emit(comments.length);
        const parent = this.parentComment();
        if (parent) {
          const updated = comments.find((c) => c.id === parent.id) ?? null;
          this.parentComment.set(updated);
          if (!updated) {
            this.view.set('list');
          }
        }
      },
      error: () => this.loading.set(false),
    });
  }

  closePanel(): void {
    this.closed.emit();
  }

  repliesFor(comment: NewsComment): NewsComment[] {
    return this.comments().filter((c) => c.parentId === comment.id);
  }

  relative(comment: NewsComment): string {
    return formatRelativeTime(comment.createdAt);
  }

  toggleMenu(event: Event, id: string): void {
    event.stopPropagation();
    this.openMenuId.update((current) => (current === id ? null : id));
  }

  startEdit(comment: NewsComment): void {
    this.openMenuId.set(null);
    this.editingId.set(comment.id);
    this.editDraft.set(comment.content);
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.editDraft.set('');
  }

  saveEdit(comment: NewsComment): void {
    const content = this.editDraft().trim();
    if (!content) {
      return;
    }
    this.newsService.updateComment(comment.id, { content }).subscribe({
      next: () => {
        this.cancelEdit();
        this.loadComments();
      },
    });
  }

  askDelete(comment: NewsComment): void {
    this.openMenuId.set(null);
    this.pendingDelete.set(comment);
    this.deleteKind.set('remove');
    this.showDeleteModal.set(true);
  }

  askPermanentDelete(comment: NewsComment): void {
    this.openMenuId.set(null);
    this.pendingDelete.set(comment);
    this.deleteKind.set('permanent');
    this.showDeleteModal.set(true);
  }

  restoreComment(comment: NewsComment): void {
    this.openMenuId.set(null);
    this.newsService.updateComment(comment.id, { removedByAdmin: false }).subscribe({
      next: () => {
        this.successKind.set('restore');
        this.showSuccessModal.set(true);
        this.loadComments();
      },
    });
  }

  closeDeleteModal(): void {
    if (!this.isDeleting()) {
      this.showDeleteModal.set(false);
      this.pendingDelete.set(null);
    }
  }

  confirmDelete(): void {
    const target = this.pendingDelete();
    if (!target || this.isDeleting()) {
      return;
    }
    this.isDeleting.set(true);
    const finish = (kind: 'remove' | 'permanent'): void => {
      this.isDeleting.set(false);
      this.showDeleteModal.set(false);
      this.pendingDelete.set(null);
      this.successKind.set(kind);
      this.showSuccessModal.set(true);
      this.loadComments();
    };

    if (this.deleteKind() === 'remove') {
      this.newsService.updateComment(target.id, { removedByAdmin: true }).subscribe({
        next: () => finish('remove'),
        error: () => this.isDeleting.set(false),
      });
      return;
    }

    const children = this.comments().filter((c) => c.parentId === target.id);
    concat(
      ...children.map((child) => this.newsService.deleteComment(child.id, this.postId())),
      this.newsService.deleteComment(target.id, this.postId()),
    ).subscribe({
      next: () => finish('permanent'),
      error: () => this.isDeleting.set(false),
    });
  }

  closeSuccessModal(): void {
    this.showSuccessModal.set(false);
  }

  successTitleKey(): string {
    switch (this.successKind()) {
      case 'restore':
        return 'NEWS.RESTORE_COMMENT_SUCCESS_TITLE';
      case 'permanent':
        return 'NEWS.DELETE_PERMANENT_SUCCESS_TITLE';
      default:
        return 'NEWS.DELETE_COMMENT_SUCCESS_TITLE';
    }
  }

  successMsgKey(): string {
    switch (this.successKind()) {
      case 'restore':
        return 'NEWS.RESTORE_COMMENT_SUCCESS_MSG';
      case 'permanent':
        return 'NEWS.DELETE_PERMANENT_SUCCESS_MSG';
      default:
        return 'NEWS.DELETE_COMMENT_SUCCESS_MSG';
    }
  }

  openReplies(comment: NewsComment): void {
    if (comment.removedByAdmin) {
      return;
    }
    this.parentComment.set(comment);
    this.view.set('replies');
    this.repliesVisible.set(true);
    this.composerText.set('');
    this.openMenuId.set(null);
  }

  backToList(): void {
    this.view.set('list');
    this.parentComment.set(null);
    this.repliesVisible.set(false);
    this.composerText.set('');
  }

  hideReplies(): void {
    this.repliesVisible.set(false);
  }

  showReplies(): void {
    this.repliesVisible.set(true);
  }

  send(): void {
    const content = this.composerText().trim();
    if (!content) {
      return;
    }
    const parent = this.view() === 'replies' ? this.parentComment() : null;
    const dto: CreateNewsCommentDto = {
      postId: this.postId(),
      parentId: parent?.id ?? '',
      authorName: CURRENT_NEWS_AUTHOR,
      authorAvatar: CURRENT_NEWS_AVATAR,
      content,
      createdAt: new Date().toISOString(),
      reactionCounts: emptyReactionCounts(),
      myReaction: null,
      removedByAdmin: false,
    };
    this.newsService.createComment(dto).subscribe({
      next: () => {
        this.composerText.set('');
        this.loadComments();
      },
    });
  }

  setReaction(comment: NewsComment, key: NewsReaction): void {
    this.openMenuId.set(null);
    const next = toggleReaction(comment.reactionCounts, comment.myReaction, key);
    this.newsService.updateComment(comment.id, next).subscribe({
      next: () => this.loadComments(),
    });
  }
}

import { Component, inject, input, signal, type OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Subject, type Observable } from 'rxjs';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import type { HasUnsavedChanges } from '../../guards/unsaved-changes.guard';
import {
  CURRENT_NEWS_AUTHOR,
  emptyReactionCounts,
  type CreateNewsPostDto,
  type NewsCategory,
  type NewsPost,
  type NewsPostStatus,
} from '../../models/news.models';
import { NewsService } from '../../services/news.service';
import {
  isImageAttachment,
  resolvePublishStatus,
  toDateInputValue,
  toTimeInputValue,
  validateNewsFile,
} from '../../utils/news.utils';

@Component({
  selector: 'app-news-form',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, ModalComponent, ModalBodyComponent],
  templateUrl: './news-form.component.html',
  styleUrls: ['../../styles/news-dialog.css', './news-form.component.css'],
})
export class NewsFormComponent implements OnInit, HasUnsavedChanges {
  private readonly fb = inject(FormBuilder);
  private readonly newsService = inject(NewsService);
  private readonly router = inject(Router);
  private deactivateResult: Subject<boolean> | null = null;

  readonly id = input<string>();

  isEditMode = false;
  private existingPost: NewsPost | null = null;

  readonly categories = signal<NewsCategory[]>([]);
  readonly isSubmitting = signal(false);
  readonly showSuccessModal = signal(false);
  readonly showUnsavedModal = signal(false);
  readonly showErrorModal = signal(false);
  private retryPersist: (() => void) | null = null;
  readonly showPreview = signal(false);
  readonly isDragging = signal(false);
  readonly submitError = signal<string | null>(null);
  readonly loadError = signal(false);
  readonly attachmentError = signal<string | null>(null);
  readonly successKind = signal<'create' | 'draft' | 'update'>('create');

  readonly form = this.fb.group({
    title: ['', Validators.required],
    body: [''],
    category: [''],
    attachmentUrl: [''],
    attachmentName: [''],
    publishDate: [''],
    publishTime: [''],
  });

  ngOnInit(): void {
    const postId = this.id();
    if (postId) {
      this.isEditMode = true;
      this.loadPost(postId);
    }

    this.newsService.getCategories().subscribe({
      next: (categories) => this.categories.set(categories),
    });

    this.form.controls.body.valueChanges.subscribe((value) => {
      if (value?.trim() && this.form.controls.body.hasError('required')) {
        this.form.controls.body.setErrors(null);
      }
    });
  }

  canDeactivate(): boolean | Observable<boolean> {
    if (this.showSuccessModal() || !this.form.dirty) {
      return true;
    }
    this.showUnsavedModal.set(true);
    this.deactivateResult = new Subject<boolean>();
    return this.deactivateResult.asObservable();
  }

  loadPost(postId: string): void {
    this.newsService.getPost(postId).subscribe({
      next: (post) => {
        this.existingPost = post;
        this.form.patchValue({
          title: post.title,
          body: post.body,
          category: post.category,
          attachmentUrl: post.attachmentUrl,
          attachmentName: post.attachmentName,
          publishDate: toDateInputValue(post.publishAt),
          publishTime: toTimeInputValue(post.publishAt),
        });
        this.form.markAsPristine();
      },
      error: () => this.loadError.set(true),
    });
  }

  isInvalid(ctrl: string): boolean {
    const control = this.form.get(ctrl);
    return !!(control && control.invalid && control.touched);
  }

  onBack(): void {
    this.router.navigate(['/news']);
  }

  onPreview(): void {
    this.showPreview.set(true);
  }

  closePreview(): void {
    this.showPreview.set(false);
  }

  triggerFilePicker(): void {
    document.getElementById('news-file-input')?.click();
  }

  onFileSelected(event: Event): void {
    const inputEl = event.target as HTMLInputElement;
    const file = inputEl.files?.[0];
    if (file) {
      this.handleFile(file);
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(true);
  }

  onDragLeave(): void {
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (file) {
      this.handleFile(file);
    }
  }

  removeAttachment(): void {
    this.form.patchValue({ attachmentUrl: '', attachmentName: '' });
    this.form.markAsDirty();
    this.attachmentError.set(null);
    const inputEl = document.getElementById('news-file-input') as HTMLInputElement | null;
    if (inputEl) {
      inputEl.value = '';
    }
  }

  saveAsDraft(): void {
    const title = this.form.controls.title.value?.trim() ?? '';
    if (!title) {
      this.form.controls.title.markAsTouched();
      return;
    }
    this.persist('draft', this.existingPost?.publishAt ?? null, 'draft', false);
  }

  publish(): void {
    const title = this.form.controls.title.value?.trim() ?? '';
    const body = this.form.controls.body.value?.trim() ?? '';
    this.form.controls.title.markAsTouched();
    this.form.controls.body.markAsTouched();
    if (!title) {
      return;
    }
    if (!body) {
      this.form.controls.body.setErrors({ required: true });
      return;
    }
    const resolved = resolvePublishStatus(
      this.form.controls.publishDate.value ?? '',
      this.form.controls.publishTime.value ?? '',
    );
    this.persist(resolved.status, resolved.publishAt, this.isEditMode ? 'update' : 'create', false);
  }

  discardUnsaved(): void {
    this.form.markAsPristine();
    this.showUnsavedModal.set(false);
    this.resolveDeactivate(true);
  }

  saveUnsavedAsDraft(): void {
    const title = this.form.controls.title.value?.trim() ?? '';
    if (!title) {
      this.form.controls.title.markAsTouched();
      this.showUnsavedModal.set(false);
      this.resolveDeactivate(false);
      return;
    }
    this.persist('draft', this.existingPost?.publishAt ?? null, 'draft', true);
  }

  closeUnsaved(): void {
    this.showUnsavedModal.set(false);
    this.resolveDeactivate(false);
  }

  confirmSuccess(): void {
    this.showSuccessModal.set(false);
    this.router.navigate(['/news']);
  }

  closeErrorModal(): void {
    this.showErrorModal.set(false);
    this.retryPersist = null;
  }

  retrySave(): void {
    this.showErrorModal.set(false);
    this.retryPersist?.();
  }

  previewTitle(): string {
    return this.form.controls.title.value?.trim() || '';
  }

  previewBody(): string {
    return this.form.controls.body.value ?? '';
  }

  previewAttachment(): string {
    return this.form.controls.attachmentUrl.value ?? '';
  }

  previewAttachmentName(): string {
    return this.form.controls.attachmentName.value ?? '';
  }

  previewIsImage(): boolean {
    return isImageAttachment(this.previewAttachment(), this.previewAttachmentName());
  }

  private handleFile(file: File): void {
    const errorKey = validateNewsFile(file);
    if (errorKey) {
      this.attachmentError.set(errorKey);
      this.form.markAsDirty();
      return;
    }
    this.attachmentError.set(null);
    const reader = new FileReader();
    reader.onload = () => {
      this.form.patchValue({
        attachmentUrl: String(reader.result),
        attachmentName: file.name,
      });
      this.form.markAsDirty();
    };
    reader.readAsDataURL(file);
  }

  private persist(
    status: NewsPostStatus,
    publishAt: string | null,
    successKind: 'create' | 'draft' | 'update',
    fromUnsavedGuard: boolean,
  ): void {
    if (this.isSubmitting()) {
      return;
    }

    const now = new Date().toISOString();
    const value = this.form.getRawValue();
    const dto: CreateNewsPostDto = {
      title: value.title!.trim(),
      body: value.body?.trim() ?? '',
      category: value.category ?? '',
      status,
      publishAt,
      createdAt: this.existingPost?.createdAt ?? now,
      updatedAt: now,
      createdBy: this.existingPost?.createdBy ?? CURRENT_NEWS_AUTHOR,
      updatedBy: CURRENT_NEWS_AUTHOR,
      attachmentUrl: value.attachmentUrl ?? '',
      attachmentName: value.attachmentName ?? '',
      reactionCounts: this.existingPost?.reactionCounts ?? emptyReactionCounts(),
      myReaction: this.existingPost?.myReaction ?? null,
      commentsCount: this.existingPost?.commentsCount ?? 0,
    };

    this.isSubmitting.set(true);
    this.submitError.set(null);

    const postId = this.id();
    const request$ =
      this.isEditMode && postId
        ? this.newsService.updatePost(postId, dto)
        : this.newsService.createPost(dto);

    request$.subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.form.markAsPristine();
        this.retryPersist = null;
        if (fromUnsavedGuard) {
          this.showUnsavedModal.set(false);
          this.resolveDeactivate(true);
          return;
        }
        this.successKind.set(successKind);
        this.showSuccessModal.set(true);
      },
      error: () => {
        this.isSubmitting.set(false);
        if (fromUnsavedGuard) {
          this.showUnsavedModal.set(false);
          this.resolveDeactivate(false);
          return;
        }
        this.retryPersist = () => this.persist(status, publishAt, successKind, false);
        this.showErrorModal.set(true);
      },
    });
  }

  private resolveDeactivate(allow: boolean): void {
    this.deactivateResult?.next(allow);
    this.deactivateResult?.complete();
    this.deactivateResult = null;
  }

  successTitleKey(): string {
    if (this.successKind() === 'draft') {
      return 'NEWS.DRAFT_SUCCESS_TITLE';
    }
    if (this.successKind() === 'update') {
      return 'NEWS.UPDATE_SUCCESS_TITLE';
    }
    return 'NEWS.CREATE_SUCCESS_TITLE';
  }

  successMsgKey(): string {
    if (this.successKind() === 'draft') {
      return 'NEWS.DRAFT_SUCCESS_MSG';
    }
    if (this.successKind() === 'update') {
      return 'NEWS.UPDATE_SUCCESS_MSG';
    }
    return 'NEWS.CREATE_SUCCESS_MSG';
  }
}

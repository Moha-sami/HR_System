import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { RewardProfileDto } from '../../models/reward.models';
import { SEEDED_REWARD_CATEGORIES } from '../../models/reward.models';
import { RewardService } from '../../services/reward.service';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';

const MAX_IMAGE_BYTES = 1 * 1024 * 1024;
const ALLOWED_IMAGE_TYPES = ['image/jpeg', 'image/png', 'image/jpg'];

@Component({
  selector: 'app-reward-form',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, ModalComponent, ModalBodyComponent],
  templateUrl: './reward-form.component.html',
  styleUrl: './reward-form.component.css',
})
export class RewardFormComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly rewardService = inject(RewardService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  readonly id = input<string>();

  isEditMode = false;
  readonly seededCategories = SEEDED_REWARD_CATEGORIES;
  readonly isSubmitting = signal(false);
  readonly showSuccessModal = signal(false);
  readonly submitError = signal<string | null>(null);
  readonly loadError = signal(false);
  readonly imageFile = signal<File | null>(null);
  readonly imagePreview = signal('');

  readonly form = this.fb.group({
    imageUrl: [''],
    name: ['', [Validators.required, Validators.minLength(2)]],
    description: [''],
    categoryId: [null as number | null, [Validators.required]],
    price: [null as number | null, [Validators.required, Validators.min(0)]],
    pointsValue: [null as number | null, [Validators.required, Validators.min(1)]],
    howToRedeem: ['', [Validators.required]],
    termsOfUse: ['', [Validators.required]],
  });

  ngOnInit(): void {
    const rewardId = this.id();
    if (rewardId) {
      this.isEditMode = true;
      this.loadReward(rewardId);
    }
  }

  ngOnDestroy(): void {
    this.revokePreview();
  }

  loadReward(rewardId: string): void {
    this.rewardService.getRewardProfile(rewardId).subscribe({
      next: (response) => this.patchForm(response.profile),
      error: () => this.loadError.set(true),
    });
  }

  patchForm(reward: RewardProfileDto): void {
    const categoryId =
      this.seededCategories.find(
        (category) => category.name.toLowerCase() === reward.category.toLowerCase(),
      )?.id ?? null;

    this.imagePreview.set(reward.imageUrl ?? '');
    this.form.patchValue({
      imageUrl: reward.imageUrl ?? '',
      name: reward.name,
      description: reward.description ?? '',
      categoryId,
      price: reward.monetaryValue,
      pointsValue: reward.points,
      howToRedeem: reward.howToRedeem ?? '',
      termsOfUse: reward.termsOfUse ?? '',
    });
  }

  isInvalid(ctrl: string): boolean {
    const control = this.form.get(ctrl);
    return !!(control && control.invalid && control.touched);
  }

  triggerFilePicker(): void {
    document.getElementById('reward-image-input')?.click();
  }

  onImageSelected(event: Event): void {
    const inputEl = event.target as HTMLInputElement;
    const file = inputEl.files?.[0];
    if (!file) {
      return;
    }

    if (!this.isAllowedImage(file)) {
      this.submitError.set(this.translate.instant('REWARD_MANAGEMENT.IMAGE_INVALID'));
      inputEl.value = '';
      return;
    }

    this.submitError.set(null);
    this.revokePreview();
    this.imageFile.set(file);
    const preview = URL.createObjectURL(file);
    this.imagePreview.set(preview);
    this.form.patchValue({ imageUrl: preview });
    this.form.get('imageUrl')?.markAsTouched();
  }

  removeImage(): void {
    this.revokePreview();
    this.imageFile.set(null);
    this.imagePreview.set('');
    this.form.patchValue({ imageUrl: '' });
    this.form.get('imageUrl')?.markAsTouched();
    const inputEl = document.getElementById('reward-image-input') as HTMLInputElement | null;
    if (inputEl) {
      inputEl.value = '';
    }
  }

  onDiscard(): void {
    this.router.navigate(['/rewards']);
  }

  onSubmit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);

    const value = this.form.getRawValue();
    const payload = {
      name: value.name!.trim(),
      description: value.description?.trim() ?? '',
      categoryId: Number(value.categoryId),
      points: Number(value.pointsValue),
      monetaryValue: Number(value.price),
      howToRedeem: value.howToRedeem?.trim() ?? '',
      termsOfUse: value.termsOfUse?.trim() ?? '',
      imageFile: this.imageFile(),
    };

    const rewardId = this.id();
    const request$ =
      this.isEditMode && rewardId
        ? this.rewardService.updateReward(rewardId, payload)
        : this.rewardService.createReward(payload);

    request$.subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.showSuccessModal.set(true);
      },
      error: (err: unknown) => {
        this.isSubmitting.set(false);
        this.submitError.set(
          this.extractErrorMessage(
            err,
            this.isEditMode ? 'REWARD_MANAGEMENT.UPDATE_ERROR' : 'REWARD_MANAGEMENT.CREATE_ERROR',
          ),
        );
      },
    });
  }

  confirmSuccess(): void {
    this.showSuccessModal.set(false);
    this.router.navigate(['/rewards']);
  }

  private isAllowedImage(file: File): boolean {
    const typeOk =
      ALLOWED_IMAGE_TYPES.includes(file.type) || /\.(jpe?g|png)$/i.test(file.name);
    return typeOk && file.size > 0 && file.size <= MAX_IMAGE_BYTES;
  }

  private revokePreview(): void {
    const preview = this.imagePreview();
    if (preview.startsWith('blob:')) {
      URL.revokeObjectURL(preview);
    }
  }

  private extractErrorMessage(err: unknown, fallbackKey: string): string {
    if (err instanceof HttpErrorResponse) {
      if (typeof err.error?.message === 'string' && err.error.message.trim()) {
        return err.error.message;
      }
      if (typeof err.error === 'string' && err.error.trim()) {
        return err.error;
      }
    }
    return this.translate.instant(fallbackKey);
  }
}

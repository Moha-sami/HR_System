import { Component, OnInit, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { CreateRewardDto, RewardCategory, RewardItem } from '../../models/reward.models';
import { RewardService } from '../../services/reward.service';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';

@Component({
  selector: 'app-reward-form',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, ModalComponent, ModalBodyComponent],
  templateUrl: './reward-form.component.html',
  styleUrl: './reward-form.component.css',
})
export class RewardFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly rewardService = inject(RewardService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  readonly id = input<string>();

  isEditMode = false;
  readonly categories = signal<RewardCategory[]>([]);
  readonly isSubmitting = signal(false);
  readonly showSuccessModal = signal(false);
  readonly submitError = signal<string | null>(null);
  readonly loadError = signal(false);

  readonly form = this.fb.group({
    imageUrl: ['', Validators.required],
    name: ['', [Validators.required, Validators.minLength(2)]],
    description: [''],
    category: ['', Validators.required],
    cost: [null as number | null, [Validators.required, Validators.min(0)]],
    price: [null as number | null, [Validators.required, Validators.min(0)]],
    pointsValue: [null as number | null, [Validators.required, Validators.min(0)]],
    howToRedeem: [''],
    termsOfUse: [''],
  });

  ngOnInit(): void {
    const rewardId = this.id();
    if (rewardId) {
      this.isEditMode = true;
      this.loadReward(rewardId);
    }

    this.rewardService.getCategories().subscribe({
      next: (categories) => this.categories.set(categories),
    });
  }

  loadReward(rewardId: string): void {
    this.rewardService.getReward(rewardId).subscribe({
      next: (reward) => this.patchForm(reward),
      error: () => this.loadError.set(true),
    });
  }

  patchForm(reward: RewardItem): void {
    this.form.patchValue({
      imageUrl: reward.imageUrl,
      name: reward.name,
      description: reward.description,
      category: reward.category,
      cost: reward.cost,
      price: reward.price,
      pointsValue: reward.pointsValue,
      howToRedeem: reward.howToRedeem,
      termsOfUse: reward.termsOfUse,
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

    const reader = new FileReader();
    reader.onload = () => {
      this.form.patchValue({ imageUrl: String(reader.result) });
      this.form.get('imageUrl')?.markAsTouched();
    };
    reader.readAsDataURL(file);
  }

  removeImage(): void {
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

    const value = this.form.getRawValue();
    const dto: CreateRewardDto = {
      name: value.name!.trim(),
      description: value.description?.trim() ?? '',
      category: value.category!,
      imageUrl: value.imageUrl!,
      cost: Number(value.cost),
      price: Number(value.price),
      pointsValue: Number(value.pointsValue),
      howToRedeem: value.howToRedeem?.trim() ?? '',
      termsOfUse: value.termsOfUse?.trim() ?? '',
      status: 'Active',
      availableStock: 0,
      createdAt: new Date().toISOString(),
    };

    this.isSubmitting.set(true);
    this.submitError.set(null);

    const rewardId = this.id();
    const request$ =
      this.isEditMode && rewardId
        ? this.rewardService.updateReward(rewardId, {
            name: dto.name,
            description: dto.description,
            category: dto.category,
            imageUrl: dto.imageUrl,
            cost: dto.cost,
            price: dto.price,
            pointsValue: dto.pointsValue,
            howToRedeem: dto.howToRedeem,
            termsOfUse: dto.termsOfUse,
          })
        : this.rewardService.createReward(dto);

    request$.subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.showSuccessModal.set(true);
      },
      error: () => {
        this.isSubmitting.set(false);
        this.submitError.set(
          this.translate.instant(
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
}

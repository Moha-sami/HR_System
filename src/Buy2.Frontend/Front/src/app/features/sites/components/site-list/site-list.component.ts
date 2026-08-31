import {
  Component,
  inject,
  signal,
  computed,
  type OnInit,
  type OnDestroy,
  HostListener,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subject, takeUntil } from 'rxjs';
import { SiteService } from '../../services/site.service';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import type { SiteListItemDto, RegionDto } from '../../models/site.models';

@Component({
  selector: 'app-site-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
    ModalComponent,
    ModalBodyComponent,
  ],
  templateUrl: './site-list.component.html',
  styleUrl: './site-list.component.css',
})
export class SiteListComponent implements OnInit, OnDestroy {
  private readonly siteService = inject(SiteService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly destroy$ = new Subject<void>();

  // ── State ─────────────────────────────────────────────────────────────────
  readonly sites = signal<SiteListItemDto[]>([]);
  readonly regions = signal<RegionDto[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly searchQuery = signal('');
  readonly selectedRegion = signal<number | null>(null);

  // ── Delete modal state ────────────────────────────────────────────────────
  readonly showDeleteModal = signal(false);
  readonly showSuccessModal = signal(false);
  readonly deletingSite = signal<SiteListItemDto | null>(null);
  readonly isDeleting = signal(false);
  readonly deleteError = signal<string | null>(null);

  // ── Three-dot menu state ──────────────────────────────────────────────────
  readonly openMenuId = signal<number | null>(null);

  // ── Computed ──────────────────────────────────────────────────────────────
  readonly filteredSites = computed(() => {
    let list = this.sites();
    const q = this.searchQuery().toLowerCase().trim();
    const reg = this.selectedRegion();
    if (q) {
      list = list.filter((s) => s.siteName.toLowerCase().includes(q));
    }
    if (reg) {
      list = list.filter((s) => s.regionId === reg);
    }
    return list;
  });

  ngOnInit(): void {
    this.loadSites();
    this.loadRegions();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Close menu when clicking outside ─────────────────────────────────────
  @HostListener('document:click')
  onDocumentClick(): void {
    this.openMenuId.set(null);
  }

  // ── Data loading ──────────────────────────────────────────────────────────
  loadSites(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.siteService
      .getSites()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.sites.set(res);
          this.loading.set(false);
        },
        error: () => {
          this.loadError.set(true);
          this.loading.set(false);
        },
      });
  }

  loadRegions(): void {
    this.siteService
      .getRegions()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => this.regions.set(res),
        error: () => {},
      });
  }

  // ── Navigation ────────────────────────────────────────────────────────────
  navigateToCreate(): void {
    this.router.navigate(['/sites/create']);
  }

  // ── Three-dot menu ─────────────────────────────────────────────────────────
  toggleMenu(event: Event, siteId: number): void {
    event.stopPropagation();
    this.openMenuId.update((cur) => (cur === siteId ? null : siteId));
  }

  // ── Delete flow ───────────────────────────────────────────────────────────
  openDeleteModal(event: Event, site: SiteListItemDto): void {
    event.stopPropagation();
    this.openMenuId.set(null);
    this.deletingSite.set(site);
    this.deleteError.set(null);
    this.showDeleteModal.set(true);
  }

  closeDeleteModal(): void {
    if (this.isDeleting()) return;
    this.showDeleteModal.set(false);
    this.deletingSite.set(null);
    this.deleteError.set(null);
  }

  confirmDelete(): void {
    const site = this.deletingSite();
    if (!site || this.isDeleting()) return;
    this.isDeleting.set(true);
    this.deleteError.set(null);
    this.siteService
      .deleteSite(site.id, { employeeSiteReassignments: [] })
      .subscribe({
        next: () => {
          this.isDeleting.set(false);
          this.showDeleteModal.set(false);
          this.showSuccessModal.set(true);
        },
        error: () => {
          this.isDeleting.set(false);
          this.deleteError.set(this.translate.instant('SITE_MANAGEMENT.DELETE_ERROR'));
        },
      });
  }

  confirmSuccess(): void {
    this.showSuccessModal.set(false);
    this.deletingSite.set(null);
    this.loadSites();
  }
}

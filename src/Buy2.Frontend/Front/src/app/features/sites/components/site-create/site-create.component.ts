import {
  Component,
  inject,
  signal,
  type OnInit,
  type OnDestroy,
  ElementRef,
  ViewChild,
  AfterViewInit,
  computed,
  HostListener,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators, type AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subject, takeUntil } from 'rxjs';
import { SiteService, type EmployeeListItemDto } from '../../services/site.service';
import { ModalComponent } from '@app/shared/components/modal/modal.component';
import { ModalBodyComponent } from '@app/shared/components/modal/modal-body.component';
import { ModalFooterComponent } from '@app/shared/components/modal/modal-footer.component';
import type { RegionDto, SiteOperationalHourDto } from '../../models/site.models';

// Day index: 0=Sunday 1=Mon 2=Tue 3=Wed 4=Thu 5=Fri 6=Sat
const DAY_KEYS = ['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT'];

export interface DayHour {
  dayIndex: number;
  labelKey: string;
  isOpen: boolean;
  from: string;
  to: string;
}

@Component({
  selector: 'app-site-create',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TranslatePipe,
    ModalComponent,
    ModalBodyComponent,
    ModalFooterComponent,
  ],
  templateUrl: './site-create.component.html',
  styleUrl: './site-create.component.css',
})
export class SiteCreateComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly siteService = inject(SiteService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly destroy$ = new Subject<void>();

  @ViewChild('mapContainer') mapContainerRef!: ElementRef<HTMLDivElement>;

  // ── State ─────────────────────────────────────────────────────────────────
  readonly regions = signal<RegionDto[]>([]);
  readonly isSubmitting = signal(false);
  readonly showSuccessModal = signal(false);
  readonly createdSiteName = signal('');
  readonly submitError = signal<string | null>(null);

  // ── Employee dropdown ─────────────────────────────────────────────────────
  readonly allEmployees = signal<EmployeeListItemDto[]>([]);
  readonly employeeSearch = signal('');
  readonly employeeDropdownOpen = signal(false);
  readonly selectedEmployees = signal<EmployeeListItemDto[]>([]);
  readonly filteredEmployees = computed(() => {
    const q = this.employeeSearch().toLowerCase();
    return this.allEmployees().filter((e) =>
      e.employeeName.toLowerCase().includes(q) &&
      !this.selectedEmployees().some((s) => s.id === e.id)
    );
  });

  // ── Region dropdown ───────────────────────────────────────────────────────
  readonly regionSearch = signal('');
  readonly regionDropdownOpen = signal(false);
  readonly selectedRegion = signal<RegionDto | null>(null);
  readonly filteredRegions = () =>
    this.regions().filter((r) =>
      r.name.toLowerCase().includes(this.regionSearch().toLowerCase()),
    );

  // ── Operational days ──────────────────────────────────────────────────────
  readonly operationalDays = signal<DayHour[]>(
    DAY_KEYS.map((k, i) => ({
      dayIndex: i,
      labelKey: `SITE_MANAGEMENT.DAYS.${k}`,
      isOpen: false,
      from: '00:00',
      to: '00:00',
    })),
  );

  // ── Map modal ─────────────────────────────────────────────────────────────
  readonly showMapModal = signal(false);
  readonly mapSearchAddress = signal('');
  readonly mapSelectedAddress = signal('');
  readonly mapLat = signal(30.0444);
  readonly mapLng = signal(31.2357);
  private map: any = null;
  private marker: any = null;

  // ── Reactive form ─────────────────────────────────────────────────────────
  readonly form = this.fb.group({
    siteName: ['', [Validators.required, Validators.minLength(2)]],
    address: [''],
    phoneNumber: [''],
    macAddress: [''],
    instructions: [''],
  });

  ngOnInit(): void {
    this.siteService
      .getRegions()
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (r) => this.regions.set(r), error: () => {} });

    this.siteService
      .getEmployees()
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: (res) => this.allEmployees.set(res.items || []), error: () => {} });
  }

  ngAfterViewInit(): void {}

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    if (this.map) {
      this.map.remove();
      this.map = null;
    }
  }

  // ── Region dropdown ───────────────────────────────────────────────────────
  toggleRegionDropdown(): void {
    this.regionDropdownOpen.update((v) => !v);
  }

  selectRegion(r: RegionDto): void {
    this.selectedRegion.set(r);
    this.regionDropdownOpen.set(false);
    this.regionSearch.set('');
  }

  // ── Employee dropdown ─────────────────────────────────────────────────────
  toggleEmployeeDropdown(): void {
    this.employeeDropdownOpen.update((v) => !v);
  }

  selectEmployee(e: EmployeeListItemDto): void {
    this.selectedEmployees.update((curr) => [...curr, e]);
    this.employeeSearch.set('');
  }

  removeEmployee(id: number): void {
    this.selectedEmployees.update((curr) => curr.filter((e) => e.id !== id));
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.regionDropdownOpen.set(false);
    this.employeeDropdownOpen.set(false);
  }

  // ── Day toggle ─────────────────────────────────────────────────────────────
  toggleDay(dayIndex: number): void {
    this.operationalDays.update((days) =>
      days.map((d) => (d.dayIndex === dayIndex ? { ...d, isOpen: !d.isOpen } : d)),
    );
  }

  updateDayTime(dayIndex: number, field: 'from' | 'to', value: string): void {
    this.operationalDays.update((days) =>
      days.map((d) => (d.dayIndex === dayIndex ? { ...d, [field]: value } : d)),
    );
  }

  // ── Map modal ─────────────────────────────────────────────────────────────
  openMapModal(): void {
    this.mapSearchAddress.set(this.form.get('address')?.value || '');
    this.mapSelectedAddress.set(this.form.get('address')?.value || '');
    this.showMapModal.set(true);
    // Init map after modal is rendered
    setTimeout(() => {
      this.initMap();
      if (this.map) {
        setTimeout(() => this.map.invalidateSize(), 150);
      }
    }, 200);
  }

  closeMapModal(): void {
    this.showMapModal.set(false);
    if (this.map) {
      this.map.remove();
      this.map = null;
      this.marker = null;
    }
  }

  confirmMapLocation(): void {
    this.form.patchValue({ address: this.mapSelectedAddress() });
    this.closeMapModal();
  }

  /**
   * ──────────────────────────────────────────────────────────────────────────
   * HOW THE MAP WORKS
   * ──────────────────────────────────────────────────────────────────────────
   * We use Leaflet.js loaded via CDN script tag (no npm install needed).
   * Leaflet is a lightweight open-source map library (unlike Google Maps, it's
   * completely free and needs no API key).
   *
   * 1. The map is rendered inside a <div> inside the modal.
   * 2. We initialize a Leaflet map pointing to OpenStreetMap tiles.
   * 3. A draggable red marker is placed at the default/current coordinates.
   * 4. When the user drags the marker, we reverse-geocode the coordinates
   *    using the free Nominatim (OpenStreetMap) API to get the address string.
   * 5. The user can also type an address in the search box, which forward-
   *    geocodes it via Nominatim and moves the map + marker.
   * 6. On "Confirm", we store the address string and lat/lng in the form.
   * ──────────────────────────────────────────────────────────────────────────
   */
  private initMap(): void {
    const L = (window as any)['L'];
    if (!L) {
      console.warn('Leaflet not loaded. Make sure the CDN script is in index.html.');
      return;
    }

    const container = document.getElementById('leaflet-map-container');
    if (!container) return;

    // Destroy previous instance
    if (this.map) { this.map.remove(); this.map = null; }

    this.map = L.map(container).setView([this.mapLat(), this.mapLng()], 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors',
    }).addTo(this.map);

    // Custom red marker icon
    const icon = L.divIcon({
      html: `<svg xmlns="http://www.w3.org/2000/svg" width="32" height="40" viewBox="0 0 24 30">
               <path d="M12 0C7.58 0 4 3.58 4 8c0 5.25 8 16 8 16s8-10.75 8-16c0-4.42-3.58-8-8-8z"
                     fill="#e53e3e" stroke="#b91c1c" stroke-width="1"/>
               <circle cx="12" cy="8" r="3" fill="#fff"/>
             </svg>`,
      iconSize: [32, 40],
      iconAnchor: [16, 40],
      className: '',
    });

    this.marker = L.marker([this.mapLat(), this.mapLng()], {
      draggable: true,
      icon,
    }).addTo(this.map);

    // On marker drag end → reverse geocode
    this.marker.on('dragend', () => {
      const pos = this.marker.getLatLng();
      this.mapLat.set(pos.lat);
      this.mapLng.set(pos.lng);
      this.reverseGeocode(pos.lat, pos.lng);
    });

    // On map click → move marker
    this.map.on('click', (e: any) => {
      this.marker.setLatLng(e.latlng);
      this.mapLat.set(e.latlng.lat);
      this.mapLng.set(e.latlng.lng);
      this.reverseGeocode(e.latlng.lat, e.latlng.lng);
    });
  }

  private reverseGeocode(lat: number, lng: number): void {
    fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}`)
      .then((r) => r.json())
      .then((data) => {
        const address = data?.display_name || `${lat.toFixed(5)}, ${lng.toFixed(5)}`;
        this.mapSelectedAddress.set(address);
      })
      .catch(() => {
        this.mapSelectedAddress.set(`${lat.toFixed(5)}, ${lng.toFixed(5)}`);
      });
  }

  searchMapAddress(): void {
    const query = this.mapSearchAddress();
    if (!query.trim()) return;
    fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}`)
      .then((r) => r.json())
      .then((results: any[]) => {
        if (results.length > 0) {
          const { lat, lon, display_name } = results[0];
          const latN = parseFloat(lat);
          const lonN = parseFloat(lon);
          this.mapLat.set(latN);
          this.mapLng.set(lonN);
          this.mapSelectedAddress.set(display_name);
          if (this.map && this.marker) {
            this.map.setView([latN, lonN], 14);
            this.marker.setLatLng([latN, lonN]);
          }
        }
      })
      .catch(() => {});
  }

  // ── Submit ────────────────────────────────────────────────────────────────
  onSubmit(): void {
    if (this.form.invalid || !this.selectedRegion() || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);

    const hours: SiteOperationalHourDto[] = this.operationalDays()
      .filter((d) => d.isOpen)
      .map((d) => ({
        day: d.dayIndex,
        isOpen: true,
        from: d.from + ':00',
        to: d.to + ':00',
      }));

    const dto = {
      siteName: this.form.value.siteName!,
      latitude: this.mapLat(),
      longitude: this.mapLng(),
      mapUrl: `https://www.google.com/maps?q=${this.mapLat()},${this.mapLng()}`,
      address: this.form.value.address || undefined,
      phoneNumber: this.form.value.phoneNumber || undefined,
      macAddress: this.form.value.macAddress || undefined,
      macWhitelist: this.form.value.macAddress ? [this.form.value.macAddress] : [],
      instructions: this.form.value.instructions || undefined,
      regionId: this.selectedRegion()!.id,
      preferredEmployeeIds: this.selectedEmployees().map(e => e.id),
      operationalHours: hours,
    };

    this.siteService.createSite(dto).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.createdSiteName.set(this.form.value.siteName!);
        this.showSuccessModal.set(true);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        let backendMsg = null;
        if (err?.error?.errors && typeof err.error.errors === 'object') {
          // It's an ASP.NET validation error object
          backendMsg = Object.values(err.error.errors).flat().join(' | ');
        } else {
          backendMsg = err?.error?.message || (typeof err?.error === 'string' ? err.error : null);
        }
        this.submitError.set(backendMsg || this.translate.instant('SITE_MANAGEMENT.CREATE_ERROR'));
      },
    });
  }

  onDiscard(): void {
    this.router.navigate(['/sites']);
  }

  confirmSuccess(): void {
    this.showSuccessModal.set(false);
    this.router.navigate(['/sites']);
  }

  // ── Template helpers ──────────────────────────────────────────────────────
  isInvalid(ctrl: string): boolean {
    const c = this.form.get(ctrl);
    return !!(c && c.invalid && c.touched);
  }
}

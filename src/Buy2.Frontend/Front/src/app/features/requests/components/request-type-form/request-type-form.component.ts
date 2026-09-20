import { Component, inject, type OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { RequestTypeService } from '../../services/request-type.service';
import type { RequestType } from '../../models/request-type.model';

@Component({
  selector: 'app-request-type-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe, RouterLink],
  templateUrl: './request-type-form.component.html',
  styleUrls: ['./request-type-form.component.css']
})
export class RequestTypeFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private requestTypeService = inject(RequestTypeService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  requestForm!: FormGroup;
  isEditMode = false;
  requestId: string | null = null;
  showSuccessModal = false;

  ngOnInit(): void {
    this.initForm();
    this.requestId = this.route.snapshot.paramMap.get('id');
    if (this.requestId) {
      this.isEditMode = true;
      this.loadRequestType(this.requestId);
    }
  }

  initForm(): void {
    this.requestForm = this.fb.group({
      category: ['', Validators.required],
      name: ['', Validators.required],
      hint: [''],
      leaveType: ['Full'],
      leavePay: ['Paid']
    });
  }

  loadRequestType(id: string): void {
    this.requestTypeService.getRequestTypeById(id).subscribe({
      next: (data) => {
        this.requestForm.patchValue({
          category: data.category,
          name: data.name,
          hint: data.hint,
          leaveType: data.leaveType || 'Full',
          leavePay: data.leavePay || 'Paid'
        });
      },
      error: (err) => console.error('Error loading request type', err)
    });
  }

  onSubmit(): void {
    if (this.requestForm.invalid) {
      this.requestForm.markAllAsTouched();
      return;
    }

    const formData = this.requestForm.value;

    if (this.isEditMode && this.requestId) {
      this.requestTypeService.updateRequestType(this.requestId, formData).subscribe({
        next: () => {
          this.router.navigate(['/requests/types']);
        },
        error: (err) => console.error('Error updating request type', err)
      });
    } else {
      this.requestTypeService.createRequestType(formData).subscribe({
        next: () => {
          this.showSuccessModal = true;
        },
        error: (err) => console.error('Error creating request type', err)
      });
    }
  }

  closeModalAndNavigate(): void {
    this.showSuccessModal = false;
    this.router.navigate(['/requests/types']);
  }
}

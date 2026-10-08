import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RequestService } from '../../services/request.service';
import type { RequestDetails } from '../../models/request';

@Component({
  selector: 'app-request-decision',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslatePipe, ReactiveFormsModule, DatePipe],
  templateUrl: './request-decision.component.html',
  styleUrls: ['./request-decision.component.css']
})
export class RequestDecisionComponent implements OnInit {
  private requestService = inject(RequestService);
  private cdr = inject(ChangeDetectorRef);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  requestId: number | null = null;
  requestDetails: RequestDetails | null = null;
  decisionForm: FormGroup;
  isSubmitting = false;
  
  // Note: in the decision page do not implement the history feature do not follow figma in this part
  // So we skip displaying previousRequests here or history.

  constructor() {
    this.decisionForm = this.fb.group({
      decision: ['Approved', Validators.required],
      comment: [''],
      rejectionReason: [''] // visible if rejected
    });
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.requestId = Number(idParam);
      this.loadRequestDetails();
    }
  }

  loadRequestDetails(): void {
    if (this.requestId) {
      this.requestService.getRequestDetails(this.requestId).subscribe({
        next: (data) => {
          this.requestDetails = data;
          this.cdr.detectChanges();
        },
        error: (err) => console.error('Error loading request details', err)
      });
    }
  }

  onSubmitDecision(): void {
    if (this.decisionForm.valid && this.requestId) {
      this.isSubmitting = true;
      const payload = {
        tier: 'HR',
        ...this.decisionForm.value,
        reviewerId: 1
      };
      
      this.requestService.submitRequestDecision(this.requestId, payload).subscribe({
        next: () => {
          this.isSubmitting = false;
          this.router.navigate(['/requests/submitted']);
        },
        error: (err) => {
          this.isSubmitting = false;
          console.error('Error submitting decision', err);
          alert(err.error?.message || 'Error saving decision');
        }
      });
    }
  }
}

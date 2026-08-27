import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-task-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-modal.html',
  styleUrl: './task-modal.css',
})
export class TaskModal {
  @Input() taskName = 'Deadlines';
  @Input() description = 'Punctuality Score ...';
  @Input() steps = [
    '1. Lorem Ipsum is simply dummy text of the printing',
    '2. Lorem Ipsum is simply dummy text of the printing',
    '3. Lorem Ipsum is simply dummy text of the printing'
  ];
  @Input() repeat = '30';
  @Input() submissionTime = '30';
  @Input() file = 'document1.pdf';

  @Output() close = new EventEmitter<void>();
  @Output() delete = new EventEmitter<void>();
  @Output() edit = new EventEmitter<void>();
}

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-edit-task',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './edit-task.html',
  styleUrl: './edit-task.css',
})
export class EditTask {
  taskName = 'Deadlines';
  steps = [
    { id: 1, text: 'Lorem Ipsum is simply dummy text of the printing', editing: false },
    { id: 2, text: 'Lorem Ipsum is simply dummy text of the printing', editing: true },
    { id: 3, text: 'Lorem Ipsum is simply dummy text of the printing', editing: false }
  ];
  description = 'Punctuality score';
  file = 'document1.pdf';
  repeat = 'Daily';
  submissionTime = '04 / 09 / 2024';

  removeStep(index: number) {}
}

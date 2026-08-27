import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-job-employees',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './job-employees.html',
  styleUrl: './job-employees.css',
})
export class JobEmployees {
  employees = [
    { id: '0409', name: 'Ahmed Mohamed', email: 'ahmed.mohamed@mail.com', joinDate: '24-4-2024' },
    { id: '0409', name: 'Ahmed Mohamed', email: 'ahmed.mohamed@mail.com', joinDate: '24-4-2024' },
    { id: '0409', name: 'Ahmed Mohamed', email: 'ahmed.mohamed@mail.com', joinDate: '24-4-2024' },
    { id: '0409', name: 'Ahmed Mohamed', email: 'ahmed.mohamed@mail.com', joinDate: '24-4-2024' },
    { id: '0409', name: 'Ahmed Mohamed', email: 'ahmed.mohamed@mail.com', joinDate: '24-4-2024' },
  ];
}

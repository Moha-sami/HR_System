import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-add-user',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './add-user.component.html',
  styleUrls: ['./add-user.component.css']
})
export class AddUserComponent {
  constructor(private router: Router) {}

  user = {
    firstName: '',
    lastName: '',
    birthDate: '',
    email: '',
    role: '',
    gender: 'male'
  };

  roles = [
    'Admin',
    'HR Manager',
    'Employee',
    'Supervisor',
    'Recruiter'
  ];

  onSave() {
    console.log('Saving user:', this.user);
    // TODO: connect to API
  }

  onDiscard() {
    this.router.navigate(['/dashboard']);
  }
}

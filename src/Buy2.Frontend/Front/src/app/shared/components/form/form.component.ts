import { Component, input } from '@angular/core';

@Component({
  selector: 'app-form',
  imports:  [],
  templateUrl: './form.component.html',
  styleUrl: './form.component.css',
})
export class FormComponent {

  showHeader = input(true);
  showActions = input(true);

}


// to use it i have to import the form component first in the parent component.
// then create the formGroup and all of its controls and validations in parent.ts file

// then in the .html file of the parent component:

// <form
//   [formGroup]=""
//   (ngSubmit)="onSubmit()">


//   <app-form
//     [showHeader]="false/true" depends on the need
//     [showActions]="false/true" depends on the need>


//     <div form-header>
//       content of the header if needed
//     </div>

//     <div form-body>
//       content of the form and its inputs
//
//       example:
//       <input
//         type="text"
//         formControlName="name"
//       >
//     </div>


//     <div form-actions>
//       content of the actions if needed
//     </div>
//   </app-form>
// </form>
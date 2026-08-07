import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonComponent } from '../../../shared/components/button/button.component';

@Component({
  selector: 'app-button-docs',
  standalone: true,
  imports: [RouterLink, ButtonComponent],
  templateUrl: './button-docs.component.html',
  styleUrl: './button-docs.component.css',
})
export class ButtonDocsComponent {
  clickCount = 0;

  onButtonClick() {
    this.clickCount++;
  }
}

import { Component } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  selector: 'app-my-tickets-page',
  imports: [TranslatePipe],
  templateUrl: './my-tickets-page.component.html'
})
export class MyTicketsPageComponent {}

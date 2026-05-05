import { Component } from '@angular/core';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  selector: 'app-admin-users-page',
  imports: [TranslatePipe],
  templateUrl: './admin-users-page.component.html'
})
export class AdminUsersPageComponent {}

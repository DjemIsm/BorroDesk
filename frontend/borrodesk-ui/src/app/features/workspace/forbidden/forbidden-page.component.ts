import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  selector: 'app-forbidden-page',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './forbidden-page.component.html'
})
export class ForbiddenPageComponent {}

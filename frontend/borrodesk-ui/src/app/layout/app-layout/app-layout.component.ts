import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ApplicationRole, AuthService } from '../../core/auth/auth.service';
import { LanguageService } from '../../core/i18n/language.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { appNavigation } from './app-navigation';

@Component({
  selector: 'app-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, TranslatePipe],
  templateUrl: './app-layout.component.html',
  styleUrl: './app-layout.component.css'
})
export class AppLayoutComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly i18n = inject(LanguageService);

  protected readonly session = this.authService.session;
  protected readonly navigation = computed(() => {
    const currentSession = this.session();

    return appNavigation.filter((item) => {
      if (!item.roles?.length) {
        return true;
      }

      return item.roles.some((role) => currentSession?.roles.includes(role));
    });
  });
  protected readonly roleLabel = computed(() => {
    const roles = this.session()?.roles ?? [];

    return roles.length > 0
      ? roles.map((role) => this.i18n.translate(this.roleTranslationKey(role))).join(', ')
      : this.i18n.translate('common.noRole');
  });
  protected readonly userLabel = computed(() => {
    const currentSession = this.session();

    return currentSession?.userName || currentSession?.email || this.i18n.translate('common.borrodeskUser');
  });

  protected signOut(): void {
    this.authService.clearSession();
    void this.router.navigate(['/login']);
  }

  private roleTranslationKey(role: ApplicationRole): string {
    return `roles.${role.toLowerCase()}`;
  }
}

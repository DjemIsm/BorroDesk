import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { appNavigation } from './app-navigation';

@Component({
  selector: 'app-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app-layout.component.html',
  styleUrl: './app-layout.component.css'
})
export class AppLayoutComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

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
  protected readonly roleLabel = computed(() => this.session()?.roles.join(', ') || 'No role');
  protected readonly userLabel = computed(() => {
    const currentSession = this.session();

    return currentSession?.userName || currentSession?.email || 'BorroDesk user';
  });

  protected signOut(): void {
    this.authService.clearSession();
    void this.router.navigate(['/login']);
  }
}

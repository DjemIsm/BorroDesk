import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApplicationRole, AuthService, LoginResponse } from '../../core/auth/auth.service';
import { LanguageService } from '../../core/i18n/language.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';

interface ProblemDetails {
  detail?: string;
  title?: string;
}

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, TranslatePipe],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly i18n = inject(LanguageService);

  protected readonly errorMessage = signal('');
  protected readonly isSubmitting = signal(false);
  protected readonly session = signal<LoginResponse | null>(this.authService.getSession());
  protected readonly showPassword = signal(false);

  protected readonly loginForm = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    rememberMe: [true]
  });

  protected readonly userLabel = computed(() => {
    const currentSession = this.session();

    return currentSession?.userName || currentSession?.email || this.i18n.translate('common.borrodeskUser');
  });
  protected readonly roleLabel = computed(() => {
    const roles = this.session()?.roles ?? [];

    return roles.length > 0
      ? roles.map((role) => this.i18n.translate(this.roleTranslationKey(role))).join(', ')
      : this.i18n.translate('login.noRoles');
  });

  protected submit(): void {
    this.errorMessage.set('');

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    const { email, password, rememberMe } = this.loginForm.getRawValue();
    this.isSubmitting.set(true);

    this.authService.login({ email, password }, rememberMe).subscribe({
      next: (response) => {
        this.session.set(response);
        this.isSubmitting.set(false);
        void this.router.navigateByUrl(this.getReturnUrl());
      },
      error: (error: unknown) => {
        this.errorMessage.set(this.resolveLoginError(error));
        this.isSubmitting.set(false);
      }
    });
  }

  protected signOut(): void {
    this.authService.clearSession();
    this.session.set(null);
    this.loginForm.controls.password.reset('');
  }

  protected openWorkspace(): void {
    void this.router.navigateByUrl(this.getReturnUrl());
  }

  protected togglePasswordVisibility(): void {
    this.showPassword.update((value) => !value);
  }

  private resolveLoginError(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) {
        return this.i18n.translate('common.apiUnavailable');
      }

      const problemDetails = error.error as ProblemDetails | null;

      if (error.status === 401) {
        return problemDetails?.detail || this.i18n.translate('login.invalidCredentials');
      }

      return problemDetails?.detail || problemDetails?.title || this.i18n.translate('login.failed');
    }

    return this.i18n.translate('login.failed');
  }

  private getReturnUrl(): string {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');

    return returnUrl?.startsWith('/') ? returnUrl : '/app/dashboard';
  }

  private roleTranslationKey(role: ApplicationRole): string {
    return `roles.${role.toLowerCase()}`;
  }
}

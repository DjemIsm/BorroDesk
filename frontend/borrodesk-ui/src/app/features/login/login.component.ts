import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService, LoginResponse } from '../../core/auth/auth.service';

interface ProblemDetails {
  detail?: string;
  title?: string;
}

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

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

    return currentSession?.userName || currentSession?.email || 'BorroDesk user';
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
        return 'Cannot reach the BorroDesk API. Start the backend and try again.';
      }

      const problemDetails = error.error as ProblemDetails | null;

      if (error.status === 401) {
        return problemDetails?.detail || 'Invalid email or password.';
      }

      return problemDetails?.detail || problemDetails?.title || 'Sign in failed. Please try again.';
    }

    return 'Sign in failed. Please try again.';
  }

  private getReturnUrl(): string {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');

    return returnUrl?.startsWith('/') ? returnUrl : '/app/dashboard';
  }
}

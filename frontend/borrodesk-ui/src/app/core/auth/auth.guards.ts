import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { ApplicationRole, AuthService } from './auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);

  if (authService.getSession()) {
    return true;
  }

  return inject(Router).createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url }
  });
};

export const authChildGuard: CanActivateChildFn = (_route, state) => {
  const authService = inject(AuthService);

  if (authService.getSession()) {
    return true;
  }

  return inject(Router).createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url }
  });
};

export const loginRedirectGuard: CanActivateFn = () => {
  const authService = inject(AuthService);

  return authService.getSession() ? inject(Router).createUrlTree(['/app/dashboard']) : true;
};

export const roleGuard: CanActivateFn = (route) => {
  const allowedRoles = (route.data['roles'] ?? []) as readonly ApplicationRole[];

  return inject(AuthService).hasAnyRole(allowedRoles)
    ? true
    : inject(Router).createUrlTree(['/app/forbidden']);
};

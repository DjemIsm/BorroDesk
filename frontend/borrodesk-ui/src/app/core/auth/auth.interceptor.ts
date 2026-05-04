import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const session = inject(AuthService).getSession();

  if (!session?.accessToken) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: {
        Authorization: `${session.tokenType || 'Bearer'} ${session.accessToken}`
      }
    })
  );
};

import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { CsrfTokenService } from '../auth/csrf-token.service';
import { ApiErrorService } from '../error-handling/api-error.service';
import { getApiProblem } from '../error-handling/api-errors';

export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const csrf = inject(CsrfTokenService);
  const errors = inject(ApiErrorService);
  const router = inject(Router);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        const isLogin = request.url.endsWith('/api/v1/auth/login');
        const isCsrfFailure =
          error.status === 400 &&
          (getApiProblem(error)?.code === 'auth.csrf_invalid' ||
            getApiProblem(error)?.title === 'Invalid CSRF token');

        if (error.status === 401) {
          auth.clearSession();
          csrf.clear();
          if (!isLogin) {
            void router.navigate(['/login']);
          }
        } else if (
          !isCsrfFailure &&
          (error.status === 0 || error.status === 403 || error.status >= 500)
        ) {
          errors.notify(error);
        }
      }
      return throwError(() => error);
    }),
  );
};

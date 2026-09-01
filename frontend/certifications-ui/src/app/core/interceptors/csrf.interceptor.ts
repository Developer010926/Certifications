import {
  HttpContextToken,
  HttpErrorResponse,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { CsrfTokenService } from '../auth/csrf-token.service';
import { getApiProblem } from '../error-handling/api-errors';

const CSRF_RETRIED = new HttpContextToken<boolean>(() => false);
const UNSAFE_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

function isInvalidCsrf(error: unknown): boolean {
  if (!(error instanceof HttpErrorResponse) || error.status !== 400) {
    return false;
  }
  const problem = getApiProblem(error);
  return problem?.code === 'auth.csrf_invalid' || problem?.title === 'Invalid CSRF token';
}

function withToken(request: HttpRequest<unknown>, token: string, retried: boolean) {
  return request.clone({
    setHeaders: { 'X-CSRF-TOKEN': token },
    context: request.context.set(CSRF_RETRIED, retried),
  });
}

export const csrfInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const csrf = inject(CsrfTokenService);
  const isUnsafeApiRequest =
    request.url.startsWith('/api/') && UNSAFE_METHODS.has(request.method.toUpperCase());

  if (
    !isUnsafeApiRequest ||
    request.url.endsWith('/api/v1/auth/login') ||
    !auth.isAuthenticated()
  ) {
    return next(request);
  }

  return csrf.getToken().pipe(
    switchMap((token) => next(withToken(request, token, false))),
    catchError((error: unknown) => {
      if (!request.context.get(CSRF_RETRIED) && isInvalidCsrf(error)) {
        return csrf
          .getToken(true)
          .pipe(switchMap((token) => next(withToken(request, token, true))));
      }
      return throwError(() => error);
    }),
  );
};

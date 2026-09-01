import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../auth/auth.service';

function withUser(
  decision: (isAdmin: boolean, router: Router) => ReturnType<Router['createUrlTree']> | boolean,
) {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.ensureLoaded().pipe(
    map((user) => (user ? decision(user.isAdmin, router) : router.createUrlTree(['/login']))),
    catchError(() => of(router.createUrlTree(['/login'], { queryParams: { unavailable: true } }))),
  );
}

export const authGuard: CanActivateFn = () => withUser(() => true);

export const activeContractGuard: CanActivateFn = () => withUser(() => true);

export const adminGuard: CanActivateFn = () =>
  withUser((isAdmin, router) => (isAdmin ? true : router.createUrlTree(['/me'])));

export const anonymousOnlyGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.ensureLoaded().pipe(
    map((user) => {
      if (!user) {
        return true;
      }
      return router.createUrlTree([user.isAdmin ? '/select-mode' : '/me']);
    }),
    catchError(() => of(true)),
  );
};

export const defaultRedirectGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.ensureLoaded().pipe(
    map((user) => router.createUrlTree([!user ? '/login' : user.isAdmin ? '/select-mode' : '/me'])),
    catchError(() => of(router.createUrlTree(['/login'], { queryParams: { unavailable: true } }))),
  );
};

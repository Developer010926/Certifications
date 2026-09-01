import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { firstValueFrom, Observable, of } from 'rxjs';
import type { CurrentUserDto } from '../api/generated/certificationsApiV1.schemas';
import { AuthService } from '../auth/auth.service';
import { adminGuard, anonymousOnlyGuard, defaultRedirectGuard } from './auth.guards';

const admin: CurrentUserDto = {
  employeeId: '1',
  personalId: 'A1',
  firstName: 'Ada',
  lastName: 'Admin',
  displayName: 'Ada Admin',
  isAdmin: true,
  preferredAdminMode: 'Administration',
};
const employee: CurrentUserDto = { ...admin, isAdmin: false, preferredAdminMode: null };
const route = {} as ActivatedRouteSnapshot;
const state = {} as RouterStateSnapshot;

async function runGuard(guard: typeof adminGuard): Promise<boolean | UrlTree> {
  const result = TestBed.runInInjectionContext(() => guard(route, state)) as Observable<
    boolean | UrlTree
  >;
  return firstValueFrom(result);
}

describe('authentication guards and routing', () => {
  it('allows administrators through the administrator guard', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { ensureLoaded: () => of(admin) } },
      ],
    });
    expect(await runGuard(adminGuard)).toBe(true);
  });

  it('redirects non-administrators to their personal page', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { ensureLoaded: () => of(employee) } },
      ],
    });
    expect(TestBed.inject(Router).serializeUrl((await runGuard(adminGuard)) as UrlTree)).toBe(
      '/me',
    );
  });

  it('routes authenticated administrators away from login to mode selection', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { ensureLoaded: () => of(admin) } },
      ],
    });
    expect(
      TestBed.inject(Router).serializeUrl((await runGuard(anonymousOnlyGuard)) as UrlTree),
    ).toBe('/select-mode');
  });

  it('routes anonymous users to login from the root', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { ensureLoaded: () => of(null) } },
      ],
    });
    expect(
      TestBed.inject(Router).serializeUrl((await runGuard(defaultRedirectGuard)) as UrlTree),
    ).toBe('/login');
  });
});

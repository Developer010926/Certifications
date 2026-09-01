import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, of, shareReplay, tap, throwError } from 'rxjs';
import { AuthenticationService as GeneratedAuthenticationService } from '../api/generated/authentication/authentication.service';
import type {
  CurrentUserDto,
  LoginRequest,
  PreferredModeRequestPreferredMode,
} from '../api/generated/certificationsApiV1.schemas';

export type AuthenticationState = 'unknown' | 'anonymous' | 'authenticated';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(GeneratedAuthenticationService);
  private readonly userSignal = signal<CurrentUserDto | null>(null);
  private readonly stateSignal = signal<AuthenticationState>('unknown');
  private restoreRequest?: Observable<CurrentUserDto | null>;

  readonly currentUser = this.userSignal.asReadonly();
  readonly state = this.stateSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.stateSignal() === 'authenticated');
  readonly isAdmin = computed(() => this.userSignal()?.isAdmin === true);

  ensureLoaded(): Observable<CurrentUserDto | null> {
    if (this.stateSignal() !== 'unknown') {
      return of(this.userSignal());
    }
    if (this.restoreRequest) {
      return this.restoreRequest;
    }

    this.restoreRequest = this.api.getCurrentUser().pipe(
      tap((user) => this.setUser(user)),
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
          this.clearSession();
          return of(null);
        }
        return throwError(() => error);
      }),
      finalize(() => (this.restoreRequest = undefined)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    return this.restoreRequest;
  }

  login(request: LoginRequest): Observable<CurrentUserDto> {
    return this.api.login(request).pipe(tap((user) => this.setUser(user)));
  }

  savePreferredMode(mode: PreferredModeRequestPreferredMode): Observable<void> {
    return this.api.setPreferredAdminMode({ preferredMode: mode }).pipe(
      tap(() => {
        const user = this.userSignal();
        if (user) {
          this.userSignal.set({ ...user, preferredAdminMode: mode });
        }
      }),
    );
  }

  logout(): Observable<void> {
    return this.api.logout().pipe(finalize(() => this.clearSession()));
  }

  setUser(user: CurrentUserDto): void {
    this.userSignal.set(user);
    this.stateSignal.set('authenticated');
  }

  clearSession(): void {
    this.userSignal.set(null);
    this.stateSignal.set('anonymous');
  }
}

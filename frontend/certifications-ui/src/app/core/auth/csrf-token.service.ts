import { Injectable, inject, signal } from '@angular/core';
import { Observable, finalize, map, of, shareReplay, tap } from 'rxjs';
import { AuthenticationService as GeneratedAuthenticationService } from '../api/generated/authentication/authentication.service';

@Injectable({ providedIn: 'root' })
export class CsrfTokenService {
  private readonly api = inject(GeneratedAuthenticationService);
  private readonly tokenSignal = signal<string | null>(null);
  private request?: Observable<string>;

  getToken(forceRefresh = false): Observable<string> {
    if (forceRefresh) {
      this.tokenSignal.set(null);
    }
    const token = this.tokenSignal();
    if (token) {
      return of(token);
    }
    if (this.request) {
      return this.request;
    }

    this.request = this.api.getCsrfToken().pipe(
      map((response) => response.requestToken),
      tap((value) => this.tokenSignal.set(value)),
      finalize(() => (this.request = undefined)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    return this.request;
  }

  clear(): void {
    this.tokenSignal.set(null);
    this.request = undefined;
  }
}

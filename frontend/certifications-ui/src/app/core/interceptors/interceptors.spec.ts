import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { CsrfTokenService } from '../auth/csrf-token.service';
import { ApiErrorService } from '../error-handling/api-error.service';
import { apiErrorInterceptor } from './api-error.interceptor';
import { credentialsInterceptor } from './credentials.interceptor';
import { csrfInterceptor } from './csrf.interceptor';

describe('API request interceptors', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('adds withCredentials to API requests only', () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([credentialsInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    const http = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);
    http.get('/api/v1/auth/me').subscribe();
    expect(controller.expectOne('/api/v1/auth/me').request.withCredentials).toBe(true);
    http.get('/assets/config.json').subscribe();
    expect(controller.expectOne('/assets/config.json').request.withCredentials).toBe(false);
    controller.verify();
  });

  it('adds the in-memory CSRF token to authenticated unsafe requests', () => {
    const csrf = { getToken: vi.fn(() => of('request-token')), clear: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([credentialsInterceptor, csrfInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { isAuthenticated: () => true } },
        { provide: CsrfTokenService, useValue: csrf },
      ],
    });
    const http = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);
    http.patch('/api/v1/employees/id', {}).subscribe();
    const request = controller.expectOne('/api/v1/employees/id');
    expect(request.request.headers.get('X-CSRF-TOKEN')).toBe('request-token');
    expect(request.request.withCredentials).toBe(true);
    request.flush({});
    controller.verify();
  });

  it('does not add CSRF to reads or login', () => {
    const csrf = { getToken: vi.fn(() => of('request-token')), clear: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([csrfInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { isAuthenticated: () => true } },
        { provide: CsrfTokenService, useValue: csrf },
      ],
    });
    const http = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);
    http.get('/api/v1/employees').subscribe();
    expect(controller.expectOne('/api/v1/employees').request.headers.has('X-CSRF-TOKEN')).toBe(
      false,
    );
    http.post('/api/v1/auth/login', {}).subscribe();
    expect(controller.expectOne('/api/v1/auth/login').request.headers.has('X-CSRF-TOKEN')).toBe(
      false,
    );
    expect(csrf.getToken).not.toHaveBeenCalled();
  });

  it('refreshes an invalid CSRF token and retries once', () => {
    const csrf = {
      getToken: vi.fn().mockReturnValueOnce(of('old-token')).mockReturnValueOnce(of('new-token')),
      clear: vi.fn(),
    };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([csrfInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { isAuthenticated: () => true } },
        { provide: CsrfTokenService, useValue: csrf },
      ],
    });
    const http = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);
    http.post('/api/v1/auth/logout', {}).subscribe();
    const first = controller.expectOne('/api/v1/auth/logout');
    expect(first.request.headers.get('X-CSRF-TOKEN')).toBe('old-token');
    first.flush(
      { title: 'Invalid CSRF token', code: 'auth.csrf_invalid' },
      { status: 400, statusText: 'Bad Request' },
    );
    const retry = controller.expectOne('/api/v1/auth/logout');
    expect(retry.request.headers.get('X-CSRF-TOKEN')).toBe('new-token');
    retry.flush(null);
    expect(csrf.getToken).toHaveBeenLastCalledWith(true);
  });

  it('clears in-memory security state and routes to login on 401', () => {
    const auth = { clearSession: vi.fn() };
    const csrf = { clear: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([apiErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
        { provide: CsrfTokenService, useValue: csrf },
        { provide: ApiErrorService, useValue: { notify: vi.fn() } },
      ],
    });
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const http = TestBed.inject(HttpClient);
    const controller = TestBed.inject(HttpTestingController);
    http.get('/api/v1/auth/me').subscribe({ error: () => undefined });
    controller.expectOne('/api/v1/auth/me').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(auth.clearSession).toHaveBeenCalled();
    expect(csrf.clear).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });
});

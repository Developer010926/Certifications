import { TestBed } from '@angular/core/testing';
import { Subject, firstValueFrom } from 'rxjs';
import { AuthenticationService as GeneratedAuthenticationService } from '../api/generated/authentication/authentication.service';
import type { CurrentUserDto } from '../api/generated/certificationsApiV1.schemas';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  it('shares one current-user request during a navigation cycle', async () => {
    const response = new Subject<CurrentUserDto>();
    const generated = { getCurrentUser: vi.fn(() => response.asObservable()) };
    TestBed.configureTestingModule({
      providers: [AuthService, { provide: GeneratedAuthenticationService, useValue: generated }],
    });
    const service = TestBed.inject(AuthService);
    const first = firstValueFrom(service.ensureLoaded());
    const second = firstValueFrom(service.ensureLoaded());
    response.next({
      employeeId: '1',
      personalId: 'EMP-1',
      firstName: 'Ada',
      lastName: 'Lovelace',
      displayName: 'Ada Lovelace',
      isAdmin: false,
      preferredAdminMode: null,
    });
    response.complete();
    expect((await first)?.personalId).toBe('EMP-1');
    expect((await second)?.personalId).toBe('EMP-1');
    expect(generated.getCurrentUser).toHaveBeenCalledTimes(1);
  });
});

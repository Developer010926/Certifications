import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PreferredModeRequestPreferredMode } from '../../core/api/generated/certificationsApiV1.schemas';
import { AuthService } from '../../core/auth/auth.service';
import { ApiErrorService } from '../../core/error-handling/api-error.service';
import { ModeSelectionComponent } from './mode-selection.component';

describe('ModeSelectionComponent', () => {
  it('saves the administrator selection before routing to the selected mode', () => {
    const auth = {
      currentUser: () => ({ preferredAdminMode: 'Administration' }),
      savePreferredMode: vi.fn(() => of(undefined)),
    };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: auth },
        { provide: ApiErrorService, useValue: { message: vi.fn() } },
      ],
    });
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const component = TestBed.runInInjectionContext(() => new ModeSelectionComponent());
    component.select(PreferredModeRequestPreferredMode.MyPage);
    expect(auth.savePreferredMode).toHaveBeenCalledWith('MyPage');
    expect(navigate).toHaveBeenCalledWith(['/me']);
  });
});

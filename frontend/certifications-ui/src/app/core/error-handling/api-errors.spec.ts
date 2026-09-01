import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiErrorService } from './api-error.service';
import { applyValidationErrors } from './api-errors';

describe('central API error handling', () => {
  it('localizes a known concurrency conflict without exposing English backend details', () => {
    TestBed.configureTestingModule({
      providers: [ApiErrorService, { provide: MatSnackBar, useValue: { open: vi.fn() } }],
    });
    const service = TestBed.inject(ApiErrorService);
    const error = new HttpErrorResponse({
      status: 409,
      error: {
        title: 'Conflict',
        detail: 'The contract was changed by another request.',
        code: 'contract.concurrency_conflict',
      },
    });
    expect(service.message(error)).toContain('Контракт был изменён другим пользователем');
    expect(service.message(error)).not.toContain('changed by another request');
  });

  it('uses a recoverable message for network failures', () => {
    TestBed.configureTestingModule({
      providers: [ApiErrorService, { provide: MatSnackBar, useValue: { open: vi.fn() } }],
    });
    expect(TestBed.inject(ApiErrorService).message(new HttpErrorResponse({ status: 0 }))).toContain(
      'Не удалось связаться с сервером',
    );
  });

  it('maps nested validation ProblemDetails fields to form controls', () => {
    const form = new FormGroup({
      firstContract: new FormGroup({ position: new FormControl('') }),
    });
    const error = new HttpErrorResponse({
      status: 400,
      error: { errors: { 'FirstContract.Position': ['The value is required.'] } },
    });
    expect(applyValidationErrors(form, error)).toBe(true);
    expect(form.get('firstContract.position')?.getError('server')).toBe(
      'Поле обязательно для заполнения.',
    );
  });

  it('localizes field-specific server validation without exposing its English text', () => {
    const form = new FormGroup({
      prolongationAlertMonths: new FormControl(1),
    });
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        errors: {
          ProlongationAlertMonths: ['Alert months must be below warning months.'],
        },
      },
    });
    expect(applyValidationErrors(form, error)).toBe(true);
    expect(form.controls.prolongationAlertMonths.getError('server')).toBe(
      'Проверьте критический период.',
    );
  });

  it('uses a Russian fallback for unknown server details and validation messages', () => {
    TestBed.configureTestingModule({
      providers: [ApiErrorService, { provide: MatSnackBar, useValue: { open: vi.fn() } }],
    });
    const service = TestBed.inject(ApiErrorService);
    const problem = new HttpErrorResponse({
      status: 418,
      error: { title: 'Unknown', detail: 'English detail from the server.' },
    });
    expect(service.message(problem)).toBe('Произошла ошибка. Повторите попытку.');

    const form = new FormGroup({ position: new FormControl('') });
    const validation = new HttpErrorResponse({
      status: 400,
      error: { errors: { Position: ['A future validation message.'] } },
    });
    expect(applyValidationErrors(form, validation)).toBe(true);
    expect(form.controls.position.getError('server')).toBe('Некорректное значение.');
  });
});

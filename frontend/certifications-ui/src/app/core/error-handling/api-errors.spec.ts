import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiErrorService } from './api-error.service';
import { applyValidationErrors } from './api-errors';

describe('central API error handling', () => {
  it('presents concurrency conflict details with a safe fallback', () => {
    TestBed.configureTestingModule({
      providers: [ApiErrorService, { provide: MatSnackBar, useValue: { open: vi.fn() } }],
    });
    const service = TestBed.inject(ApiErrorService);
    const error = new HttpErrorResponse({
      status: 409,
      error: { title: 'Conflict', detail: 'The contract was changed by another request.' },
    });
    expect(service.message(error)).toContain('changed by another request');
  });

  it('uses a recoverable message for network failures', () => {
    TestBed.configureTestingModule({
      providers: [ApiErrorService, { provide: MatSnackBar, useValue: { open: vi.fn() } }],
    });
    expect(TestBed.inject(ApiErrorService).message(new HttpErrorResponse({ status: 0 }))).toContain(
      'cannot be reached',
    );
  });

  it('maps nested validation ProblemDetails fields to form controls', () => {
    const form = new FormGroup({
      firstContract: new FormGroup({ position: new FormControl('') }),
    });
    const error = new HttpErrorResponse({
      status: 400,
      error: { errors: { 'FirstContract.Position': ['Position is required.'] } },
    });
    expect(applyValidationErrors(form, error)).toBe(true);
    expect(form.get('firstContract.position')?.getError('server')).toBe('Position is required.');
  });
});

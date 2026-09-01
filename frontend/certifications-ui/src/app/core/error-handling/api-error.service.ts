import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UI_TEXT } from '../../shared/utilities/ui-text';
import { getApiProblem } from './api-errors';

@Injectable({ providedIn: 'root' })
export class ApiErrorService {
  private readonly snackBar = inject(MatSnackBar);

  message(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return UI_TEXT.genericError;
    }
    if (error.status === 0) {
      return UI_TEXT.networkError;
    }
    const problem = getApiProblem(error);
    if (error.status === 403) {
      return UI_TEXT.forbidden;
    }
    if (error.status === 409) {
      return problem?.detail || UI_TEXT.conflict;
    }
    return problem?.detail || problem?.title || UI_TEXT.genericError;
  }

  notify(error: unknown): void {
    this.snackBar.open(this.message(error), 'Dismiss', { duration: 6000 });
  }
}

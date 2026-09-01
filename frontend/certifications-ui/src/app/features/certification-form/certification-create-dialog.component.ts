import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatDatepickerIntl } from '@angular/material/datepicker';
import { finalize } from 'rxjs';
import { CertificationsService } from '../../core/api/generated/certifications/certifications.service';
import type { CertificationDto } from '../../core/api/generated/certificationsApiV1.schemas';
import { ApiErrorService } from '../../core/error-handling/api-error.service';
import { applyValidationErrors } from '../../core/error-handling/api-errors';
import { createRussianDatepickerIntl } from '../../core/localization/russian-material-intl';
import { MATERIAL_IMPORTS } from '../../shared/material/material-imports';
import { toDateOnly } from '../../shared/utilities/date-only';

@Component({
  selector: 'app-certification-create-dialog',
  imports: [ReactiveFormsModule, ...MATERIAL_IMPORTS],
  providers: [{ provide: MatDatepickerIntl, useFactory: createRussianDatepickerIntl }],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Создание сертификации</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="dialog-form" (ngSubmit)="submit()">
        <mat-form-field appearance="outline"
          ><mat-label>Экзаменатор</mat-label><input matInput formControlName="assessor" />
          @if (form.controls.assessor.invalid && form.controls.assessor.touched) {
            <mat-error>Укажите экзаменатора.</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline"
          ><mat-label>Дата сертификации</mat-label
          ><input
            matInput
            [matDatepicker]="picker"
            formControlName="certificationDate"
          /><mat-datepicker-toggle matIconSuffix [for]="picker" /><mat-datepicker #picker />
          @if (form.controls.certificationDate.invalid && form.controls.certificationDate.touched) {
            <mat-error>Укажите дату сертификации.</mat-error>
          }
        </mat-form-field>
      </form>
      @if (errorMessage()) {
        <p class="form-error" role="alert">{{ errorMessage() }}</p>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end"
      ><button mat-button type="button" [disabled]="submitting()" (click)="dialogRef.close()">
        Отмена</button
      ><button mat-flat-button type="button" [disabled]="submitting()" (click)="submit()">
        {{ submitting() ? 'Создание…' : 'Создать' }}
      </button></mat-dialog-actions
    >
  `,
})
export class CertificationCreateDialogComponent {
  readonly contractId = inject<number>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(
    MatDialogRef<CertificationCreateDialogComponent, CertificationDto | undefined>,
  );
  private readonly certifications = inject(CertificationsService);
  private readonly errors = inject(ApiErrorService);
  readonly submitting = signal(false);
  readonly errorMessage = signal('');
  readonly form = new FormGroup({
    assessor: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    certificationDate: new FormControl<Date | null>(null, { validators: [Validators.required] }),
  });
  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const date = toDateOnly(value.certificationDate);
    if (!date) return;
    this.submitting.set(true);
    this.errorMessage.set('');
    this.certifications
      .createCertification(this.contractId, {
        assessor: value.assessor.trim(),
        certificationDate: date,
      })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (result) => this.dialogRef.close(result),
        error: (error: unknown) => {
          if (!applyValidationErrors(this.form, error))
            this.errorMessage.set(this.errors.message(error));
        },
      });
  }
}

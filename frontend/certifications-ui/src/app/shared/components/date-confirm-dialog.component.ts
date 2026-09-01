import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatDatepickerIntl } from '@angular/material/datepicker';
import { createRussianDatepickerIntl } from '../../core/localization/russian-material-intl';
import { MATERIAL_IMPORTS } from '../material/material-imports';
import { toDateOnly } from '../utilities/date-only';

export interface DateConfirmDialogData {
  title: string;
  description: string;
  label: string;
  confirmLabel: string;
  initialDate?: Date;
}

@Component({
  selector: 'app-date-confirm-dialog',
  imports: [ReactiveFormsModule, ...MATERIAL_IMPORTS],
  providers: [{ provide: MatDatepickerIntl, useFactory: createRussianDatepickerIntl }],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p>{{ data.description }}</p>
      <mat-form-field appearance="outline">
        <mat-label>{{ data.label }}</mat-label>
        <input matInput [matDatepicker]="picker" [formControl]="date" />
        <mat-datepicker-toggle matIconSuffix [for]="picker" /><mat-datepicker #picker />
        @if (date.invalid && date.touched) {
          <mat-error>Укажите дату.</mat-error>
        }
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end"
      ><button mat-button type="button" (click)="dialogRef.close()">Отмена</button
      ><button mat-flat-button type="button" (click)="confirm()">
        {{ data.confirmLabel }}
      </button></mat-dialog-actions
    >
  `,
})
export class DateConfirmDialogComponent {
  readonly data = inject<DateConfirmDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<DateConfirmDialogComponent, string | undefined>);
  readonly date = new FormControl<Date | null>(this.data.initialDate ?? new Date(), {
    validators: [Validators.required],
  });
  confirm(): void {
    if (this.date.invalid) {
      this.date.markAsTouched();
      return;
    }
    this.dialogRef.close(toDateOnly(this.date.value) ?? undefined);
  }
}

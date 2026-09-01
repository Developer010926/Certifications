import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { controlError } from '../../core/error-handling/api-errors';
import { MATERIAL_IMPORTS } from '../material/material-imports';

@Component({
  selector: 'app-contract-form-fields',
  imports: [ReactiveFormsModule, ...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [formGroup]="form()" class="form-grid">
      <mat-form-field appearance="outline"
        ><mat-label>Position</mat-label><input matInput formControlName="position" />
        @if (field('position')?.invalid && field('position')?.touched) {
          <mat-error>{{ error(field('position'), 'Position') }}</mat-error>
        }
      </mat-form-field>
      <mat-form-field appearance="outline"
        ><mat-label>Department</mat-label><input matInput formControlName="department"
      /></mat-form-field>
      <mat-form-field appearance="outline"
        ><mat-label>Division</mat-label><input matInput formControlName="division"
      /></mat-form-field>
      <mat-form-field appearance="outline"
        ><mat-label>Contract date</mat-label
        ><input
          matInput
          [matDatepicker]="contractPicker"
          formControlName="contractDate"
        /><mat-datepicker-toggle matIconSuffix [for]="contractPicker" /><mat-datepicker
          #contractPicker
        />
        @if (field('contractDate')?.invalid && field('contractDate')?.touched) {
          <mat-error>Contract date is required.</mat-error>
        }
      </mat-form-field>
      <mat-form-field appearance="outline"
        ><mat-label>Valid to</mat-label
        ><input
          matInput
          [matDatepicker]="validPicker"
          formControlName="validTo" /><mat-datepicker-toggle
          matIconSuffix
          [for]="validPicker" /><mat-datepicker #validPicker
      /></mat-form-field>
      <mat-form-field appearance="outline"
        ><mat-label>Warning months</mat-label
        ><input matInput type="number" min="0" formControlName="prolongationWarningMonths"
      /></mat-form-field>
      <mat-form-field appearance="outline"
        ><mat-label>Alert months</mat-label
        ><input matInput type="number" min="0" formControlName="prolongationAlertMonths"
      /></mat-form-field>
      <mat-form-field appearance="outline"
        ><mat-label>Prolongation years</mat-label
        ><input matInput type="number" min="1" formControlName="prolongationForYears"
      /></mat-form-field>
    </div>
    @if (form().hasError('thresholdOrder')) {
      <p class="form-error" role="alert">Alert months must be lower than warning months.</p>
    }
    @if (form().hasError('nonNegativeThresholds')) {
      <p class="form-error" role="alert">Warning and alert months cannot be negative.</p>
    }
    @if (form().hasError('positiveYears')) {
      <p class="form-error" role="alert">Prolongation years must be positive.</p>
    }
  `,
})
export class ContractFormFieldsComponent {
  readonly form = input.required<FormGroup>();
  readonly error = controlError;
  field(name: string) {
    return this.form().get(name);
  }
}

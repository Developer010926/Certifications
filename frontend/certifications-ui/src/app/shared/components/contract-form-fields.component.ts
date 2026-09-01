import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatDatepickerIntl } from '@angular/material/datepicker';
import { controlError } from '../../core/error-handling/api-errors';
import { createRussianDatepickerIntl } from '../../core/localization/russian-material-intl';
import { MATERIAL_IMPORTS } from '../material/material-imports';

@Component({
  selector: 'app-contract-form-fields',
  imports: [ReactiveFormsModule, ...MATERIAL_IMPORTS],
  providers: [{ provide: MatDatepickerIntl, useFactory: createRussianDatepickerIntl }],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div [formGroup]="form()" class="form-grid">
      <mat-form-field appearance="outline"
        ><mat-label>Должность</mat-label><input matInput formControlName="position" />
        @if (field('position')?.invalid && field('position')?.touched) {
          <mat-error>{{ error(field('position'), 'Должность') }}</mat-error>
        }
      </mat-form-field>
      <mat-form-field appearance="outline"
        ><mat-label>Подразделение</mat-label><input matInput formControlName="department"
      /></mat-form-field>
      <mat-form-field appearance="outline"
        ><mat-label>Организационная единица</mat-label><input matInput formControlName="division"
      /></mat-form-field>
      <mat-form-field appearance="outline"
        ><mat-label>Дата начала контракта</mat-label
        ><input
          matInput
          [matDatepicker]="contractPicker"
          formControlName="contractDate"
        /><mat-datepicker-toggle matIconSuffix [for]="contractPicker" /><mat-datepicker
          #contractPicker
        />
        @if (field('contractDate')?.invalid && field('contractDate')?.touched) {
          <mat-error>Дата начала контракта обязательна.</mat-error>
        }
      </mat-form-field>
      <mat-form-field appearance="outline"
        ><mat-label>Дата окончания</mat-label
        ><input
          matInput
          [matDatepicker]="validPicker"
          formControlName="validTo" /><mat-datepicker-toggle
          matIconSuffix
          [for]="validPicker" /><mat-datepicker #validPicker
      /></mat-form-field>
      <mat-form-field appearance="outline"
        ><mat-label>Период предупреждения (мес.)</mat-label
        ><input matInput type="number" min="0" formControlName="prolongationWarningMonths"
      /></mat-form-field>
      <mat-form-field appearance="outline"
        ><mat-label>Критический период (мес.)</mat-label
        ><input matInput type="number" min="0" formControlName="prolongationAlertMonths"
      /></mat-form-field>
      <mat-form-field appearance="outline"
        ><mat-label>Срок продления (лет)</mat-label
        ><input matInput type="number" min="1" formControlName="prolongationForYears"
      /></mat-form-field>
    </div>
    @if (form().hasError('thresholdOrder')) {
      <p class="form-error" role="alert">
        Критический период должен быть меньше периода предупреждения.
      </p>
    }
    @if (form().hasError('nonNegativeThresholds')) {
      <p class="form-error" role="alert">
        Период предупреждения и критический период не могут быть отрицательными.
      </p>
    }
    @if (form().hasError('positiveYears')) {
      <p class="form-error" role="alert">Срок продления должен быть больше нуля.</p>
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

import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import type { CreateEmployeeRequest } from '../../core/api/generated/certificationsApiV1.schemas';
import { EmployeesService } from '../../core/api/generated/employees/employees.service';
import { ApiErrorService } from '../../core/error-handling/api-error.service';
import { applyValidationErrors, controlError } from '../../core/error-handling/api-errors';
import { ContractFormFieldsComponent } from '../../shared/components/contract-form-fields.component';
import { PasswordDialogComponent } from '../../shared/components/password-dialog.component';
import { MATERIAL_IMPORTS } from '../../shared/material/material-imports';
import { createContractForm, toContractRequest } from '../../shared/utilities/contract-form';

export function buildCreateEmployeeForm() {
  const contractForm = createContractForm();
  const form = new FormGroup({
    personalId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    firstName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    middleName: new FormControl('', { nonNullable: true }),
    lastName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    isAdmin: new FormControl(false, { nonNullable: true }),
    firstContract: contractForm,
  });
  return { form, contractForm };
}

@Component({
  selector: 'app-employee-create',
  imports: [ReactiveFormsModule, RouterLink, ContractFormFieldsComponent, ...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="feature-page narrow-page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Сотрудники</p>
          <h1>Создание сотрудника и первого контракта</h1>
        </div>
        <a mat-button routerLink="/admin/users">Отмена</a>
      </header>
      <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <mat-card appearance="outlined" class="section-card">
          <mat-card-header><mat-card-title>Сотрудник</mat-card-title></mat-card-header>
          <mat-card-content class="form-grid">
            <mat-form-field appearance="outline"
              ><mat-label>Табельный номер</mat-label><input matInput formControlName="personalId" />
              @if (form.controls.personalId.invalid && form.controls.personalId.touched) {
                <mat-error>{{ error(form.controls.personalId, 'Табельный номер') }}</mat-error>
              }
            </mat-form-field>
            <mat-form-field appearance="outline"
              ><mat-label>Имя</mat-label><input matInput formControlName="firstName" />
              @if (form.controls.firstName.invalid && form.controls.firstName.touched) {
                <mat-error>{{ error(form.controls.firstName, 'Имя') }}</mat-error>
              }
            </mat-form-field>
            <mat-form-field appearance="outline"
              ><mat-label>Отчество</mat-label><input matInput formControlName="middleName"
            /></mat-form-field>
            <mat-form-field appearance="outline"
              ><mat-label>Фамилия</mat-label><input matInput formControlName="lastName" />
              @if (form.controls.lastName.invalid && form.controls.lastName.touched) {
                <mat-error>{{ error(form.controls.lastName, 'Фамилия') }}</mat-error>
              }
            </mat-form-field>
            <mat-checkbox formControlName="isAdmin">Администратор</mat-checkbox>
          </mat-card-content>
        </mat-card>
        <mat-card appearance="outlined" class="section-card">
          <mat-card-header><mat-card-title>Первый контракт</mat-card-title></mat-card-header>
          <mat-card-content><app-contract-form-fields [form]="contractForm" /></mat-card-content>
        </mat-card>
        @if (submitError()) {
          <p class="form-error" role="alert">{{ submitError() }}</p>
        }
        <div class="form-actions">
          <button mat-flat-button type="submit" [disabled]="submitting()">
            {{ submitting() ? 'Создание…' : 'Создать сотрудника' }}
          </button>
        </div>
      </form>
    </section>
  `,
})
export class EmployeeCreateComponent {
  private readonly employees = inject(EmployeesService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly errors = inject(ApiErrorService);
  private readonly employeeForms = buildCreateEmployeeForm();
  readonly error = controlError;
  readonly contractForm = this.employeeForms.contractForm;
  readonly form = this.employeeForms.form;
  readonly submitting = signal(false);
  readonly submitError = signal('');

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const request: CreateEmployeeRequest = {
      personalId: value.personalId.trim(),
      firstName: value.firstName.trim(),
      middleName: value.middleName.trim() || null,
      lastName: value.lastName.trim(),
      isAdmin: value.isAdmin,
      firstContract: toContractRequest(this.contractForm),
    };
    this.submitting.set(true);
    this.submitError.set('');
    this.employees
      .createEmployee(request)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (response) => {
          const employeeId = response.employee.employeeId;
          const data = {
            title: 'Сгенерированный пароль',
            description:
              'Пароль показывается один раз после создания. Скопируйте его в безопасное место перед закрытием окна.',
            password: response.generatedPassword,
          };
          response.generatedPassword = '';
          this.dialog
            .open(PasswordDialogComponent, { data, restoreFocus: true, disableClose: true })
            .afterClosed()
            .subscribe(() => void this.router.navigate(['/admin/users', employeeId]));
        },
        error: (error: unknown) => {
          if (!applyValidationErrors(this.form, error))
            this.submitError.set(this.errors.message(error));
        },
      });
  }
}

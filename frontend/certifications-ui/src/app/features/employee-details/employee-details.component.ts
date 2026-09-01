import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import type {
  EmployeeDetailsDto,
  UpdateEmployeeRequest,
} from '../../core/api/generated/certificationsApiV1.schemas';
import { EmployeesService } from '../../core/api/generated/employees/employees.service';
import { ApiErrorService } from '../../core/error-handling/api-error.service';
import { applyValidationErrors, controlError } from '../../core/error-handling/api-errors';
import { PasswordDialogComponent } from '../../shared/components/password-dialog.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge.component';
import { MATERIAL_IMPORTS } from '../../shared/material/material-imports';
import { DateOnlyDisplayPipe } from '../../shared/utilities/date-only-display.pipe';
import { UI_TEXT } from '../../shared/utilities/ui-text';

@Component({
  selector: 'app-employee-details',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DateOnlyDisplayPipe,
    StatusBadgeComponent,
    ...MATERIAL_IMPORTS,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="feature-page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Сотрудники</p>
          <h1>Данные сотрудника</h1>
        </div>
        <div class="header-actions">
          <button
            mat-stroked-button
            type="button"
            [disabled]="passwordBusy()"
            (click)="revealPassword()"
          >
            Показать пароль</button
          ><button
            mat-stroked-button
            type="button"
            [disabled]="passwordBusy()"
            (click)="generatePassword()"
          >
            Сгенерировать новый пароль
          </button>
        </div>
      </header>
      @if (loading()) {
        <mat-progress-bar mode="indeterminate" aria-label="Загрузка данных сотрудника" />
      } @else if (loadError()) {
        <div class="state-panel" role="alert">
          <p>{{ loadError() }}</p>
          <button mat-button type="button" (click)="load()">Обновить</button>
        </div>
      } @else if (employee(); as item) {
        <form [formGroup]="form" (ngSubmit)="save()" novalidate>
          <mat-card appearance="outlined" class="section-card">
            <mat-card-header><mat-card-title>Личные данные</mat-card-title></mat-card-header>
            <mat-card-content class="form-grid">
              <mat-form-field appearance="outline"
                ><mat-label>Табельный номер</mat-label
                ><input matInput formControlName="personalId" />
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
            <mat-card-actions align="end"
              ><button mat-flat-button type="submit" [disabled]="saving()">
                {{ saving() ? 'Сохранение…' : 'Сохранить данные' }}
              </button></mat-card-actions
            >
          </mat-card>
          @if (saveError()) {
            <p class="form-error" role="alert">{{ saveError() }}</p>
          }
        </form>

        <mat-card appearance="outlined" class="section-card">
          <mat-card-header
            ><mat-card-title>Текущий контракт</mat-card-title><span class="header-spacer"></span>
            @if (item.currentContract) {
              <app-status-badge [status]="item.currentContract.contract.status" />
            }
          </mat-card-header>
          <mat-card-content>
            @if (item.currentContract; as details) {
              <div class="details-grid">
                <div>
                  <span>Должность</span><strong>{{ details.contract.position }}</strong>
                </div>
                <div>
                  <span>Подразделение</span
                  ><strong>{{ details.contract.department || '—' }}</strong>
                </div>
                <div>
                  <span>Организационная единица</span
                  ><strong>{{ details.contract.division || '—' }}</strong>
                </div>
                <div>
                  <span>Дата начала контракта</span
                  ><strong>{{ details.contract.contractDate | dateOnly }}</strong>
                </div>
                <div>
                  <span>Расчётная дата окончания</span
                  ><strong>{{ details.contract.effectiveValidTo | dateOnly }}</strong>
                </div>
                <div>
                  <span>Сертификации</span><strong>{{ details.certifications.length }}</strong>
                </div>
              </div>
            } @else {
              <p class="empty-text">У сотрудника нет активного контракта.</p>
            }
          </mat-card-content>
          <mat-card-actions
            ><a mat-stroked-button [routerLink]="['/admin/users', employeeId, 'contract']">{{
              item.currentContract ? 'Управление контрактом' : 'Создать контракт'
            }}</a></mat-card-actions
          >
        </mat-card>

        @if (item.currentContract; as details) {
          <mat-card appearance="outlined" class="section-card">
            <mat-card-header><mat-card-title>История сертификаций</mat-card-title></mat-card-header>
            <mat-card-content>
              @if (details.certifications.length === 0) {
                <p class="empty-text">Сертификации отсутствуют.</p>
              } @else {
                <div
                  class="table-scroll"
                  role="region"
                  aria-label="История сертификаций"
                  tabindex="0"
                >
                  <table mat-table [dataSource]="details.certifications">
                    <ng-container matColumnDef="date"
                      ><th mat-header-cell *matHeaderCellDef>Дата сертификации</th>
                      <td mat-cell *matCellDef="let row">
                        {{ row.certificationDate | dateOnly }}
                      </td></ng-container
                    >
                    <ng-container matColumnDef="assessor"
                      ><th mat-header-cell *matHeaderCellDef>Экзаменатор</th>
                      <td mat-cell *matCellDef="let row">{{ row.assessor }}</td></ng-container
                    >
                    <ng-container matColumnDef="returned"
                      ><th mat-header-cell *matHeaderCellDef>Дата возврата</th>
                      <td mat-cell *matCellDef="let row">
                        {{
                          row.prolongationReturned
                            ? (row.prolongationReturned | dateOnly)
                            : 'В процессе'
                        }}
                      </td></ng-container
                    >
                    <ng-container matColumnDef="actions"
                      ><th mat-header-cell *matHeaderCellDef>
                        <span class="visually-hidden">Действия</span>
                      </th>
                      <td mat-cell *matCellDef="let row">
                        <a
                          mat-button
                          [routerLink]="['/admin/certifications', row.certificationId]"
                          [queryParams]="{ employeeId, contractId: details.contract.contractId }"
                          >Открыть</a
                        >
                      </td></ng-container
                    >
                    <tr mat-header-row *matHeaderRowDef="certificationColumns"></tr>
                    <tr mat-row *matRowDef="let row; columns: certificationColumns"></tr>
                  </table>
                </div>
              }
            </mat-card-content>
          </mat-card>
        }
      }
    </section>
  `,
})
export class EmployeeDetailsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly employees = inject(EmployeesService);
  private readonly errors = inject(ApiErrorService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  readonly employeeId = this.route.snapshot.paramMap.get('employeeId') ?? '';
  readonly certificationColumns = ['date', 'assessor', 'returned', 'actions'];
  readonly employee = signal<EmployeeDetailsDto | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly passwordBusy = signal(false);
  readonly loadError = signal('');
  readonly saveError = signal('');
  readonly error = controlError;
  readonly form = new FormGroup({
    personalId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    firstName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    middleName: new FormControl('', { nonNullable: true }),
    lastName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    isAdmin: new FormControl(false, { nonNullable: true }),
  });

  ngOnInit(): void {
    this.load();
  }
  load(): void {
    this.loading.set(true);
    this.loadError.set('');
    this.employees
      .getEmployee(this.employeeId)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (item) => {
          this.employee.set(item);
          this.form.reset({
            personalId: item.personalId,
            firstName: item.firstName,
            middleName: item.middleName ?? '',
            lastName: item.lastName,
            isAdmin: item.isAdmin,
          });
        },
        error: (error) => this.loadError.set(this.errors.message(error)),
      });
  }
  save(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const request: UpdateEmployeeRequest = {
      personalId: value.personalId.trim(),
      firstName: value.firstName.trim(),
      middleName: value.middleName.trim() || null,
      lastName: value.lastName.trim(),
      isAdmin: value.isAdmin,
    };
    this.saving.set(true);
    this.saveError.set('');
    this.employees
      .updateEmployee(this.employeeId, request)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (item) => {
          this.employee.set(item);
          this.snackBar.open(UI_TEXT.saved, undefined, { duration: 2500 });
        },
        error: (error: unknown) => {
          if (!applyValidationErrors(this.form, error))
            this.saveError.set(this.errors.message(error));
        },
      });
  }
  revealPassword(): void {
    this.passwordOperation('reveal');
  }
  generatePassword(): void {
    this.passwordOperation('generate');
  }
  private passwordOperation(kind: 'reveal' | 'generate'): void {
    if (this.passwordBusy()) return;
    this.passwordBusy.set(true);
    const request =
      kind === 'generate'
        ? this.employees.generateEmployeePassword(this.employeeId)
        : this.employees.revealEmployeePassword(this.employeeId);
    request.pipe(finalize(() => this.passwordBusy.set(false))).subscribe({
      next: (response) => {
        const data = {
          title: kind === 'generate' ? 'Новый сгенерированный пароль' : 'Пароль сотрудника',
          description:
            kind === 'generate'
              ? 'Предыдущий пароль больше не действует. Скопируйте новый пароль в безопасное место.'
              : 'Копируйте пароль только при явной необходимости.',
          password: response.password,
        };
        response.password = '';
        this.dialog.open(PasswordDialogComponent, { data, disableClose: true, restoreFocus: true });
      },
      error: (error) => this.errors.notify(error),
    });
  }
}

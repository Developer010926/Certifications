import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import type { EmployeeDetailsDto } from '../../core/api/generated/certificationsApiV1.schemas';
import { ContractsService } from '../../core/api/generated/contracts/contracts.service';
import { EmployeesService } from '../../core/api/generated/employees/employees.service';
import { ApiErrorService } from '../../core/error-handling/api-error.service';
import { applyValidationErrors } from '../../core/error-handling/api-errors';
import { CertificationCreateDialogComponent } from '../certification-form/certification-create-dialog.component';
import { ContractFormFieldsComponent } from '../../shared/components/contract-form-fields.component';
import { DateConfirmDialogComponent } from '../../shared/components/date-confirm-dialog.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge.component';
import { MATERIAL_IMPORTS } from '../../shared/material/material-imports';
import { createContractForm, toContractRequest } from '../../shared/utilities/contract-form';
import { DateOnlyDisplayPipe } from '../../shared/utilities/date-only-display.pipe';
import { UI_TEXT } from '../../shared/utilities/ui-text';

@Component({
  selector: 'app-contract-management',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    ContractFormFieldsComponent,
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
          <h1>Управление контрактом</h1>
        </div>
        <a mat-button [routerLink]="['/admin/users', employeeId]">К сотруднику</a>
      </header>
      @if (loading()) {
        <mat-progress-bar mode="indeterminate" aria-label="Загрузка контракта" />
      } @else if (errorMessage()) {
        <div class="state-panel" role="alert">
          <p>{{ errorMessage() }}</p>
          <button mat-button type="button" (click)="load()">Обновить</button>
        </div>
      } @else if (employee(); as item) {
        @if (item.currentContract; as details) {
          <mat-card appearance="outlined" class="section-card">
            <mat-card-header
              ><mat-card-title>Активный контракт</mat-card-title><span class="header-spacer"></span
              ><app-status-badge [status]="details.contract.status"
            /></mat-card-header>
            <mat-card-content class="details-grid">
              <div>
                <span>Должность</span><strong>{{ details.contract.position }}</strong>
              </div>
              <div>
                <span>Подразделение</span><strong>{{ details.contract.department || '—' }}</strong>
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
                <span>Дата окончания</span
                ><strong>{{
                  details.contract.validTo ? (details.contract.validTo | dateOnly) : '—'
                }}</strong>
              </div>
              <div>
                <span>Расчётная дата окончания</span
                ><strong>{{ details.contract.effectiveValidTo | dateOnly }}</strong>
              </div>
              <div>
                <span>Период предупреждения (мес.)</span
                ><strong>{{ details.contract.prolongationWarningMonths }}</strong>
              </div>
              <div>
                <span>Критический период (мес.)</span
                ><strong>{{ details.contract.prolongationAlertMonths }}</strong>
              </div>
              <div>
                <span>Срок продления (лет)</span
                ><strong>{{ details.contract.prolongationForYears }}</strong>
              </div>
            </mat-card-content>
            <mat-card-actions align="end"
              ><button
                mat-stroked-button
                type="button"
                [disabled]="busy()"
                (click)="closeContract(details.contract.contractId, details.contract.rowVersion)"
              >
                Закрыть контракт
              </button></mat-card-actions
            >
          </mat-card>
          <mat-card appearance="outlined" class="section-card">
            <mat-card-header
              ><mat-card-title>История сертификаций</mat-card-title
              ><span class="header-spacer"></span
              ><button
                mat-flat-button
                type="button"
                [disabled]="busy() || hasIncomplete()"
                [matTooltip]="hasIncomplete() ? 'Сначала завершите текущую сертификацию' : ''"
                (click)="createCertification(details.contract.contractId)"
              >
                Добавить сертификацию
              </button></mat-card-header
            >
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
                    ><ng-container matColumnDef="assessor"
                      ><th mat-header-cell *matHeaderCellDef>Экзаменатор</th>
                      <td mat-cell *matCellDef="let row">{{ row.assessor }}</td></ng-container
                    ><ng-container matColumnDef="protocol"
                      ><th mat-header-cell *matHeaderCellDef>Дата протокола</th>
                      <td mat-cell *matCellDef="let row">
                        {{ row.protocolDate ? (row.protocolDate | dateOnly) : '—' }}
                      </td></ng-container
                    ><ng-container matColumnDef="sent"
                      ><th mat-header-cell *matHeaderCellDef>Дата отправки</th>
                      <td mat-cell *matCellDef="let row">
                        {{ row.prolongationSend ? (row.prolongationSend | dateOnly) : '—' }}
                      </td></ng-container
                    ><ng-container matColumnDef="returned"
                      ><th mat-header-cell *matHeaderCellDef>Дата возврата</th>
                      <td mat-cell *matCellDef="let row">
                        {{
                          row.prolongationReturned
                            ? (row.prolongationReturned | dateOnly)
                            : 'В процессе'
                        }}
                      </td></ng-container
                    ><ng-container matColumnDef="actions"
                      ><th mat-header-cell *matHeaderCellDef>
                        <span class="visually-hidden">Действия</span>
                      </th>
                      <td mat-cell *matCellDef="let row">
                        <a
                          mat-button
                          [routerLink]="['/admin/certifications', row.certificationId]"
                          [queryParams]="{ employeeId, contractId: details.contract.contractId }"
                          >{{ row.isCompleted ? 'Просмотреть' : 'Редактировать' }}</a
                        >
                      </td></ng-container
                    >
                    <tr mat-header-row *matHeaderRowDef="columns"></tr>
                    <tr mat-row *matRowDef="let row; columns: columns"></tr>
                  </table>
                </div>
              }
            </mat-card-content>
          </mat-card>
        } @else {
          <mat-card appearance="outlined" class="section-card">
            <mat-card-header
              ><mat-card-title>Создание активного контракта</mat-card-title></mat-card-header
            >
            <mat-card-content
              ><app-contract-form-fields [form]="contractForm" />
              @if (operationError()) {
                <p class="form-error" role="alert">{{ operationError() }}</p>
              }
            </mat-card-content>
            <mat-card-actions align="end"
              ><button mat-flat-button type="button" [disabled]="busy()" (click)="createContract()">
                {{ busy() ? 'Создание…' : 'Создать контракт' }}
              </button></mat-card-actions
            >
          </mat-card>
        }
      }
    </section>
  `,
})
export class ContractManagementComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly employees = inject(EmployeesService);
  private readonly contracts = inject(ContractsService);
  private readonly errors = inject(ApiErrorService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  readonly employeeId = this.route.snapshot.paramMap.get('employeeId') ?? '';
  readonly columns = ['date', 'assessor', 'protocol', 'sent', 'returned', 'actions'];
  readonly contractForm = createContractForm();
  readonly employee = signal<EmployeeDetailsDto | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly errorMessage = signal('');
  readonly operationError = signal('');
  readonly hasIncomplete = () =>
    this.employee()?.currentContract?.certifications.some((item) => !item.isCompleted) ?? false;

  ngOnInit(): void {
    this.load();
  }
  load(): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.employees
      .getEmployee(this.employeeId)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (item) => this.employee.set(item),
        error: (error) => this.errorMessage.set(this.errors.message(error)),
      });
  }
  createContract(): void {
    if (this.contractForm.invalid || this.busy()) {
      this.contractForm.markAllAsTouched();
      return;
    }
    this.busy.set(true);
    this.operationError.set('');
    this.contracts
      .createEmployeeContract(this.employeeId, toContractRequest(this.contractForm))
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: () => {
          this.snackBar.open('Контракт создан.', undefined, { duration: 2500 });
          this.load();
        },
        error: (error: unknown) => {
          if (!applyValidationErrors(this.contractForm, error))
            this.operationError.set(this.errors.message(error));
        },
      });
  }
  closeContract(contractId: number, rowVersion: number): void {
    this.dialog
      .open(DateConfirmDialogComponent, {
        data: {
          title: 'Закрытие контракта',
          description:
            'Закрытие контракта необратимо. Сотрудник не сможет войти в систему, пока не будет создан новый контракт.',
          label: 'Дата закрытия',
          confirmLabel: 'Закрыть контракт',
          initialDate: new Date(),
        },
        restoreFocus: true,
      })
      .afterClosed()
      .subscribe((closedOn) => {
        if (!closedOn) return;
        this.busy.set(true);
        this.operationError.set('');
        this.contracts
          .closeContract(contractId, { closedOn, rowVersion })
          .pipe(finalize(() => this.busy.set(false)))
          .subscribe({
            next: () => {
              this.snackBar.open('Контракт закрыт.', undefined, { duration: 2500 });
              this.load();
            },
            error: (error) => {
              this.operationError.set(this.errors.message(error));
              this.errors.notify(error);
            },
          });
      });
  }
  createCertification(contractId: number): void {
    this.dialog
      .open(CertificationCreateDialogComponent, { data: contractId, restoreFocus: true })
      .afterClosed()
      .subscribe((result) => {
        if (result) {
          this.snackBar.open('Сертификация создана.', undefined, { duration: 2500 });
          this.load();
        }
      });
  }
}

import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatDatepickerIntl } from '@angular/material/datepicker';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom, finalize } from 'rxjs';
import type {
  CertificationDto,
  ContractDto,
  EmployeeDetailsDto,
  UpdateCertificationRequest,
} from '../../core/api/generated/certificationsApiV1.schemas';
import { CertificationsService } from '../../core/api/generated/certifications/certifications.service';
import { EmployeesService } from '../../core/api/generated/employees/employees.service';
import { ApiErrorService } from '../../core/error-handling/api-error.service';
import { applyValidationErrors } from '../../core/error-handling/api-errors';
import { createRussianDatepickerIntl } from '../../core/localization/russian-material-intl';
import { DateConfirmDialogComponent } from '../../shared/components/date-confirm-dialog.component';
import { MATERIAL_IMPORTS } from '../../shared/material/material-imports';
import { DateOnlyDisplayPipe } from '../../shared/utilities/date-only-display.pipe';
import {
  certificationDateOrderValidator,
  fromDateOnly,
  toDateOnly,
} from '../../shared/utilities/date-only';

@Component({
  selector: 'app-certification-detail',
  imports: [ReactiveFormsModule, RouterLink, DateOnlyDisplayPipe, ...MATERIAL_IMPORTS],
  providers: [{ provide: MatDatepickerIntl, useFactory: createRussianDatepickerIntl }],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="feature-page narrow-page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Сертификации</p>
          <h1>Данные сертификации</h1>
        </div>
        @if (employeeId()) {
          <a mat-button [routerLink]="['/admin/users', employeeId(), 'contract']">К контракту</a>
        }
      </header>
      @if (loading()) {
        <mat-progress-bar mode="indeterminate" aria-label="Загрузка сертификации" />
      } @else if (loadError()) {
        <div class="state-panel" role="alert">
          <p>{{ loadError() }}</p>
          <button mat-button type="button" (click)="load()">Обновить</button>
        </div>
      } @else if (certification(); as item) {
        <mat-card appearance="outlined" class="section-card">
          <mat-card-header
            ><mat-card-title>{{
              item.isCompleted ? 'Завершённая сертификация' : 'Сертификация в процессе'
            }}</mat-card-title
            ><span class="header-spacer"></span
            ><span class="record-state">{{
              item.isCompleted ? 'Только просмотр' : 'Редактирование'
            }}</span></mat-card-header
          >
          <mat-card-content>
            <form [formGroup]="form" class="form-grid" (ngSubmit)="save()">
              <mat-form-field appearance="outline"
                ><mat-label>Экзаменатор</mat-label
                ><input matInput formControlName="assessor" [readonly]="item.isCompleted" />
                @if (form.controls.assessor.invalid && form.controls.assessor.touched) {
                  <mat-error>Укажите экзаменатора.</mat-error>
                }
              </mat-form-field>
              <mat-form-field appearance="outline"
                ><mat-label>Дата сертификации</mat-label
                ><input
                  matInput
                  [matDatepicker]="certPicker"
                  formControlName="certificationDate"
                  [readonly]="item.isCompleted" /><mat-datepicker-toggle
                  matIconSuffix
                  [for]="certPicker"
                  [disabled]="item.isCompleted" /><mat-datepicker #certPicker
              /></mat-form-field>
              <mat-form-field appearance="outline"
                ><mat-label>Дата протокола</mat-label
                ><input
                  matInput
                  [matDatepicker]="protocolPicker"
                  formControlName="protocolDate"
                  [readonly]="item.isCompleted" /><mat-datepicker-toggle
                  matIconSuffix
                  [for]="protocolPicker"
                  [disabled]="item.isCompleted" /><mat-datepicker #protocolPicker
              /></mat-form-field>
              <mat-form-field appearance="outline"
                ><mat-label>Дата отправки документов</mat-label
                ><input
                  matInput
                  [matDatepicker]="sentPicker"
                  formControlName="prolongationSend"
                  [readonly]="item.isCompleted" /><mat-datepicker-toggle
                  matIconSuffix
                  [for]="sentPicker"
                  [disabled]="item.isCompleted" /><mat-datepicker #sentPicker
              /></mat-form-field>
            </form>
            @if (item.prolongationReturned) {
              <div class="details-grid">
                <div>
                  <span>Дата возврата документов</span
                  ><strong>{{ item.prolongationReturned | dateOnly }}</strong>
                </div>
              </div>
            }
            @if (form.hasError('protocolRequired')) {
              <p class="form-error">Перед датой отправки необходимо указать дату протокола.</p>
            }
            @if (form.hasError('certificationOrder') || form.hasError('protocolOrder')) {
              <p class="form-error">Даты сертификации должны соответствовать порядку этапов.</p>
            }
            @if (operationError()) {
              <div class="state-panel compact-state" role="alert">
                <p>{{ operationError() }}</p>
                <button mat-button type="button" (click)="load()">Обновить данные с сервера</button>
              </div>
            }
          </mat-card-content>
          @if (!item.isCompleted) {
            <mat-card-actions align="end"
              ><button
                mat-stroked-button
                type="button"
                [disabled]="busy() || !canReturn()"
                [matTooltip]="canReturn() ? '' : 'Сначала сохраните дату протокола и дату отправки'"
                (click)="returnCertification()"
              >
                Завершить сертификацию</button
              ><button mat-flat-button type="button" [disabled]="busy()" (click)="save()">
                {{ busy() ? 'Сохранение…' : 'Сохранить изменения' }}
              </button></mat-card-actions
            >
          }
        </mat-card>
      }
    </section>
  `,
})
export class CertificationDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly certifications = inject(CertificationsService);
  private readonly employees = inject(EmployeesService);
  private readonly errors = inject(ApiErrorService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  readonly certificationId = Number(this.route.snapshot.paramMap.get('certificationId'));
  readonly employeeId = signal(this.route.snapshot.queryParamMap.get('employeeId') ?? '');
  readonly certification = signal<CertificationDto | null>(null);
  readonly contract = signal<ContractDto | null>(null);
  readonly loading = signal(true);
  readonly busy = signal(false);
  readonly loadError = signal('');
  readonly operationError = signal('');
  readonly form = new FormGroup(
    {
      assessor: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      certificationDate: new FormControl<Date | null>(null, { validators: [Validators.required] }),
      protocolDate: new FormControl<Date | null>(null),
      prolongationSend: new FormControl<Date | null>(null),
    },
    { validators: [certificationDateOrderValidator] },
  );
  readonly canReturn = () =>
    Boolean(
      this.certification()?.protocolDate &&
      this.certification()?.prolongationSend &&
      this.form.valid,
    );

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set('');
    this.operationError.set('');
    try {
      let employeeId = this.employeeId();
      if (!employeeId) employeeId = await this.resolveEmployeeIdFromOverview();
      if (!employeeId) throw new Error('Не удалось определить сотрудника для этой сертификации.');
      const employee = await firstValueFrom(this.employees.getEmployee(employeeId));
      const details = employee.currentContract;
      if (!details) throw new Error('У сотрудника нет активного контракта с этой сертификацией.');
      const expectedContractId = Number(this.route.snapshot.queryParamMap.get('contractId') ?? 0);
      if (expectedContractId && details.contract.contractId !== expectedContractId)
        throw new Error('Контракт был изменён и больше не соответствует открытым данным.');
      const certification = details.certifications.find(
        (item) => item.certificationId === this.certificationId,
      );
      if (!certification) throw new Error('Сертификация не найдена в истории активного контракта.');
      this.employeeId.set(employeeId);
      this.contract.set(details.contract);
      this.certification.set(certification);
      this.form.reset({
        assessor: certification.assessor,
        certificationDate: fromDateOnly(certification.certificationDate),
        protocolDate: fromDateOnly(certification.protocolDate),
        prolongationSend: fromDateOnly(certification.prolongationSend),
      });
      if (certification.isCompleted) this.form.disable({ emitEvent: false });
      else this.form.enable({ emitEvent: false });
    } catch (error) {
      this.loadError.set(error instanceof Error ? error.message : this.errors.message(error));
    } finally {
      this.loading.set(false);
    }
  }

  save(): void {
    if (this.form.invalid || this.busy() || this.certification()?.isCompleted) {
      this.form.markAllAsTouched();
      return;
    }
    const value = this.form.getRawValue();
    const certificationDate = toDateOnly(value.certificationDate);
    if (!certificationDate) return;
    const request: UpdateCertificationRequest = {
      assessor: value.assessor.trim(),
      certificationDate,
      protocolDate: toDateOnly(value.protocolDate),
      prolongationSend: toDateOnly(value.prolongationSend),
    };
    this.busy.set(true);
    this.operationError.set('');
    this.certifications
      .updateCertification(this.certificationId, request)
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: (updated) => {
          this.certification.set(updated);
          this.snackBar.open('Сертификация обновлена.', undefined, { duration: 2500 });
          void this.load();
        },
        error: (error: unknown) => {
          if (!applyValidationErrors(this.form, error))
            this.operationError.set(this.errors.message(error));
        },
      });
  }

  returnCertification(): void {
    const contract = this.contract();
    if (!contract || !this.canReturn() || this.busy()) return;
    this.dialog
      .open(DateConfirmDialogComponent, {
        data: {
          title: 'Завершение сертификации',
          description: 'Завершение сертификации необратимо и изменит срок действия контракта.',
          label: 'Дата возврата документов',
          confirmLabel: 'Завершить сертификацию',
          initialDate: new Date(),
        },
        restoreFocus: true,
      })
      .afterClosed()
      .subscribe((prolongationReturned) => {
        if (!prolongationReturned) return;
        this.busy.set(true);
        this.operationError.set('');
        this.certifications
          .returnCertification(this.certificationId, {
            prolongationReturned,
            rowVersion: contract.rowVersion,
          })
          .pipe(finalize(() => this.busy.set(false)))
          .subscribe({
            next: () => {
              this.snackBar.open('Сертификация завершена, срок контракта обновлён.', undefined, {
                duration: 3000,
              });
              void this.load();
            },
            error: (error) => {
              this.operationError.set(this.errors.message(error));
              this.errors.notify(error);
            },
          });
      });
  }

  private async resolveEmployeeIdFromOverview(): Promise<string> {
    let page = 1;
    while (true) {
      const result = await firstValueFrom(
        this.certifications.getCertificationOverview({
          page,
          pageSize: 100,
          includeInactive: true,
        }),
      );
      const match = result.items.find(
        (row) => row.latestCertification?.certificationId === this.certificationId,
      );
      if (match) return match.employeeId;
      if (page * result.pageSize >= result.totalCount) return '';
      page += 1;
    }
  }
}

import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { PageEvent } from '@angular/material/paginator';
import { Sort } from '@angular/material/sort';
import { RouterLink } from '@angular/router';
import {
  EMPTY,
  Subject,
  catchError,
  debounceTime,
  finalize,
  startWith,
  switchMap,
  tap,
} from 'rxjs';
import {
  CertificationOverviewRowDto,
  CertificationOverviewRowDtoPagedResult,
  GetCertificationOverviewParams,
  GetCertificationOverviewStatus,
} from '../../core/api/generated/certificationsApiV1.schemas';
import { CertificationsService } from '../../core/api/generated/certifications/certifications.service';
import { ApiErrorService } from '../../core/error-handling/api-error.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge.component';
import { MATERIAL_IMPORTS } from '../../shared/material/material-imports';
import { dateRangeValidator, toDateOnly } from '../../shared/utilities/date-only';

export interface CertificationFilterValue {
  name: string;
  department: string;
  status: GetCertificationOverviewStatus | null;
  validToFrom: Date | null;
  validToTo: Date | null;
  includeInactive: boolean;
}

export function buildCertificationOverviewParams(
  value: CertificationFilterValue,
  pageIndex: number,
  pageSize: number,
  sort: string,
  direction: 'asc' | 'desc',
): GetCertificationOverviewParams {
  return {
    page: pageIndex + 1,
    pageSize,
    name: value.name.trim() || undefined,
    department: value.department.trim() || undefined,
    status: value.status ?? undefined,
    validToFrom: toDateOnly(value.validToFrom) ?? undefined,
    validToTo: toDateOnly(value.validToTo) ?? undefined,
    includeInactive: value.includeInactive,
    sort,
    direction,
  };
}

@Component({
  selector: 'app-certification-overview',
  imports: [ReactiveFormsModule, RouterLink, StatusBadgeComponent, ...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="feature-page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Administration</p>
          <h1>Certification overview</h1>
        </div>
      </header>

      <mat-card appearance="outlined" class="filter-card">
        <mat-card-content>
          <form [formGroup]="filters" class="filter-grid" aria-label="Certification filters">
            <mat-form-field appearance="outline"
              ><mat-label>Employee name or ID</mat-label><input matInput formControlName="name"
            /></mat-form-field>
            <mat-form-field appearance="outline"
              ><mat-label>Department</mat-label><input matInput formControlName="department"
            /></mat-form-field>
            <mat-form-field appearance="outline"
              ><mat-label>Status</mat-label
              ><mat-select formControlName="status"
                ><mat-option [value]="null">All statuses</mat-option>
                @for (option of statusOptions; track option.value) {
                  <mat-option [value]="option.value">{{ option.label }}</mat-option>
                }
              </mat-select></mat-form-field
            >
            <mat-form-field appearance="outline"
              ><mat-label>Effective valid from</mat-label
              ><input
                matInput
                [matDatepicker]="fromPicker"
                formControlName="validToFrom" /><mat-datepicker-toggle
                matIconSuffix
                [for]="fromPicker" /><mat-datepicker #fromPicker
            /></mat-form-field>
            <mat-form-field appearance="outline"
              ><mat-label>Effective valid to</mat-label
              ><input
                matInput
                [matDatepicker]="toPicker"
                formControlName="validToTo" /><mat-datepicker-toggle
                matIconSuffix
                [for]="toPicker" /><mat-datepicker #toPicker
            /></mat-form-field>
            <mat-checkbox formControlName="includeInactive"
              >Include inactive employees</mat-checkbox
            >
            <button mat-button type="button" (click)="clearFilters()">Clear filters</button>
          </form>
          @if (filters.hasError('dateRange')) {
            <p class="form-error" role="alert">The start date must not be after the end date.</p>
          }
        </mat-card-content>
      </mat-card>

      <mat-card appearance="outlined" class="table-card">
        @if (loading()) {
          <mat-progress-bar mode="indeterminate" aria-label="Loading certification overview" />
        }
        @if (errorMessage()) {
          <div class="state-panel" role="alert">
            <p>{{ errorMessage() }}</p>
            <button mat-button type="button" (click)="reload()">Retry</button>
          </div>
        } @else if (!loading() && rows().length === 0) {
          <div class="state-panel"><p>No certification records match the filters.</p></div>
        } @else {
          <div
            class="table-scroll"
            role="region"
            aria-label="Certification overview table"
            tabindex="0"
          >
            <table
              mat-table
              [dataSource]="rows()"
              matSort
              [matSortActive]="sortActive()"
              [matSortDirection]="sortDirection()"
              (matSortChange)="sortChanged($event)"
            >
              <ng-container matColumnDef="personalId"
                ><th mat-header-cell *matHeaderCellDef>Personal ID</th>
                <td mat-cell *matCellDef="let row">{{ row.personalId }}</td></ng-container
              >
              <ng-container matColumnDef="lastName"
                ><th mat-header-cell *matHeaderCellDef mat-sort-header="name">Last name</th>
                <td mat-cell *matCellDef="let row">{{ row.lastName }}</td></ng-container
              >
              <ng-container matColumnDef="firstName"
                ><th mat-header-cell *matHeaderCellDef>First name</th>
                <td mat-cell *matCellDef="let row">{{ row.firstName }}</td></ng-container
              >
              <ng-container matColumnDef="middleName"
                ><th mat-header-cell *matHeaderCellDef>Middle name</th>
                <td mat-cell *matCellDef="let row">{{ row.middleName || '—' }}</td></ng-container
              >
              <ng-container matColumnDef="position"
                ><th mat-header-cell *matHeaderCellDef>Position</th>
                <td mat-cell *matCellDef="let row">{{ row.position || '—' }}</td></ng-container
              >
              <ng-container matColumnDef="department"
                ><th mat-header-cell *matHeaderCellDef mat-sort-header="department">Department</th>
                <td mat-cell *matCellDef="let row">{{ row.department || '—' }}</td></ng-container
              >
              <ng-container matColumnDef="division"
                ><th mat-header-cell *matHeaderCellDef>Division</th>
                <td mat-cell *matCellDef="let row">{{ row.division || '—' }}</td></ng-container
              >
              <ng-container matColumnDef="contractDate"
                ><th mat-header-cell *matHeaderCellDef>Contract date</th>
                <td mat-cell *matCellDef="let row">{{ row.contractDate || '—' }}</td></ng-container
              >
              <ng-container matColumnDef="effectiveValidTo"
                ><th mat-header-cell *matHeaderCellDef mat-sort-header="effectiveValidTo">
                  Effective valid to
                </th>
                <td mat-cell *matCellDef="let row">
                  {{ row.effectiveValidTo || '—' }}
                </td></ng-container
              >
              <ng-container matColumnDef="latest"
                ><th mat-header-cell *matHeaderCellDef>Latest certification</th>
                <td mat-cell *matCellDef="let row">
                  @if (row.latestCertification) {
                    {{ row.latestCertification.certificationDate }} ·
                    {{ row.latestCertification.assessor }}
                  } @else {
                    —
                  }
                </td></ng-container
              >
              <ng-container matColumnDef="status"
                ><th mat-header-cell *matHeaderCellDef mat-sort-header="status">Status</th>
                <td mat-cell *matCellDef="let row"><app-status-badge [status]="row.status" /></td
              ></ng-container>
              <ng-container matColumnDef="actions"
                ><th mat-header-cell *matHeaderCellDef>
                  <span class="visually-hidden">Actions</span>
                </th>
                <td mat-cell *matCellDef="let row">
                  <a mat-button [routerLink]="['/admin/users', row.employeeId]">Employee</a>
                  @if (row.latestCertification && row.contractId) {
                    <a
                      mat-button
                      [routerLink]="[
                        '/admin/certifications',
                        row.latestCertification.certificationId,
                      ]"
                      [queryParams]="{ employeeId: row.employeeId, contractId: row.contractId }"
                      >Certification</a
                    >
                  }
                </td></ng-container
              >
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr mat-row *matRowDef="let row; columns: columns"></tr>
            </table>
          </div>
          <mat-paginator
            [length]="totalCount()"
            [pageIndex]="pageIndex()"
            [pageSize]="pageSize()"
            [pageSizeOptions]="[10, 25, 50, 100]"
            (page)="pageChanged($event)"
            showFirstLastButtons
          />
        }
      </mat-card>
    </section>
  `,
})
export class CertificationOverviewComponent implements OnInit {
  private readonly api = inject(CertificationsService);
  private readonly errors = inject(ApiErrorService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reloadSubject = new Subject<void>();
  readonly columns = [
    'personalId',
    'lastName',
    'firstName',
    'middleName',
    'position',
    'department',
    'division',
    'contractDate',
    'effectiveValidTo',
    'latest',
    'status',
    'actions',
  ];
  readonly statusOptions = [
    { value: GetCertificationOverviewStatus.ContractValid, label: 'Contract valid' },
    { value: GetCertificationOverviewStatus.CertificationPending, label: 'Certification pending' },
    {
      value: GetCertificationOverviewStatus.CertificationInProgress,
      label: 'Certification in progress',
    },
    { value: GetCertificationOverviewStatus.CertificationMissing, label: 'Certification missing' },
    { value: GetCertificationOverviewStatus.NotApplicable, label: 'Not applicable' },
  ];
  readonly filters = new FormGroup(
    {
      name: new FormControl('', { nonNullable: true }),
      department: new FormControl('', { nonNullable: true }),
      status: new FormControl<GetCertificationOverviewStatus | null>(null),
      validToFrom: new FormControl<Date | null>(null),
      validToTo: new FormControl<Date | null>(null),
      includeInactive: new FormControl(false, { nonNullable: true }),
    },
    { validators: [dateRangeValidator] },
  );
  readonly rows = signal<CertificationOverviewRowDto[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(25);
  readonly sortActive = signal('name');
  readonly sortDirection = signal<'asc' | 'desc'>('asc');
  readonly loading = signal(false);
  readonly errorMessage = signal('');

  ngOnInit(): void {
    this.filters.valueChanges
      .pipe(debounceTime(350), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        if (this.filters.valid) {
          this.pageIndex.set(0);
          this.reload();
        }
      });
    this.reloadSubject
      .pipe(
        startWith(undefined),
        tap(() => {
          this.loading.set(true);
          this.errorMessage.set('');
        }),
        switchMap(() =>
          this.api.getCertificationOverview(this.params()).pipe(
            catchError((error: unknown) => {
              this.errorMessage.set(this.errors.message(error));
              return EMPTY;
            }),
            finalize(() => this.loading.set(false)),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result: CertificationOverviewRowDtoPagedResult) => {
        this.rows.set(result.items);
        this.totalCount.set(result.totalCount);
      });
  }

  reload(): void {
    this.reloadSubject.next();
  }
  clearFilters(): void {
    this.filters.reset({
      name: '',
      department: '',
      status: null,
      validToFrom: null,
      validToTo: null,
      includeInactive: false,
    });
  }
  pageChanged(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.reload();
  }
  sortChanged(event: Sort): void {
    this.sortActive.set(event.active || 'name');
    this.sortDirection.set(event.direction === 'desc' ? 'desc' : 'asc');
    this.pageIndex.set(0);
    this.reload();
  }

  private params(): GetCertificationOverviewParams {
    return buildCertificationOverviewParams(
      this.filters.getRawValue(),
      this.pageIndex(),
      this.pageSize(),
      this.sortActive(),
      this.sortDirection(),
    );
  }
}

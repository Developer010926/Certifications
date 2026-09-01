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
  takeUntil,
  tap,
} from 'rxjs';
import type {
  EmployeeSummaryDto,
  EmployeeSummaryDtoPagedResult,
  ListEmployeesParams,
} from '../../core/api/generated/certificationsApiV1.schemas';
import { EmployeesService } from '../../core/api/generated/employees/employees.service';
import { ApiErrorService } from '../../core/error-handling/api-error.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge.component';
import { MATERIAL_IMPORTS } from '../../shared/material/material-imports';

@Component({
  selector: 'app-employee-list',
  imports: [ReactiveFormsModule, RouterLink, StatusBadgeComponent, ...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="feature-page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Administration</p>
          <h1>Employees</h1>
        </div>
        <a mat-flat-button routerLink="/admin/users/new">Create employee</a>
      </header>
      <mat-card appearance="outlined" class="filter-card"
        ><mat-card-content>
          <form
            [formGroup]="filters"
            class="filter-grid compact-filters"
            aria-label="Employee filters"
          >
            <mat-form-field appearance="outline"
              ><mat-label>Name or Personal ID</mat-label><input matInput formControlName="name"
            /></mat-form-field>
            <mat-checkbox formControlName="includeInactive"
              >Include employees without an active contract</mat-checkbox
            >
            <button
              mat-button
              type="button"
              (click)="filters.reset({ name: '', includeInactive: false })"
            >
              Clear filters
            </button>
          </form>
        </mat-card-content></mat-card
      >
      <mat-card appearance="outlined" class="table-card">
        @if (loading()) {
          <mat-progress-bar mode="indeterminate" aria-label="Loading employees" />
        }
        @if (errorMessage()) {
          <div class="state-panel" role="alert">
            <p>{{ errorMessage() }}</p>
            <button mat-button type="button" (click)="reload()">Retry</button>
          </div>
        } @else if (!loading() && rows().length === 0) {
          <div class="state-panel"><p>No employees match the filters.</p></div>
        } @else {
          <div class="table-scroll" role="region" aria-label="Employee table" tabindex="0">
            <table
              mat-table
              [dataSource]="rows()"
              matSort
              [matSortActive]="sortActive()"
              [matSortDirection]="sortDirection()"
              (matSortChange)="sortChanged($event)"
            >
              <ng-container matColumnDef="personalId"
                ><th mat-header-cell *matHeaderCellDef mat-sort-header="personalId">Personal ID</th>
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
              <ng-container matColumnDef="admin"
                ><th mat-header-cell *matHeaderCellDef>Administrator</th>
                <td mat-cell *matCellDef="let row">
                  {{ row.isAdmin ? 'Yes' : 'No' }}
                </td></ng-container
              >
              <ng-container matColumnDef="contract"
                ><th mat-header-cell *matHeaderCellDef>Active contract</th>
                <td mat-cell *matCellDef="let row">
                  <span [class.inactive-text]="!row.hasActiveContract">{{
                    row.hasActiveContract ? 'Active' : 'No active contract'
                  }}</span>
                </td></ng-container
              >
              <ng-container matColumnDef="status"
                ><th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let row"><app-status-badge [status]="row.status" /></td
              ></ng-container>
              <ng-container matColumnDef="actions"
                ><th mat-header-cell *matHeaderCellDef>
                  <span class="visually-hidden">Actions</span>
                </th>
                <td mat-cell *matCellDef="let row">
                  <a mat-button [routerLink]="['/admin/users', row.employeeId]">Open</a>
                </td></ng-container
              >
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr
                mat-row
                *matRowDef="let row; columns: columns"
                [class.inactive-row]="!row.hasActiveContract"
              ></tr>
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
  styles: `
    .inactive-row {
      opacity: 0.72;
      background: var(--mat-sys-surface-container-low);
    }
    .inactive-text {
      font-weight: 700;
    }
  `,
})
export class EmployeeListComponent implements OnInit {
  private readonly api = inject(EmployeesService);
  private readonly errors = inject(ApiErrorService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reloadSubject = new Subject<void>();
  readonly columns = [
    'personalId',
    'lastName',
    'firstName',
    'middleName',
    'admin',
    'contract',
    'status',
    'actions',
  ];
  readonly filters = new FormGroup({
    name: new FormControl('', { nonNullable: true }),
    includeInactive: new FormControl(false, { nonNullable: true }),
  });
  readonly rows = signal<EmployeeSummaryDto[]>([]);
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
        this.pageIndex.set(0);
        this.reload();
      });
    this.reloadSubject
      .pipe(
        startWith(undefined),
        tap(() => {
          this.loading.set(true);
          this.errorMessage.set('');
        }),
        switchMap(() =>
          this.api.listEmployees(this.params()).pipe(
            catchError((error: unknown) => {
              this.errorMessage.set(this.errors.message(error));
              return EMPTY;
            }),
            finalize(() => this.loading.set(false)),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result: EmployeeSummaryDtoPagedResult) => {
        this.rows.set(result.items);
        this.totalCount.set(result.totalCount);
      });
  }

  reload(): void {
    this.reloadSubject.next();
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
  private params(): ListEmployeesParams {
    const value = this.filters.getRawValue();
    return {
      page: this.pageIndex() + 1,
      pageSize: this.pageSize(),
      name: value.name.trim() || undefined,
      includeInactive: value.includeInactive,
      sort: this.sortActive(),
      direction: this.sortDirection(),
    };
  }
}

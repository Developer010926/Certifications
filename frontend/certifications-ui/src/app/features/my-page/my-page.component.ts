import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { finalize } from 'rxjs';
import type { ContractDetailsDto } from '../../core/api/generated/certificationsApiV1.schemas';
import { ContractsService } from '../../core/api/generated/contracts/contracts.service';
import { PasswordsService } from '../../core/api/generated/passwords/passwords.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApiErrorService } from '../../core/error-handling/api-error.service';
import { PasswordDialogComponent } from '../../shared/components/password-dialog.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge.component';
import { MATERIAL_IMPORTS } from '../../shared/material/material-imports';

@Component({
  selector: 'app-my-page',
  imports: [StatusBadgeComponent, ...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="feature-page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Personal area</p>
          <h1>My page</h1>
        </div>
        <button
          mat-stroked-button
          type="button"
          [disabled]="revealing()"
          (click)="revealPassword()"
        >
          {{ revealing() ? 'Revealing…' : 'Reveal my password' }}
        </button>
      </header>

      @if (auth.currentUser(); as user) {
        <mat-card appearance="outlined" class="section-card">
          <mat-card-header><mat-card-title>Personal information</mat-card-title></mat-card-header>
          <mat-card-content class="details-grid">
            <div>
              <span>Personal ID</span><strong>{{ user.personalId }}</strong>
            </div>
            <div>
              <span>First name</span><strong>{{ user.firstName }}</strong>
            </div>
            <div>
              <span>Middle name</span><strong>{{ user.middleName || '—' }}</strong>
            </div>
            <div>
              <span>Last name</span><strong>{{ user.lastName }}</strong>
            </div>
            <div>
              <span>Administrator</span><strong>{{ user.isAdmin ? 'Yes' : 'No' }}</strong>
            </div>
          </mat-card-content>
        </mat-card>
      }

      @if (loading()) {
        <mat-progress-bar mode="indeterminate" aria-label="Loading contract" />
      } @else if (errorMessage()) {
        <div class="state-panel" role="alert">
          <p>{{ errorMessage() }}</p>
          <button mat-button type="button" (click)="load()">Retry</button>
        </div>
      } @else if (details(); as data) {
        <mat-card appearance="outlined" class="section-card">
          <mat-card-header
            ><mat-card-title>Current contract</mat-card-title><span class="header-spacer"></span
            ><app-status-badge [status]="data.contract.status"
          /></mat-card-header>
          <mat-card-content class="details-grid">
            <div>
              <span>Position</span><strong>{{ data.contract.position }}</strong>
            </div>
            <div>
              <span>Department</span><strong>{{ data.contract.department || '—' }}</strong>
            </div>
            <div>
              <span>Division</span><strong>{{ data.contract.division || '—' }}</strong>
            </div>
            <div>
              <span>Contract date</span><strong>{{ data.contract.contractDate }}</strong>
            </div>
            <div>
              <span>Valid to</span><strong>{{ data.contract.validTo || '—' }}</strong>
            </div>
            <div>
              <span>Effective valid to</span><strong>{{ data.contract.effectiveValidTo }}</strong>
            </div>
          </mat-card-content>
        </mat-card>
        <mat-card appearance="outlined" class="section-card">
          <mat-card-header><mat-card-title>Certification history</mat-card-title></mat-card-header>
          <mat-card-content>
            @if (data.certifications.length === 0) {
              <p class="empty-text">No certifications recorded.</p>
            } @else {
              <div
                class="table-scroll"
                role="region"
                aria-label="Certification history"
                tabindex="0"
              >
                <table mat-table [dataSource]="data.certifications">
                  <ng-container matColumnDef="date"
                    ><th mat-header-cell *matHeaderCellDef>Certification date</th>
                    <td mat-cell *matCellDef="let row">
                      {{ row.certificationDate }}
                    </td></ng-container
                  >
                  <ng-container matColumnDef="assessor"
                    ><th mat-header-cell *matHeaderCellDef>Assessor</th>
                    <td mat-cell *matCellDef="let row">{{ row.assessor }}</td></ng-container
                  >
                  <ng-container matColumnDef="protocol"
                    ><th mat-header-cell *matHeaderCellDef>Protocol</th>
                    <td mat-cell *matCellDef="let row">
                      {{ row.protocolDate || '—' }}
                    </td></ng-container
                  >
                  <ng-container matColumnDef="sent"
                    ><th mat-header-cell *matHeaderCellDef>Sent</th>
                    <td mat-cell *matCellDef="let row">
                      {{ row.prolongationSend || '—' }}
                    </td></ng-container
                  >
                  <ng-container matColumnDef="returned"
                    ><th mat-header-cell *matHeaderCellDef>Returned</th>
                    <td mat-cell *matCellDef="let row">
                      {{ row.prolongationReturned || '—' }}
                    </td></ng-container
                  >
                  <tr mat-header-row *matHeaderRowDef="columns"></tr>
                  <tr mat-row *matRowDef="let row; columns: columns"></tr>
                </table>
              </div>
            }
          </mat-card-content>
        </mat-card>
      }
    </section>
  `,
})
export class MyPageComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly contracts = inject(ContractsService);
  private readonly passwords = inject(PasswordsService);
  private readonly dialog = inject(MatDialog);
  private readonly errors = inject(ApiErrorService);
  readonly columns = ['date', 'assessor', 'protocol', 'sent', 'returned'];
  readonly details = signal<ContractDetailsDto | null>(null);
  readonly loading = signal(true);
  readonly revealing = signal(false);
  readonly errorMessage = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.contracts
      .getOwnContract()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (data) => this.details.set(data),
        error: (error) => this.errorMessage.set(this.errors.message(error)),
      });
  }

  revealPassword(): void {
    if (this.revealing()) return;
    this.revealing.set(true);
    this.passwords
      .revealOwnPassword()
      .pipe(finalize(() => this.revealing.set(false)))
      .subscribe({
        next: (response) => {
          const data = {
            title: 'Your password',
            description: 'Keep this password private. Copy it only to a secure destination.',
            password: response.password,
          };
          response.password = '';
          this.dialog.open(PasswordDialogComponent, {
            data,
            restoreFocus: true,
            disableClose: true,
          });
        },
        error: (error) => this.errors.notify(error),
      });
  }
}

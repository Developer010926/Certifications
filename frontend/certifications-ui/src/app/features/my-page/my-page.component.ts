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
import { DateOnlyDisplayPipe } from '../../shared/utilities/date-only-display.pipe';

@Component({
  selector: 'app-my-page',
  imports: [DateOnlyDisplayPipe, StatusBadgeComponent, ...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="feature-page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Личный кабинет</p>
          <h1>Моя страница</h1>
        </div>
        <button
          mat-stroked-button
          type="button"
          [disabled]="revealing()"
          (click)="revealPassword()"
        >
          {{ revealing() ? 'Получение пароля…' : 'Показать мой пароль' }}
        </button>
      </header>

      @if (auth.currentUser(); as user) {
        <mat-card appearance="outlined" class="section-card">
          <mat-card-header><mat-card-title>Личные данные</mat-card-title></mat-card-header>
          <mat-card-content class="details-grid">
            <div>
              <span>Табельный номер</span><strong>{{ user.personalId }}</strong>
            </div>
            <div>
              <span>Имя</span><strong>{{ user.firstName }}</strong>
            </div>
            <div>
              <span>Отчество</span><strong>{{ user.middleName || '—' }}</strong>
            </div>
            <div>
              <span>Фамилия</span><strong>{{ user.lastName }}</strong>
            </div>
            <div>
              <span>Администратор</span><strong>{{ user.isAdmin ? 'Да' : 'Нет' }}</strong>
            </div>
          </mat-card-content>
        </mat-card>
      }

      @if (loading()) {
        <mat-progress-bar mode="indeterminate" aria-label="Загрузка контракта" />
      } @else if (errorMessage()) {
        <div class="state-panel" role="alert">
          <p>{{ errorMessage() }}</p>
          <button mat-button type="button" (click)="load()">Повторить</button>
        </div>
      } @else if (details(); as data) {
        <mat-card appearance="outlined" class="section-card">
          <mat-card-header
            ><mat-card-title>Текущий контракт</mat-card-title><span class="header-spacer"></span
            ><app-status-badge [status]="data.contract.status"
          /></mat-card-header>
          <mat-card-content class="details-grid">
            <div>
              <span>Должность</span><strong>{{ data.contract.position }}</strong>
            </div>
            <div>
              <span>Подразделение</span><strong>{{ data.contract.department || '—' }}</strong>
            </div>
            <div>
              <span>Организационная единица</span
              ><strong>{{ data.contract.division || '—' }}</strong>
            </div>
            <div>
              <span>Дата начала контракта</span
              ><strong>{{ data.contract.contractDate | dateOnly }}</strong>
            </div>
            <div>
              <span>Дата окончания</span
              ><strong>{{
                data.contract.validTo ? (data.contract.validTo | dateOnly) : '—'
              }}</strong>
            </div>
            <div>
              <span>Расчётная дата окончания</span
              ><strong>{{ data.contract.effectiveValidTo | dateOnly }}</strong>
            </div>
          </mat-card-content>
        </mat-card>
        <mat-card appearance="outlined" class="section-card">
          <mat-card-header><mat-card-title>История сертификаций</mat-card-title></mat-card-header>
          <mat-card-content>
            @if (data.certifications.length === 0) {
              <p class="empty-text">Сертификации отсутствуют.</p>
            } @else {
              <div
                class="table-scroll"
                role="region"
                aria-label="История сертификаций"
                tabindex="0"
              >
                <table mat-table [dataSource]="data.certifications">
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
                  <ng-container matColumnDef="protocol"
                    ><th mat-header-cell *matHeaderCellDef>Дата протокола</th>
                    <td mat-cell *matCellDef="let row">
                      {{ row.protocolDate ? (row.protocolDate | dateOnly) : '—' }}
                    </td></ng-container
                  >
                  <ng-container matColumnDef="sent"
                    ><th mat-header-cell *matHeaderCellDef>Дата отправки</th>
                    <td mat-cell *matCellDef="let row">
                      {{ row.prolongationSend ? (row.prolongationSend | dateOnly) : '—' }}
                    </td></ng-container
                  >
                  <ng-container matColumnDef="returned"
                    ><th mat-header-cell *matHeaderCellDef>Дата возврата</th>
                    <td mat-cell *matCellDef="let row">
                      {{ row.prolongationReturned ? (row.prolongationReturned | dateOnly) : '—' }}
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
            title: 'Ваш пароль',
            description:
              'Не сообщайте пароль посторонним. Копируйте его только в безопасное место.',
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

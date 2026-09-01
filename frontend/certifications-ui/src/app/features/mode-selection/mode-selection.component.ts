import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { PreferredModeRequestPreferredMode } from '../../core/api/generated/certificationsApiV1.schemas';
import { AuthService } from '../../core/auth/auth.service';
import { ApiErrorService } from '../../core/error-handling/api-error.service';
import { MATERIAL_IMPORTS } from '../../shared/material/material-imports';

@Component({
  selector: 'app-mode-selection',
  imports: [...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="feature-page narrow-page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Добро пожаловать</p>
          <h1>Выберите режим работы</h1>
        </div>
      </header>
      <p>Выберите раздел приложения. Ваш выбор будет сохранён для следующего входа.</p>
      <div class="mode-grid">
        <button
          type="button"
          class="mode-card"
          [class.saved]="savedMode() === modes.MyPage"
          [disabled]="saving()"
          (click)="select(modes.MyPage)"
        >
          <strong>Моя страница</strong
          ><span>Просмотр личных данных, контракта и истории сертификаций.</span>
          @if (savedMode() === modes.MyPage) {
            <small>Сохранённый выбор</small>
          }
        </button>
        <button
          type="button"
          class="mode-card"
          [class.saved]="savedMode() === modes.Administration"
          [disabled]="saving()"
          (click)="select(modes.Administration)"
        >
          <strong>Администрирование приложения</strong
          ><span>Управление сотрудниками, контрактами и сертификациями.</span>
          @if (savedMode() === modes.Administration) {
            <small>Сохранённый выбор</small>
          }
        </button>
      </div>
      @if (saving()) {
        <mat-progress-bar mode="indeterminate" aria-label="Сохранение выбранного режима" />
      }
      @if (saveError()) {
        <p class="form-error" role="alert">{{ saveError() }}</p>
      }
    </section>
  `,
  styles: `
    .mode-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 1rem;
      margin-top: 1.5rem;
    }
    .mode-card {
      min-height: 12rem;
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      gap: 0.75rem;
      padding: 1.5rem;
      border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 1rem;
      background: var(--mat-sys-surface);
      color: inherit;
      text-align: start;
      cursor: pointer;
    }
    .mode-card:hover,
    .mode-card:focus-visible {
      border-color: var(--mat-sys-primary);
      box-shadow: 0 6px 24px rgb(20 50 80 / 10%);
    }
    .mode-card.saved {
      border: 2px solid var(--mat-sys-primary);
      background: var(--mat-sys-primary-container);
    }
    .mode-card strong {
      font-size: 1.2rem;
    }
    .mode-card small {
      margin-top: auto;
      font-weight: 700;
      color: var(--mat-sys-primary);
    }
    @media (max-width: 650px) {
      .mode-grid {
        grid-template-columns: 1fr;
      }
    }
  `,
})
export class ModeSelectionComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly errors = inject(ApiErrorService);
  readonly modes = PreferredModeRequestPreferredMode;
  readonly savedMode = () => this.auth.currentUser()?.preferredAdminMode ?? null;
  readonly saving = signal(false);
  readonly saveError = signal('');

  select(mode: PreferredModeRequestPreferredMode): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.saveError.set('');
    this.auth
      .savePreferredMode(mode)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () =>
          void this.router.navigate([
            mode === PreferredModeRequestPreferredMode.MyPage ? '/me' : '/admin/certifications',
          ]),
        error: (error: unknown) => this.saveError.set(this.errors.message(error)),
      });
  }
}

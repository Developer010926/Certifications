import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { controlError } from '../../core/error-handling/api-errors';
import { MATERIAL_IMPORTS } from '../../shared/material/material-imports';
import { UI_TEXT } from '../../shared/utilities/ui-text';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, ...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="login-page">
      <mat-card appearance="outlined" class="login-card">
        <mat-card-header>
          <mat-card-title>Certifications</mat-card-title>
          <mat-card-subtitle>Sign in to continue</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <mat-form-field appearance="outline">
              <mat-label>Personal ID</mat-label>
              <input matInput formControlName="personalId" autocomplete="username" />
              @if (form.controls.personalId.invalid && form.controls.personalId.touched) {
                <mat-error>{{ error(form.controls.personalId, 'Personal ID') }}</mat-error>
              }
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Password</mat-label>
              <input
                matInput
                formControlName="password"
                [type]="showPassword() ? 'text' : 'password'"
                autocomplete="current-password"
              />
              <button
                mat-button
                matSuffix
                type="button"
                (click)="showPassword.update((value) => !value)"
                [attr.aria-label]="showPassword() ? 'Hide password' : 'Show password'"
              >
                {{ showPassword() ? 'Hide' : 'Show' }}
              </button>
              @if (form.controls.password.invalid && form.controls.password.touched) {
                <mat-error>{{ error(form.controls.password, 'Password') }}</mat-error>
              }
            </mat-form-field>
            @if (loginError()) {
              <p class="form-error" role="alert">{{ loginError() }}</p>
            }
            <button mat-flat-button type="submit" [disabled]="submitting()">
              {{ submitting() ? 'Signing in…' : 'Sign in' }}
            </button>
          </form>
        </mat-card-content>
        @if (submitting()) {
          <mat-progress-bar mode="indeterminate" aria-label="Signing in" />
        }
      </mat-card>
    </main>
  `,
  styles: `
    .login-page {
      min-height: 100dvh;
      display: grid;
      place-items: center;
      padding: 1rem;
      background: linear-gradient(145deg, #eef4ff, #f8fafc 55%, #eaf7f2);
    }
    .login-card {
      width: min(100%, 28rem);
      overflow: hidden;
    }
    mat-card-content {
      padding-top: 1.5rem;
    }
    form {
      display: grid;
      gap: 0.75rem;
    }
    button[type='submit'] {
      min-height: 3rem;
    }
  `,
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly showPassword = signal(false);
  readonly submitting = signal(false);
  readonly loginError = signal('');
  readonly error = controlError;
  readonly form = new FormGroup({
    personalId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.loginError.set('');
    this.auth
      .login(this.form.getRawValue())
      .pipe(
        finalize(() => {
          this.form.controls.password.setValue('');
          this.submitting.set(false);
        }),
      )
      .subscribe({
        next: (user) => void this.router.navigate([user.isAdmin ? '/select-mode' : '/me']),
        error: () => this.loginError.set(UI_TEXT.invalidCredentials),
      });
  }
}

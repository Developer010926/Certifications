import { Clipboard } from '@angular/cdk/clipboard';
import { ChangeDetectionStrategy, Component, OnDestroy, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MATERIAL_IMPORTS } from '../material/material-imports';
import { UI_TEXT } from '../utilities/ui-text';

export interface PasswordDialogData {
  title: string;
  description: string;
  password: string;
}

@Component({
  selector: 'app-password-dialog',
  imports: [...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p>{{ data.description }}</p>
      <div class="password" role="status" aria-label="Показанный пароль">{{ password() }}</div>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="copy()">Скопировать пароль</button>
      <button mat-flat-button type="button" (click)="close()">Готово</button>
    </mat-dialog-actions>
  `,
  styles: `
    p {
      max-width: 30rem;
    }
    .password {
      margin-block: 1rem;
      padding: 1rem;
      border-radius: 0.5rem;
      background: var(--mat-sys-surface-container-high);
      font:
        600 1.15rem/1.4 ui-monospace,
        SFMono-Regular,
        Menlo,
        monospace;
      overflow-wrap: anywhere;
      user-select: all;
    }
  `,
})
export class PasswordDialogComponent implements OnDestroy {
  readonly data = inject<PasswordDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<PasswordDialogComponent>);
  private readonly clipboard = inject(Clipboard);
  private readonly snackBar = inject(MatSnackBar);
  readonly password = signal(this.data.password);

  copy(): void {
    if (this.clipboard.copy(this.password())) {
      this.snackBar.open(UI_TEXT.copied, undefined, { duration: 2500 });
    }
  }

  close(): void {
    this.clear();
    this.dialogRef.close();
  }

  ngOnDestroy(): void {
    this.clear();
  }

  private clear(): void {
    this.password.set('');
    this.data.password = '';
  }
}

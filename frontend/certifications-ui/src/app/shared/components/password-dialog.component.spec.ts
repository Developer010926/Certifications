import { Clipboard } from '@angular/cdk/clipboard';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PasswordDialogComponent, PasswordDialogData } from './password-dialog.component';

describe('PasswordDialogComponent', () => {
  it('clears the revealed password from dialog and caller state on close', () => {
    const data: PasswordDialogData = {
      title: 'Password',
      description: 'Sensitive',
      password: 'Secret123',
    };
    TestBed.configureTestingModule({
      imports: [PasswordDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: { close: vi.fn() } },
        { provide: Clipboard, useValue: { copy: vi.fn(() => true) } },
        { provide: MatSnackBar, useValue: { open: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(PasswordDialogComponent);
    fixture.componentInstance.close();
    expect(fixture.componentInstance.password()).toBe('');
    expect(data.password).toBe('');
  });
});

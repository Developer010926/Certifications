import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MATERIAL_IMPORTS } from '../../shared/material/material-imports';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink, ...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="centered-page">
      <mat-card appearance="outlined">
        <mat-card-header><mat-card-title>Page not found</mat-card-title></mat-card-header>
        <mat-card-content><p>The requested page does not exist.</p></mat-card-content>
        <mat-card-actions
          ><a mat-flat-button routerLink="/">Go to the application</a></mat-card-actions
        >
      </mat-card>
    </main>
  `,
})
export class NotFoundComponent {}

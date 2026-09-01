import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

const STATUS_LABELS: Readonly<Record<string, string>> = {
  NotApplicable: 'Not applicable',
  ContractValid: 'Contract valid',
  CertificationPending: 'Certification pending',
  CertificationInProgress: 'Certification in progress',
  CertificationMissing: 'Certification missing',
};

@Component({
  selector: 'app-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="status-badge" [class]="cssClass()" [attr.aria-label]="'Status: ' + label()">
      {{ label() }}
    </span>
  `,
  styles: `
    .status-badge {
      display: inline-flex;
      align-items: center;
      min-height: 1.75rem;
      padding: 0.15rem 0.65rem;
      border: 1px solid currentColor;
      border-radius: 999px;
      background: var(--mat-sys-surface-container-low);
      color: var(--mat-sys-on-surface-variant);
      font-size: 0.78rem;
      font-weight: 650;
      line-height: 1.2;
      white-space: nowrap;
    }
    .status-valid {
      color: #176b3a;
      background: #e7f6ec;
    }
    .status-pending {
      color: #7a5300;
      background: #fff3d5;
    }
    .status-progress {
      color: #155e75;
      background: #e2f5fb;
    }
    .status-missing {
      color: #a12622;
      background: #fdebea;
    }
  `,
})
export class StatusBadgeComponent {
  readonly status = input<string | null | undefined>();
  readonly label = computed(
    () => STATUS_LABELS[this.status() ?? ''] ?? `Unknown (${this.status() ?? '—'})`,
  );
  readonly cssClass = computed(() => {
    const value = this.status();
    if (value === 'ContractValid') return 'status-badge status-valid';
    if (value === 'CertificationPending') return 'status-badge status-pending';
    if (value === 'CertificationInProgress') return 'status-badge status-progress';
    if (value === 'CertificationMissing') return 'status-badge status-missing';
    return 'status-badge';
  });
}

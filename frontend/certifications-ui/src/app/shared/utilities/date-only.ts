import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function fromDateOnly(value: string | null | undefined): Date | null {
  if (!value) {
    return null;
  }

  const [year, month, day] = value.split('-').map(Number);
  if (!year || !month || !day) {
    return null;
  }

  return new Date(year, month - 1, day);
}

export function toDateOnly(value: Date | null | undefined): string | null {
  if (!value || Number.isNaN(value.getTime())) {
    return null;
  }

  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, '0');
  const day = String(value.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export const dateRangeValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const from = control.get('validToFrom')?.value as Date | null | undefined;
  const to = control.get('validToTo')?.value as Date | null | undefined;
  return from && to && from > to ? { dateRange: true } : null;
};

export const contractThresholdValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const warning = Number(control.get('prolongationWarningMonths')?.value);
  const alert = Number(control.get('prolongationAlertMonths')?.value);
  const years = Number(control.get('prolongationForYears')?.value);
  const errors: ValidationErrors = {};

  if (warning < 0 || alert < 0) {
    errors['nonNegativeThresholds'] = true;
  }
  if (alert >= warning) {
    errors['thresholdOrder'] = true;
  }
  if (years <= 0) {
    errors['positiveYears'] = true;
  }

  return Object.keys(errors).length ? errors : null;
};

export const certificationDateOrderValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const certification = control.get('certificationDate')?.value as Date | null | undefined;
  const protocol = control.get('protocolDate')?.value as Date | null | undefined;
  const sent = control.get('prolongationSend')?.value as Date | null | undefined;
  const returned = control.get('prolongationReturned')?.value as Date | null | undefined;

  if (sent && !protocol) {
    return { protocolRequired: true };
  }
  if (returned && !sent) {
    return { sentRequired: true };
  }
  if (certification && protocol && certification > protocol) {
    return { certificationOrder: true };
  }
  if (protocol && sent && protocol > sent) {
    return { protocolOrder: true };
  }
  if (sent && returned && sent > returned) {
    return { returnOrder: true };
  }

  return null;
};

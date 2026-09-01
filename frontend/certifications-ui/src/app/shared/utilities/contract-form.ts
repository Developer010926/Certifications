import { FormControl, FormGroup, Validators } from '@angular/forms';
import type { CreateContractRequest } from '../../core/api/generated/certificationsApiV1.schemas';
import { contractThresholdValidator, toDateOnly } from './date-only';

export function createContractForm(): FormGroup {
  return new FormGroup(
    {
      position: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      department: new FormControl('', { nonNullable: true }),
      division: new FormControl('', { nonNullable: true }),
      contractDate: new FormControl<Date | null>(null, { validators: [Validators.required] }),
      validTo: new FormControl<Date | null>(null),
      prolongationWarningMonths: new FormControl(3, {
        nonNullable: true,
        validators: [Validators.required, Validators.min(0)],
      }),
      prolongationAlertMonths: new FormControl(1, {
        nonNullable: true,
        validators: [Validators.required, Validators.min(0)],
      }),
      prolongationForYears: new FormControl(1, {
        nonNullable: true,
        validators: [Validators.required, Validators.min(1)],
      }),
    },
    { validators: [contractThresholdValidator] },
  );
}

export function toContractRequest(form: FormGroup): CreateContractRequest {
  const value = form.getRawValue() as {
    position: string;
    department: string;
    division: string;
    contractDate: Date | null;
    validTo: Date | null;
    prolongationWarningMonths: number;
    prolongationAlertMonths: number;
    prolongationForYears: number;
  };
  const contractDate = toDateOnly(value.contractDate);
  if (!contractDate) {
    throw new Error('Contract date is required.');
  }
  return {
    position: value.position.trim(),
    department: value.department.trim() || null,
    division: value.division.trim() || null,
    contractDate,
    validTo: toDateOnly(value.validTo),
    prolongationWarningMonths: value.prolongationWarningMonths,
    prolongationAlertMonths: value.prolongationAlertMonths,
    prolongationForYears: value.prolongationForYears,
  };
}

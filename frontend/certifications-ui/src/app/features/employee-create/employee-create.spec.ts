import { buildCreateEmployeeForm } from './employee-create.component';

describe('create employee form', () => {
  it('requires employee identity and first-contract fields and applies contract defaults', () => {
    const { form, contractForm } = buildCreateEmployeeForm();
    expect(form.valid).toBe(false);
    expect(contractForm.get('prolongationWarningMonths')?.value).toBe(3);
    expect(contractForm.get('prolongationAlertMonths')?.value).toBe(1);
    expect(contractForm.get('prolongationForYears')?.value).toBe(1);

    form.patchValue({ personalId: 'EMP-1', firstName: 'Ada', lastName: 'Lovelace' });
    contractForm.patchValue({ position: 'Engineer', contractDate: new Date(2026, 8, 1) });
    expect(form.valid).toBe(true);
  });

  it('rejects invalid warning and alert thresholds', () => {
    const { contractForm } = buildCreateEmployeeForm();
    contractForm.patchValue({ prolongationWarningMonths: 1, prolongationAlertMonths: 1 });
    expect(contractForm.hasError('thresholdOrder')).toBe(true);
  });
});

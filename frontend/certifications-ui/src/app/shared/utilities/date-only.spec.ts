import { FormControl, FormGroup } from '@angular/forms';
import { certificationDateOrderValidator, fromDateOnly, toDateOnly } from './date-only';

describe('date-only utilities and certification ordering', () => {
  it('round-trips a calendar date without UTC conversion', () => {
    expect(toDateOnly(fromDateOnly('2026-09-01'))).toBe('2026-09-01');
  });

  it('rejects out-of-order certification workflow dates', () => {
    const form = new FormGroup(
      {
        certificationDate: new FormControl(new Date(2026, 8, 10)),
        protocolDate: new FormControl(new Date(2026, 8, 9)),
        prolongationSend: new FormControl<Date | null>(null),
        prolongationReturned: new FormControl<Date | null>(null),
      },
      { validators: [certificationDateOrderValidator] },
    );
    expect(form.hasError('certificationOrder')).toBe(true);
  });
});

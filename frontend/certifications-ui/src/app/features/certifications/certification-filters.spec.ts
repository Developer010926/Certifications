import { GetCertificationOverviewStatus } from '../../core/api/generated/certificationsApiV1.schemas';
import { buildCertificationOverviewParams } from './certification-overview.component';

describe('certification overview filters', () => {
  it('maps filters to the canonical server query without UTC date conversion', () => {
    const params = buildCertificationOverviewParams(
      {
        name: '  Ada  ',
        department: '  IT ',
        status: GetCertificationOverviewStatus.CertificationPending,
        validToFrom: new Date(2026, 8, 1),
        validToTo: new Date(2026, 8, 30),
        includeInactive: true,
      },
      2,
      25,
      'effectiveValidTo',
      'desc',
    );
    expect(params).toEqual({
      page: 3,
      pageSize: 25,
      name: 'Ada',
      department: 'IT',
      status: 'CertificationPending',
      validToFrom: '2026-09-01',
      validToTo: '2026-09-30',
      includeInactive: true,
      sort: 'effectiveValidTo',
      direction: 'desc',
    });
  });
});

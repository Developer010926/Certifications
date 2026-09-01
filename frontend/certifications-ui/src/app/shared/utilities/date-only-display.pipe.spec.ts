import { DateOnlyDisplayPipe } from './date-only-display.pipe';

describe('DateOnlyDisplayPipe', () => {
  const pipe = new DateOnlyDisplayPipe();

  it('formats an API date in Russian display order', () => {
    expect(pipe.transform('2026-09-01')).toBe('01.09.2026');
  });

  it('does not display invalid or missing dates', () => {
    expect(pipe.transform('2026-02-31')).toBe('');
    expect(pipe.transform(null)).toBe('');
  });
});

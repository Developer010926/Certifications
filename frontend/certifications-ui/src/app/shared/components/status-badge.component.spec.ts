import { TestBed } from '@angular/core/testing';
import { StatusBadgeComponent } from './status-badge.component';

describe('StatusBadgeComponent localization', () => {
  it('renders a Russian status label and accessible name', () => {
    TestBed.configureTestingModule({ imports: [StatusBadgeComponent] });
    const fixture = TestBed.createComponent(StatusBadgeComponent);
    fixture.componentRef.setInput('status', 'CertificationInProgress');
    fixture.detectChanges();
    const badge = fixture.nativeElement.querySelector('.status-badge') as HTMLElement;
    expect(badge.textContent).toContain('Сертификация в процессе');
    expect(badge.getAttribute('aria-label')).toBe('Статус: Сертификация в процессе');
  });

  it('uses a Russian fallback for an unknown status', () => {
    TestBed.configureTestingModule({ imports: [StatusBadgeComponent] });
    const fixture = TestBed.createComponent(StatusBadgeComponent);
    fixture.componentRef.setInput('status', 'FutureStatus');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Неизвестный статус (FutureStatus)');
  });
});

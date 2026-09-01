import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-empty-route',
  template: '',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyRouteComponent {}

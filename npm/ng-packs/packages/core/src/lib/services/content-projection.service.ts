import { Injectable, Injector, TemplateRef, Type, inject } from '@angular/core';
import { ProjectionStrategy } from '../strategies/projection.strategy';

@Injectable({ providedIn: 'root' })
export class ContentProjectionService {
  private injector = inject(Injector);

  /** Inserted by Angular inject() migration for backwards compatibility */
  constructor(...args: unknown[]);

  constructor() {}

  projectContent<T extends Type<any> | TemplateRef<any>>(
    projectionStrategy: ProjectionStrategy<T>,
    injector = this.injector,
  ) {
    return projectionStrategy.injectContent(injector);
  }
}

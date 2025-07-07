import { Directive, TemplateRef, inject } from '@angular/core';

@Directive({
  selector: '[abpTreeExpandedIconTemplate],[abp-tree-expanded-icon-template]',
})
export class ExpandedIconTemplateDirective {
  template = inject<TemplateRef<any>>(TemplateRef);

  /** Inserted by Angular inject() migration for backwards compatibility */
  constructor(...args: unknown[]);

  constructor() {}
}

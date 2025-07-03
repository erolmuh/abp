import { Directive, TemplateRef, inject } from '@angular/core';

@Directive({
  selector: '[abpTreeExpandedIconTemplate],[abp-tree-expanded-icon-template]',
})
export class ExpandedIconTemplateDirective {
  public template = inject(TemplateRef<any>);
}

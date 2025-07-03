import { Directive, TemplateRef, inject } from '@angular/core';

@Directive({
  selector: '[abpTreeNodeTemplate],[abp-tree-node-template]',
})
export class TreeNodeTemplateDirective {
  public template = inject(TemplateRef<any>);
}

/* eslint-disable @angular-eslint/no-input-rename */
import {
  Directive,
  Injector,
  Input,
  OnChanges,
  OnDestroy,
  TemplateRef,
  ViewContainerRef,
  inject,
} from '@angular/core';
import { PropData, PropList } from '../models/props';

@Directive({
  exportAs: 'abpPropData',
  selector: '[abpPropData]',
})
export class PropDataDirective<L extends PropList<any>>
  extends PropData<InferredData<L>>
  implements OnChanges, OnDestroy
{
  private tempRef = inject(TemplateRef<any>);
  private vcRef = inject(ViewContainerRef);
  private injector = inject(Injector);

  @Input('abpPropDataFromList') propList?: L;

  @Input('abpPropDataWithRecord') record!: InferredData<L>['record'];

  @Input('abpPropDataAtIndex') index?: number;

  readonly getInjected: InferredData<L>['getInjected'];

  constructor() {
    super();
    this.getInjected = this.injector.get.bind(this.injector);
  }

  ngOnChanges() {
    this.vcRef.clear();

    this.vcRef.createEmbeddedView(this.tempRef, {
      $implicit: this.data,
      index: 0,
    });
  }

  ngOnDestroy() {
    this.vcRef.clear();
  }
}

type InferredData<L> = PropData<InferredRecord<L>>;
type InferredRecord<L> = L extends PropList<infer R> ? R : never;

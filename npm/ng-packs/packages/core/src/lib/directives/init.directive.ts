import { Directive, Output, EventEmitter, ElementRef, AfterViewInit, inject } from '@angular/core';

@Directive({
  selector: '[abpInit]',
})
export class InitDirective implements AfterViewInit {
  private elRef = inject(ElementRef);

  @Output('abpInit') readonly init = new EventEmitter<ElementRef<any>>();

  /** Inserted by Angular inject() migration for backwards compatibility */
  constructor(...args: unknown[]);

  constructor() {}

  ngAfterViewInit() {
    this.init.emit(this.elRef);
  }
}

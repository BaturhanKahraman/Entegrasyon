import { Directive, ElementRef } from '@angular/core';

@Directive({
  selector: '[nestedMenu]'
})
export class NestedMenuDirective {

  constructor(private el: ElementRef) { 
    this.el.nativeElement.style.paddingRight="16px"
    this.el.nativeElement.style.paddingLeft="16px"
    this.el.nativeElement.style.fontSize="16px"
  }

}

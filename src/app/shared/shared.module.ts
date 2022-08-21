import { NgModule } from '@angular/core';
import { FlexLayoutModule } from '@angular/flex-layout';
import { MaterialModule } from '../material.module';
import { CommonModule } from '@angular/common';
import { NestedMenuDirective } from './directives/nested-menu.directive';

@NgModule({
  declarations:[NestedMenuDirective],
  imports: [
    
  ],
  exports:[
    FlexLayoutModule,
    MaterialModule,
    CommonModule,
    NestedMenuDirective
  ]
  
})
export class SharedModule { }

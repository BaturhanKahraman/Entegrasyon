import { NgModule } from '@angular/core';
import { FlexLayoutModule } from '@angular/flex-layout';
import { MaterialModule } from '../material.module';
import { CommonModule } from '@angular/common';
import { NestedMenuDirective } from './directives/nested-menu.directive';
import { HasAccessDirective } from './directives/has-access.directive';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { TurkishPaginatorIntl } from './translate/TurkishPaginatorIntl.service';

@NgModule({
  declarations:[NestedMenuDirective,HasAccessDirective],
  imports: [
    
  ],
  exports:[
    FlexLayoutModule,
    MaterialModule,
    CommonModule,
    NestedMenuDirective,
    HasAccessDirective
  ],
  providers:[{provide: MatPaginatorIntl, useClass: TurkishPaginatorIntl}]
  
})
export class SharedModule { }

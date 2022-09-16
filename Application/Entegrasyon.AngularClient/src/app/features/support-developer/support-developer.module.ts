import { NgModule } from '@angular/core';
import { SupportDeveloperComponent } from './support-developer.component';
import { RouterModule } from '@angular/router';



@NgModule({
  declarations: [SupportDeveloperComponent],
  imports: [
    RouterModule.forChild([{path:'',component:SupportDeveloperComponent}])
  ],
  exports:[RouterModule]
})
export class SupportDeveloperModule { }

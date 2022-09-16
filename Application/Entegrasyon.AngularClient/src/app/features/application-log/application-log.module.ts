import { NgModule } from '@angular/core';
import { ApplicationLogComponent } from './application-log.component';
import { SharedModule } from 'src/app/shared/shared.module';
import {  RouterModule, Routes } from '@angular/router';
import { ApplicationLogListComponent } from './application-log-list/application-log-list.component';

const routes :Routes =[{
  path:'',component:ApplicationLogComponent
}];
@NgModule({
  imports: [
    SharedModule,
    RouterModule.forChild(routes)
  ],
  declarations: [ApplicationLogComponent,ApplicationLogListComponent],
  exports:[RouterModule]
})
export class ApplicationLogModule { }

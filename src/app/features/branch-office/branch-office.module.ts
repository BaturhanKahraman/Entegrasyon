import { NgModule } from '@angular/core';
import { BranchOfficeComponent } from './branch-office.component';
import { SharedModule } from 'src/app/shared/shared.module';
import { RouterModule, Routes, ROUTES } from '@angular/router';
import { BranchListComponent } from './branch-list/branch-list.component';


const routes : Routes = [
  {path:"",component:BranchOfficeComponent,children:[
    {path:"",component:BranchListComponent}
  ]}
]
@NgModule({
  imports: [
    SharedModule,RouterModule.forChild(routes)
  ],
  exports:[RouterModule],
  declarations: [BranchOfficeComponent,BranchListComponent]
})
export class BranchOfficeModule { }

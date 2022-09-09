import { NgModule } from '@angular/core';
import { BranchOfficeComponent } from './branch-office.component';
import { SharedModule } from 'src/app/shared/shared.module';
import { RouterModule, Routes, ROUTES } from '@angular/router';
import { BranchListComponent } from './branch-list/branch-list.component';
import { BranchAddDialogComponent } from './branch-add-dialog/branch-add-dialog.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { BranchEditDialogComponent } from './branch-edit-dialog/branch-edit-dialog.component';


const routes : Routes = [
  {path:"",component:BranchOfficeComponent,children:[
    {path:"",component:BranchListComponent}
  ]}
]
@NgModule({
  imports: [
    SharedModule,RouterModule.forChild(routes),FormsModule,ReactiveFormsModule
  ],
  exports:[RouterModule],
  declarations: [BranchOfficeComponent,BranchListComponent,BranchAddDialogComponent,BranchEditDialogComponent]
})
export class BranchOfficeModule { }

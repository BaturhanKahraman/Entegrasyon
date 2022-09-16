import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable } from 'rxjs';
import { BranchOfficeService } from 'src/app/core/services/branch-office.service';
import { BranchOfficeModel } from 'src/app/shared/models/branch-office.model';
import { BranchEditDialogComponent } from '../branch-edit-dialog/branch-edit-dialog.component';

@Component({
  selector: 'app-branch-list',
  templateUrl: './branch-list.component.html',
  styleUrls: ['./branch-list.component.css']
})
export class BranchListComponent implements OnInit {
  branches$ : Observable<BranchOfficeModel[]>;
  displayedColumns=['id','name'];
  constructor(private branchService:BranchOfficeService,private matDialog : MatDialog) { }

  ngOnInit() {
    this.branches$=this.branchService.branches$;
  }
  openEditDialog(id:number){
    this.matDialog.open(BranchEditDialogComponent,{disableClose:true,hasBackdrop:true,data:id})
  }
}

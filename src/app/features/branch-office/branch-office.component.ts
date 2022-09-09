import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { BranchAddDialogComponent } from './branch-add-dialog/branch-add-dialog.component';

@Component({
  selector: 'app-branch-office',
  templateUrl: './branch-office.component.html',
  styleUrls: ['./branch-office.component.css']
})
export class BranchOfficeComponent implements OnInit {

  constructor(public dialog: MatDialog) { }

  ngOnInit() {
  }
  openAddDialog(){
    this.dialog.open(BranchAddDialogComponent)
  }
}

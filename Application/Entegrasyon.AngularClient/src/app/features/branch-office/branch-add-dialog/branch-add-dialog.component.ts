import { Component, Inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { map, tap } from 'rxjs';
import { BranchOfficeService } from 'src/app/core/services/branch-office.service';
import { BranchOfficeDetailModel } from 'src/app/shared/models/branch-office-detail.model';
import { BranchOfficeModel } from 'src/app/shared/models/branch-office.model';

@Component({
  selector: 'app-branch-add',
  templateUrl: './branch-add-dialog.component.html',
  styleUrls: ['./branch-add-dialog.component.css'],
})
export class BranchAddDialogComponent implements OnInit {
  branchForm: FormGroup;
  branchOfficeDetail:BranchOfficeDetailModel;
  constructor(
    public dialogRef: MatDialogRef<BranchAddDialogComponent>,
    private branchService: BranchOfficeService,
    private snackBar: MatSnackBar,
    @Inject(MAT_DIALOG_DATA) public data: BranchOfficeModel
  ) {}

  ngOnInit() {
    this.branchForm = new FormGroup({
      branchOffice: new FormControl(null, Validators.required),
    });
  }
  submit() {
    if (this.branchForm.valid)
      this.branchService
        .addBranch(this.branchForm.value.branchOffice)
        .pipe(tap((x) => this.snackBar.open(x.message)))
        .subscribe((x) => {
          if (x.success) this.dialogRef.close();
          this.branchService.init();
        });
  }
  onNoClick(){
    this.dialogRef.close();
  }
  
}

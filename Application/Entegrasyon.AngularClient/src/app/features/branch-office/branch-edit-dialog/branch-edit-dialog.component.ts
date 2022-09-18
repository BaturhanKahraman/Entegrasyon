import { Component, Inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSlideToggleChange } from '@angular/material/slide-toggle';
import { MatSnackBar } from '@angular/material/snack-bar';
import { map, tap } from 'rxjs';
import { BranchOfficeService } from 'src/app/core/services/branch-office.service';
import { BranchOfficeDetailModel } from 'src/app/shared/models/branch-office-detail.model';
import { BranchOfficeModel } from 'src/app/shared/models/branch-office.model';

@Component({
  selector: 'app-branch-edit-dialog',
  templateUrl: './branch-edit-dialog.component.html',
  styleUrls: ['./branch-edit-dialog.component.css'],
})
export class BranchEditDialogComponent implements OnInit {
  disabled = true;
  branchOfficeDetail: BranchOfficeDetailModel;
  branchForm: FormGroup;
  constructor(
    public dialogRef: MatDialogRef<BranchEditDialogComponent>,
    private branchService: BranchOfficeService,
    private snackBar: MatSnackBar,
    @Inject(MAT_DIALOG_DATA) public data: number
  ) {}

  ngOnInit() {
    this.branchForm = new FormGroup({
      id: new FormControl(),
      name: new FormControl(
        { value: null, disabled: this.disabled },
        Validators.required
      ),
    });
    this.getBranchDetail();
  }
  submit() {
    if (this.branchForm.valid) {
      let body:BranchOfficeModel = Object.assign(this.branchForm.value);
      this.branchService
        .editBranch(body)
        .pipe(
          tap((x) => {
            if (x.success !== true)
              this.snackBar.open(x.message, undefined, { duration: 5000 });
          }),
          map((x) => x.data)
        )
        .subscribe(this.patchValues).add(()=>{
          this.branchService.init();
          this.dialogRef.close();
          this.snackBar.open("Başarıyla eklendi.", undefined, { duration: 5000 });
        });
    }
  }
  deleteBranch() {
    const question =
      this.branchOfficeDetail.name +
      ' isimli şubeyi silmek istediğinizden emin misiniz ?';
    if (confirm(question)) {
      this.branchService.deleteBranch(this.branchForm.value.id).pipe(tap(x=>this.snackBar.open(x.message,'Tamam',{duration:5000})))
      .subscribe(()=>{this.branchService.init();this.dialogRef.close();});
    }
  }
  onNoClick() {
    this.dialogRef.close();
  }
  getBranchDetail() {
    if (this.data)
      this.branchService
        .GetBranchDetail(this.data)
        .pipe(
          tap((x) => {
            if (x.success !== true)
              this.snackBar.open(x.message, undefined, { duration: 5000 });
          }),
          map((x) => x.data)
        )
        .subscribe(this.patchValues);
  }

  changeControlEnability(toggleChange: MatSlideToggleChange) {
    if (toggleChange.checked) {
      this.branchForm.get('name')?.enable();
      this.disabled = false;
    } else {
      this.branchForm.get('name')?.disable();
      this.disabled = true;
    }
  }

  patchValues = (x: BranchOfficeDetailModel): void => {
    this.branchOfficeDetail = x;
    this.branchForm.patchValue({
      id: x.id,
      name: x.name,
    });
  };
}

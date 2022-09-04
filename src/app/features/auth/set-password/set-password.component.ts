import { Component, OnInit } from '@angular/core';
import {
  AbstractControl,
  AsyncValidator,
  AsyncValidatorFn,
  FormControl,
  FormGroup,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ActivatedRoute, Route, Router } from '@angular/router';
import { map, tap } from 'rxjs';
import { AuthService } from 'src/app/core/services/auth.service';
import { LoginSetPasswordModel } from 'src/app/shared/models/login-set-password.model';

@Component({
  selector: 'app-set-password',
  templateUrl: './set-password.component.html',
  styleUrls: ['./set-password.component.scss'],
})
export class SetPasswordComponent implements OnInit {
  setPasswordForm: FormGroup;
  isLoading = false;
  constructor(private authService: AuthService,private activatedRoute:ActivatedRoute,
    private router:Router,private snackBar:MatSnackBar) {}

  ngOnInit() {
    this.initializePasswordForm();
    this.activatedRoute.queryParams.pipe(map(x=>x["userId"])).subscribe(
      x=>this.setPasswordForm.patchValue({"userId":x})
    );
  }

  initializePasswordForm() {
    this.setPasswordForm = new FormGroup(
      {
        "userId":new FormControl('',Validators.required),
        "password": new FormControl('', Validators.required),
        "passwordConfirm": new FormControl('', Validators.required),
      },
      this.passwordConfirming
    );
  }
  setPassword() {
    if (this.isLoading == true || !this.setPasswordForm.valid) return;
    this.isLoading = true;
    this.isLoading = false;
    const loginSetPassword:LoginSetPasswordModel = Object.assign(this.setPasswordForm.value)
    this.authService.setFirstPassword(loginSetPassword)
    .pipe(tap(x=>this.snackBar.open(x.message,"Tamam",{duration:5000})))
    .subscribe(()=>this.router.navigate(["auth","login"]));
  }
  passwordConfirming(c: AbstractControl): ValidationErrors | null {
    if (c.get('password')!.value === c.get('passwordConfirm')!.value) {
        return null;
    }
    return {notMatches:true}
  }
}

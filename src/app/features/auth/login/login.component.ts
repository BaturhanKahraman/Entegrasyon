import { Component, OnInit } from '@angular/core';
import { UntypedFormControl, UntypedFormGroup, Validators } from '@angular/forms';
import { LoginModel } from 'src/app/shared/models/login.model';
import { AuthService } from 'src/app/core/services/auth.service';
import { map } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  isLoading=false;
  isLogining=false;
  loginForm!:UntypedFormGroup;
  constructor(private authService:AuthService,private snackBar:MatSnackBar) { }

  ngOnInit(): void {
    this.loginForm = new UntypedFormGroup({
      'username': new UntypedFormControl(null,[Validators.required,Validators.maxLength(100)]),
      'password':new UntypedFormControl(null,[Validators.required,Validators.minLength(5),Validators.maxLength(100)]),
    });
  }

  login(){
    if(this.isLogining)
      return;
    this.isLogining = true;
    this.isLoading = true;
    let loginModel:LoginModel=Object.assign(this.loginForm.value)
    this.authService.login(loginModel).pipe(map(x=>x.message))
    .subscribe()
    .add(()=>{
      this.isLoading=false;
      this.isLogining = false;
    });
  }

}

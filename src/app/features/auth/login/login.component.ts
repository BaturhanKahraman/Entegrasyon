import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { LoginModel } from 'src/app/shared/models/login.model';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit {
  isLoading=false;
  isLogining=false;
  loginForm!:FormGroup;
  constructor(private authService:AuthService) { }

  ngOnInit(): void {
    this.loginForm = new FormGroup({
      'email': new FormControl(null,[Validators.required,Validators.email,Validators.minLength(5),Validators.maxLength(100)]),
      'password':new FormControl(null,[Validators.required,Validators.minLength(5),Validators.maxLength(100)]),
    });
  }

  login(){
    if(this.isLogining)
      return;
    this.isLogining = true;
    this.isLoading = true;
    console.log(this.loginForm);
    let loginModel:LoginModel=Object.assign(this.loginForm.value)
    console.log(loginModel);
    this.authService.
    login(loginModel)
    .subscribe((result)=>{
      console.log(result);
      
    }).add(()=>{
      this.isLoading=false;
    });
  }

}

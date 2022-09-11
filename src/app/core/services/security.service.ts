import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { RoleClaimModel } from 'src/app/shared/models/role-claim.model';
import { RoleService } from './role.service';

@Injectable({
  providedIn: 'root',
})
export class SecurityService {
  private userClaimsSubject=new BehaviorSubject<RoleClaimModel[]>([]);
  userClaims$:Observable<RoleClaimModel[]> = this.userClaimsSubject.asObservable();
  constructor(private roleService: RoleService) {}

  init(){
    const roleClaims =this.roleService.getClaimsFromToken().map<RoleClaimModel>(x=>{return {name:x,description:''}});
    this.userClaimsSubject.next(roleClaims);
  }


}

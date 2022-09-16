import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { RoleClaimModel } from 'src/app/shared/models/role-claim.model';
import { RoleService } from './role.service';

@Injectable({
  providedIn: 'root',
})
export class SecurityService {
  private userClaimsSubject=new BehaviorSubject<string[]>([]);
  constructor(private roleService: RoleService) {}

  init(){
    const roleClaims =this.roleService.getClaimsFromToken();
    this.userClaimsSubject.next(roleClaims);
  }

  checkPermission(permission:string):boolean{
    const claims = this.userClaimsSubject.getValue();
    return claims.includes(permission);
  }

}

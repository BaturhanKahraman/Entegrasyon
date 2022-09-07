import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { BranchOfficeModel } from 'src/app/shared/models/branch-office.model';
import { UserDetailModel } from 'src/app/shared/models/user-detail.model';
import { BranchOfficeService } from './branch-office.service';
import { UserService } from './user.service';

@Injectable()
export class StoreService {
  private usersSubject = new BehaviorSubject<UserDetailModel[]>([]);
  usersDetails$ = this.usersSubject.asObservable();
  private branchSubject = new BehaviorSubject<BranchOfficeModel[]>([]);
  branches$ = this.usersSubject.asObservable();
  private userCount: BehaviorSubject<number> = new BehaviorSubject(0);
  userCount$ = this.userCount.asObservable();
  constructor(private userService: UserService,private branchService:BranchOfficeService) {}
  init() {
    this.userService
      .getUserDetail()
      .subscribe((x) => this.usersSubject.next(x));
    this.userService.getUserCount().subscribe((x) => this.userCount.next(x));
    this.branchService.getBranches().subscribe(branches=>this.branchSubject.next(branches))
  }
}

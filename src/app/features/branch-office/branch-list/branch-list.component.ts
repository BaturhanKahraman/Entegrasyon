import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { StoreService } from 'src/app/core/services/store.service';
import { BranchOfficeModel } from 'src/app/shared/models/branch-office.model';

@Component({
  selector: 'app-branch-list',
  templateUrl: './branch-list.component.html',
  styleUrls: ['./branch-list.component.css']
})
export class BranchListComponent implements OnInit {
  branches$ : Observable<BranchOfficeModel[]>;
  displayedColumns=['id','name'];
  constructor(private store:StoreService) { }

  ngOnInit() {
    this.branches$=this.store.branches$;
  }

}

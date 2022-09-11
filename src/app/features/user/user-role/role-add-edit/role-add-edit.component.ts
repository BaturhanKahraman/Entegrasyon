import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-role-add-edit',
  templateUrl: './role-add-edit.component.html',
  styleUrls: ['./role-add-edit.component.css']
})
export class RoleAddEditComponent implements OnInit {
  isEdit:boolean;
  constructor() { }

  ngOnInit() {
  }

}

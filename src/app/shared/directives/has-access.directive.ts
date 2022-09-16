import { Directive, Input, TemplateRef, ViewContainerRef } from '@angular/core';
import { SecurityService } from 'src/app/core/services/security.service';
import { NoPermissionComponent } from '../components/no-permission/no-permission.component';

@Directive({
  selector: '[hasAccess]'
})
export class HasAccessDirective {
  constructor(private templateRef: TemplateRef<any>,
    private viewContainer: ViewContainerRef,
    private securityService:SecurityService) { }

    @Input() set hasAccess(permission:string){
      if(this.securityService.checkPermission(permission)){
        this.viewContainer.createEmbeddedView(this.templateRef);
      }else{
        this.viewContainer.clear();
        this.viewContainer.createComponent(NoPermissionComponent)
      }
    }

}

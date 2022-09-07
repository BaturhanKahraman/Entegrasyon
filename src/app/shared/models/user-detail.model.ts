export class UserDetailModel{
    id:number;
    name:string;
    surname:string;
    username:string;
    isActive:boolean;
    isTwoFactorAuthActive:boolean;
    needsTakeNewPassword:boolean;
    createdAt:Date;
    defaultOfficeName:string;
}
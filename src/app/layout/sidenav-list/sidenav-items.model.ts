export class SideNavItemsModel{
    claimName:string;
    routerLink:string;
    content:string;
    matIcon:string;
    order:number;
    IsBottom:boolean;    
}

export var sidenavs:SideNavItemsModel[] =[
    {claimName:'',content:'Anasayfa',matIcon:'home',routerLink:'/',order:1,IsBottom:false},
    {claimName:'product',content:'Ürünler',matIcon:'hive',routerLink:'/product',order:2,IsBottom:false},
    {claimName:'category',content:'Kategoriler',matIcon:'category',routerLink:'/',order:3,IsBottom:false},
    {claimName:'sale',content:'Satış',matIcon:'point_of_sale',routerLink:'/',order:4,IsBottom:false},
    {claimName:'customer',content:'Müşteriler',matIcon:'contact_page',routerLink:'/',order:5,IsBottom:false},
    {claimName:'order',content:'Siparişler',matIcon:'shopping_cart',routerLink:'/',order:6,IsBottom:false},
    {claimName:'report',content:'Raporlar',matIcon:'summarize',routerLink:'/',order:7,IsBottom:false},
    {claimName:'branchoffice',content:'Ofisler',matIcon:'maps_home_work',routerLink:'/offices',order:8,IsBottom:false},
    {claimName:'cargo',content:'Kargo Firmaları',matIcon:'local_shipping',routerLink:'/',order:9,IsBottom:false},
    {claimName:'integration',content:'Entegrasyon',matIcon:'view_module',routerLink:'/',order:10,IsBottom:false},
    {claimName:'user',content:'Kullanıcılar',matIcon:'people',routerLink:'/user',order:11,IsBottom:false},
    {claimName:'log',content:'Sistem Kayıtları',matIcon:'view_headline',routerLink:'/',order:12,IsBottom:true},
    {claimName:'setting',content:'Ayarlar',matIcon:'settings',routerLink:'/',order:13,IsBottom:true},
    {claimName:'',content:'Destek',matIcon:'help',routerLink:'/support',order:14,IsBottom:true},
];
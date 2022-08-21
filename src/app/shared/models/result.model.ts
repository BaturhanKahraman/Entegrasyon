export interface Result<T>{
    message:string;
    data:T;
    success:boolean;
}
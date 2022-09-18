export class User {
    /**
     *
     */
    constructor(
      public email: string,
      public id: string,
      public name:string,
      public surname:string,
      public userName:string,
      private _token:string,
      private _tokenExpirationDate:Date) {
      
    }
      //public role:string[],
      
      get token(){
        
        if(!this._tokenExpirationDate || new Date() > this._tokenExpirationDate)
          return null;
        return this._token;
      }
    
  }
  
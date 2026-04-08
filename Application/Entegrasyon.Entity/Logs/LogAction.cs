namespace Entegrasyon.Entity.Logs;

public enum LogAction
{
    None=0,
    Add=1,
    Update,
    Delete,
    List,
    Sync,
    Import,
    Publish,
    Retry,
    RequestOpen = 9,
    RequestApprove = 10,
    RequestReject = 11,
    Transfer = 12
}
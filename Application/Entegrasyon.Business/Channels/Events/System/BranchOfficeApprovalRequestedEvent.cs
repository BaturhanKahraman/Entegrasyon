namespace Entegrasyon.Business.Channels.Events.System;

public sealed class BranchOfficeApprovalRequestedEvent : BaseEvent
{
    public int ApprovalId { get; set; }
    public int BranchOfficeId { get; set; }
    public Guid RequestedByUserId { get; set; }

    public BranchOfficeApprovalRequestedEvent() { }
    public BranchOfficeApprovalRequestedEvent(int approvalId, int branchOfficeId, Guid requestedByUserId)
    {
        ApprovalId = approvalId;
        BranchOfficeId = branchOfficeId;
        RequestedByUserId = requestedByUserId;
    }
}

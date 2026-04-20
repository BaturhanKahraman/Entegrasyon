namespace Entegrasyon.Business.Channels.Events.System;

public sealed class BranchOfficeApprovalRejectedEvent : BaseEvent
{
    public int ApprovalId { get; set; }
    public int BranchOfficeId { get; set; }
    public Guid RejectedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;

    public BranchOfficeApprovalRejectedEvent() { }
    public BranchOfficeApprovalRejectedEvent(int approvalId, int branchOfficeId, Guid rejectedByUserId, string reason)
    {
        ApprovalId = approvalId;
        BranchOfficeId = branchOfficeId;
        RejectedByUserId = rejectedByUserId;
        Reason = reason;
    }
}

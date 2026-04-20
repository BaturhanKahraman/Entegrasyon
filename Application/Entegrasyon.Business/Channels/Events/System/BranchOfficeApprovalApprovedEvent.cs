namespace Entegrasyon.Business.Channels.Events.System;

public sealed class BranchOfficeApprovalApprovedEvent : BaseEvent
{
    public int ApprovalId { get; set; }
    public int BranchOfficeId { get; set; }
    public Guid ApprovedByUserId { get; set; }

    public BranchOfficeApprovalApprovedEvent() { }
    public BranchOfficeApprovalApprovedEvent(int approvalId, int branchOfficeId, Guid approvedByUserId)
    {
        ApprovalId = approvalId;
        BranchOfficeId = branchOfficeId;
        ApprovedByUserId = approvedByUserId;
    }
}

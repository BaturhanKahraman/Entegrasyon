namespace MainDatabase.MainEntities;

public class Customer
{
    public int Id { get; set; }
    public string CompanyName { get; set; }
    public DateTime BoughtAt { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsCloudUsing { get; set; }
    public int MembershipPlanId { get; set; }
    public MembershipPlan MembershipPlan { get; set; }
    
    public int ConnectionInfoId { get; set; }
    public ConnectionInfo ConnectionInfo { get; set; }
}
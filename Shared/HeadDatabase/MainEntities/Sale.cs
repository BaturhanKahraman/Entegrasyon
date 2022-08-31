namespace MainDatabase.MainEntities;

public class Sale
{
    public int Id { get; set; }

    public decimal Price { get; set; }//money
    public int CustomerId { get; set; }
    public Customer Customer { get; set; }

    public int MemberShipId { get; set; }
    public MembershipPlan MembershipPlan { get; set; }
}
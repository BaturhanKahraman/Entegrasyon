namespace MainDatabase.MainEntities;

public class SupportTicket
{
    public int Id { get; set; }
    public int? CustomerId { get; set; }
    public Customer Customer { get; set; }

    public string NameSurname { get; set; }

    public string Description { get; set; }

    public DateTime CreatedAt { get; set; }

    
}
namespace Entegrasyon.Entity.Shipping;

public enum ShipmentStatus
{
    Created = 1,
    PickedUp = 2,
    InTransit = 3,
    OutForDelivery = 4,
    Delivered = 5,
    ReturnedToSender = 6,
    Failed = 7,
    Cancelled = 8
}

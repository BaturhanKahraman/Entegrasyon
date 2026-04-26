namespace Entegrasyon.Business.Abstract;

public interface ICurrentUserContext
{
    Guid? UserId { get; }
}

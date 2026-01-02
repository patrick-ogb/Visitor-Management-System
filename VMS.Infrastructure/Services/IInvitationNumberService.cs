using VMS.Infrastructure.Repositories;

namespace VMS.Infrastructure.Services;

public interface IInvitationNumberService
{
    Task<string> GenerateInvitationNumberAsync(int year, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate invitation number with transaction-aware locking to prevent race conditions.
    /// This method should be called within a database transaction.
    /// </summary>
    Task<string> GenerateInvitationNumberWithLockAsync(IUnitOfWork unitOfWork, int year, CancellationToken cancellationToken = default);
}















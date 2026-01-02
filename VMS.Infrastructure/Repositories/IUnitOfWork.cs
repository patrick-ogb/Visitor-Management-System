using VMS.Core.Entities;

namespace VMS.Infrastructure.Repositories;

public interface IUnitOfWork : IDisposable
{
    IRepository<Enterprise> Enterprises { get; }
    IRepository<Guest> Guests { get; }
    IRepository<Vehicle> Vehicles { get; }
    IRepository<GuestInvitation> GuestInvitations { get; }
    IRepository<Approval> Approvals { get; }
    IRepository<NotificationEvent> NotificationEvents { get; }
    IRepository<Notification> Notifications { get; }
    IRepository<UserPresence> UserPresences { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    
    // Get DbContext for advanced operations (e.g., raw SQL with locking)
    Microsoft.EntityFrameworkCore.DbContext GetDbContext();
}








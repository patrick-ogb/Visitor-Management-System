using Microsoft.EntityFrameworkCore.Storage;
using VMS.Core.Entities;
using VMS.Infrastructure.Data;

namespace VMS.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly VmsDbContext _context;
    private IDbContextTransaction? _transaction;

    private IRepository<Enterprise>? _enterprises;
    private IRepository<Guest>? _guests;
    private IRepository<Vehicle>? _vehicles;
    private IRepository<GuestInvitation>? _guestInvitations;
    private IRepository<Approval>? _approvals;
    private IRepository<NotificationEvent>? _notificationEvents;
    private IRepository<Notification>? _notifications;
    private IRepository<UserPresence>? _userPresences;

    public UnitOfWork(VmsDbContext context)
    {
        _context = context;
    }

    public IRepository<Enterprise> Enterprises =>
        _enterprises ??= new Repository<Enterprise>(_context);

    public IRepository<Guest> Guests =>
        _guests ??= new Repository<Guest>(_context);

    public IRepository<Vehicle> Vehicles =>
        _vehicles ??= new Repository<Vehicle>(_context);

    public IRepository<GuestInvitation> GuestInvitations =>
        _guestInvitations ??= new Repository<GuestInvitation>(_context);

    public IRepository<Approval> Approvals =>
        _approvals ??= new Repository<Approval>(_context);

    public IRepository<NotificationEvent> NotificationEvents =>
        _notificationEvents ??= new Repository<NotificationEvent>(_context);

    public IRepository<Notification> Notifications =>
        _notifications ??= new Repository<Notification>(_context);

    public IRepository<UserPresence> UserPresences =>
        _userPresences ??= new Repository<UserPresence>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public Microsoft.EntityFrameworkCore.DbContext GetDbContext()
    {
        return _context;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}








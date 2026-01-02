using Microsoft.EntityFrameworkCore;
using VMS.Infrastructure.Data;
using VMS.Infrastructure.Repositories;

namespace VMS.Infrastructure.Services;

// Helper class for raw SQL query result
internal class MaxSequenceResult
{
    public int? MaxSequence { get; set; }
}

public class InvitationNumberService : IInvitationNumberService
{
    private readonly IUnitOfWork _unitOfWork;

    public InvitationNumberService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GenerateInvitationNumberAsync(int year, CancellationToken cancellationToken = default)
    {
        var prefix = $"INV-{year}-";
        
        // Find all existing invitations for this year
        var existingInvitations = await _unitOfWork.GuestInvitations.FindAsync(
            i => i.InvitationNo != null && i.InvitationNo.StartsWith(prefix));

        int nextSequence = 1;
        
        if (existingInvitations.Any())
        {
            // Extract sequence numbers and find the maximum
            var sequences = existingInvitations
                .Where(i => !string.IsNullOrEmpty(i.InvitationNo) && i.InvitationNo.Length > prefix.Length)
                .Select(i =>
                {
                    try
                    {
                        var numberPart = i.InvitationNo!.Substring(prefix.Length);
                        if (int.TryParse(numberPart, out int seq))
                        {
                            return seq;
                        }
                    }
                    catch
                    {
                        // Skip invalid format
                    }
                    return 0;
                })
                .Where(seq => seq > 0)
                .ToList();

            if (sequences.Any())
            {
                nextSequence = sequences.Max() + 1;
            }
        }

        // Format as INV-YYYY-NNNN (4-digit zero-padded sequence)
        return $"{prefix}{nextSequence:D4}";
    }

    public async Task<string> GenerateInvitationNumberWithLockAsync(IUnitOfWork unitOfWork, int year, CancellationToken cancellationToken = default)
    {
        var prefix = $"INV-{year}-";
        var context = unitOfWork.GetDbContext() as VmsDbContext;
        
        if (context == null)
        {
            throw new InvalidOperationException("DbContext is not available or is not of type VmsDbContext");
        }

        int? maxSequence = null;
        
        try
        {
            // Execute raw SQL query with parameter using SqlQueryRaw
            // Use FormattableString for parameterization to prevent SQL injection
            // UPDLOCK: Locks rows for update, preventing other transactions from reading them
            // ROWLOCK: Uses row-level locking instead of page-level
            // This ensures atomic sequence generation within a transaction
            var sql = $@"
                SELECT MAX(CAST(SUBSTRING(InvitationNo, LEN({{0}}) + 1, 4) AS INT)) AS MaxSequence
                FROM GuestInvitations WITH (UPDLOCK, ROWLOCK)
                WHERE InvitationNo LIKE {{0}} + '%'";
            
            // Execute raw SQL query and get result
            var result = await context.Database
                .SqlQueryRaw<MaxSequenceResult>(sql, prefix)
                .FirstOrDefaultAsync(cancellationToken);
            
            maxSequence = result?.MaxSequence;
        }
        catch
        {
            // Fallback to LINQ query if raw SQL fails (e.g., different database provider)
            // Note: This fallback doesn't have locking, so retry logic in handler is important
            var existingInvitations = await unitOfWork.GuestInvitations.FindAsync(
                i => i.InvitationNo != null && i.InvitationNo.StartsWith(prefix));

            if (existingInvitations.Any())
            {
                var sequences = existingInvitations
                    .Where(i => !string.IsNullOrEmpty(i.InvitationNo) && i.InvitationNo.Length > prefix.Length)
                    .Select(i =>
                    {
                        try
                        {
                            var numberPart = i.InvitationNo!.Substring(prefix.Length);
                            if (int.TryParse(numberPart, out int seq))
                            {
                                return seq;
                            }
                        }
                        catch
                        {
                            // Skip invalid format
                        }
                        return 0;
                    })
                    .Where(seq => seq > 0)
                    .ToList();

                if (sequences.Any())
                {
                    maxSequence = sequences.Max();
                }
            }
        }

        int nextSequence = (maxSequence ?? 0) + 1;

        // Format as INV-YYYY-NNNN (4-digit zero-padded sequence)
        return $"{prefix}{nextSequence:D4}";
    }
}


using Microsoft.EntityFrameworkCore;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Domain.Common.Results;
using Oficina.Infrastructure.Messaging.Outbox;
using Oficina.Infrastructure.Persistence.Errors;

namespace Oficina.Infrastructure.Persistence;

public sealed class UnitOfWork(OficinaDbContext context, OutboxWriter outboxWriter) : IUnitOfWork
{
    public async Task<Result> CommitAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            outboxWriter.AppendPendingEvents(context);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException exception) when (SqlErrorTranslator.Translate(exception) is { } error)
        {
            await transaction.RollbackAsync(cancellationToken);
            context.ChangeTracker.Clear();
            return error;
        }
    }
}

using System.Text.Json;
using ArchLens.Contracts.Events;
using ArchLens.SharedKernel.Domain;
using ArchLens.Upload.Domain.Events;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Context;
using ArchLens.Upload.Infrastructure.Persistence.EFCore.Outbox;
using Microsoft.EntityFrameworkCore;

namespace ArchLens.Upload.Infrastructure.Persistence;

public sealed class UnitOfWork(UploadDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ConvertDomainEventsToOutboxMessages();
        return await context.SaveChangesAsync(cancellationToken);
    }

    private void ConvertDomainEventsToOutboxMessages()
    {
        var aggregates = context.ChangeTracker
            .Entries<AggregateRoot<Guid>>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            var domainEvents = aggregate.PopDomainEvents();
            foreach (var domainEvent in domainEvents)
            {
                var integrationEvent = MapToIntegrationEvent(domainEvent);
                if (integrationEvent is null) continue;

                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = integrationEvent.GetType().AssemblyQualifiedName!,
                    Content = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
                    CreatedAt = DateTime.UtcNow
                };

                context.OutboxMessages.Add(outboxMessage);
            }
        }
    }

    private static object? MapToIntegrationEvent(IDomainEvent domainEvent) => domainEvent switch
    {
        DiagramUploadCreatedEvent e => new DiagramUploadedEvent
        {
            DiagramId = e.DiagramId,
            FileName = e.FileName,
            FileHash = e.FileHash,
            StoragePath = e.StoragePath,
            UserId = e.UserId,
            Timestamp = DateTime.UtcNow
        },
        _ => null
    };

    public async Task ExecuteAsync(Func<CancellationToken, Task> work, CancellationToken ct = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await context.Database.BeginTransactionAsync(ct);
            try
            {
                await work(ct);
                await context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await context.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await work(ct);
                await context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return result;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }
}

using Account.Contracts.Events.External;
using Account.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.External;

/// <summary>
/// This consumer is responsible for syncing data from the database to the external system.
/// </summary>
/// <param name="dbContext"></param>
/// <param name="logger"></param>
public class SyncDataConsumer(AppDbContext dbContext, ILogger<SyncDataConsumer> logger)
    : IConsumer<SyncDataEvent>
{
    public async Task Consume(ConsumeContext<SyncDataEvent> context)
    {
        var batchSize = context.Message.BatchSize;
        var offset = context.Message.Offset;

        var users = await dbContext.AppUsers
            .AsNoTracking()
            .Include(u => u.ApiKeys)
            .OrderBy(u => u.Id)
            .Skip(offset)
            .Take(batchSize)
            .ToListAsync(context.CancellationToken);

        if (users.Count == 0)
        {
            logger.LogInformation("SyncData completed. Total users processed: {Offset}", offset);
            return;
        }

        var syncRecords = users.Select(u => new UserSyncRecord
        {
            Email = u.Email,
            ApiKey = u.ApiKeys
                .Where(k => k.IsAuthorize && k.ExpiredAt > DateTime.UtcNow)
                .Select(k => k.ApiKeyValue)
                .FirstOrDefault() ?? "",
            ReferralId = u.ReferrerId,
            IsActive = u.ApiKeys.Any(k => k.IsAuthorize && k.ExpiredAt > DateTime.UtcNow),
            EmailConfirmed = u.EmailConfirmed
        }).ToList();

        await context.Publish(new UserSyncBatchEvent { Users = syncRecords });

        if (users.Count == batchSize)
        {
            await context.Publish(new SyncDataEvent
            {
                BatchSize = batchSize,
                Offset = offset + batchSize
            });
        }

        logger.LogInformation(
            "Synced batch at offset {Offset}: {Count} users. HasMore: {HasMore}",
            offset, users.Count, users.Count == batchSize);
    }
}
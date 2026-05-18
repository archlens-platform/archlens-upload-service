using ArchLens.Upload.Domain.Interfaces.AnalysisProcessInterfaces;
using ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;
using ArchLens.Upload.Domain.Interfaces.StorageInterfaces;
using ArchLens.Upload.Domain.ValueObjects.Diagrams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArchLens.Upload.Infrastructure.BackgroundServices;

public sealed class DataRetentionProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<DataRetentionProcessor> logger) : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);
    private const int RetentionDays = 90;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunRetentionPolicyAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error running data retention policy");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }
    }

    private async Task RunRetentionPolicyAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var uploadRepo = scope.ServiceProvider.GetRequiredService<IDiagramUploadRepository>();
        var analysisRepo = scope.ServiceProvider.GetRequiredService<IAnalysisProcessRepository>();
        var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

        var skip = 0;
        const int take = 100;
        var totalDeleted = 0;

        while (true)
        {
            var (items, _) = await uploadRepo.GetPagedAsync(skip, take, cancellationToken: ct);
            var expired = items
                .Where(u => u.CreatedAt < cutoff &&
                            (u.Status == DiagramStatus.Analyzed || u.Status == DiagramStatus.Error))
                .ToList();

            if (expired.Count == 0 && items.Count < take)
                break;

            foreach (var upload in expired)
            {
                try
                {
                    await storage.DeleteAsync(upload.StoragePath, ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete file {StoragePath} during retention", upload.StoragePath);
                }

                var analysis = await analysisRepo.GetByDiagramIdAsync(upload.Id, ct);
                if (analysis is not null)
                    await analysisRepo.DeleteAsync(analysis, ct);

                await uploadRepo.DeleteAsync(upload, ct);
                totalDeleted++;
            }

            if (items.Count < take)
                break;

            skip += take;
        }

        if (totalDeleted > 0)
            logger.LogInformation("Data retention: deleted {Count} expired uploads (>{Days} days)", totalDeleted, RetentionDays);
    }
}

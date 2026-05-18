using ArchLens.Contracts.Events;
using ArchLens.SharedKernel.Domain;
using ArchLens.Upload.Domain.Interfaces.AnalysisProcessInterfaces;
using ArchLens.Upload.Domain.Interfaces.DiagramUploadInterfaces;
using ArchLens.Upload.Domain.Interfaces.StorageInterfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ArchLens.Upload.Infrastructure.Consumers;

public sealed class UserAccountDeletedConsumer(
    IDiagramUploadRepository uploadRepository,
    IAnalysisProcessRepository analysisProcessRepository,
    IFileStorageService fileStorageService,
    IUnitOfWork unitOfWork,
    ILogger<UserAccountDeletedConsumer> logger)
    : IConsumer<UserAccountDeletedEvent>
{
    public async Task Consume(ConsumeContext<UserAccountDeletedEvent> context)
    {
        var userId = context.Message.UserId.ToString();
        logger.LogInformation("Processing cascade delete for user {UserId}", context.Message.UserId);

        var uploads = await uploadRepository.GetAllByUserIdAsync(userId, context.CancellationToken);

        if (uploads.Count == 0)
        {
            logger.LogInformation("No uploads found for user {UserId}", context.Message.UserId);
            return;
        }

        await unitOfWork.ExecuteAsync(async ct =>
        {
            foreach (var upload in uploads)
            {
                try
                {
                    await fileStorageService.DeleteAsync(upload.StoragePath, ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete file {StoragePath} for user {UserId}", upload.StoragePath, context.Message.UserId);
                }

                var analysisProcess = await analysisProcessRepository.GetByDiagramIdAsync(upload.Id, ct);
                if (analysisProcess is not null)
                    await analysisProcessRepository.DeleteAsync(analysisProcess, ct);

                await uploadRepository.DeleteAsync(upload, ct);
            }
        }, context.CancellationToken);

        logger.LogInformation("Deleted {Count} uploads for user {UserId}", uploads.Count, context.Message.UserId);
    }
}

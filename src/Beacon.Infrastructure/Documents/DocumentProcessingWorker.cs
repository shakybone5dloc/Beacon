using Beacon.Application.Documents;
using Beacon.Domain.Documents;
using Beacon.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beacon.Infrastructure.Documents;

internal sealed class DocumentProcessingWorker(
    ChannelDocumentQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<DocumentProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Document worker started");

        await RequeueUnfinishedAsync(stoppingToken);

        await foreach (var documentId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dps = scope.ServiceProvider.GetRequiredService<DocumentProcessingService>();

                await dps.ProcessAsync(documentId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unexpected error processing document {DocumentId}", documentId);
            }
        }
    }

    private async Task RequeueUnfinishedAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BeaconDbContext>();

            var ids = await db.Documents
                .Where(d => d.Status == DocumentStatus.Pending || d.Status == DocumentStatus.Processing)
                .Select(d => d.Id)
                .ToListAsync(ct);

            foreach (var id in ids)
                await queue.EnqueueAsync(id, ct);

            if (ids.Count > 0)
                logger.LogInformation("Re-queued {Count} unfinished documents", ids.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not re-queue unfinished documents at startup");
        }
    }
}
using Beacon.Contracts.Documents;
using Beacon.Domain.Documents;

namespace Beacon.Application.Documents;

public static class DocumentMappings
{
    public static DocumentResponse ToResponse(this Document d, int chunkCount) => new(
        d.Id, d.FileName, d.Kind.ToString(), d.Status.ToString(),
        d.JobApplicationId, chunkCount, d.Error, d.CreatedAt, d.UpdatedAt);
}
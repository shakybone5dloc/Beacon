using Beacon.Contracts.Applications;
using Beacon.Domain.Applications;

namespace Beacon.Application.Applications;

public static class ApplicationMappings
{
    public static ApplicationResponse ToResponse(this JobApplication a) => new(
        a.Id,
        a.Company,
        a.Role,
        a.JobUrl,
        a.Notes,
        a.Status.ToString(),
        a.CreatedAt,
        a.UpdatedAt,
        a.Version);
}
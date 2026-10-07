using Beacon.Contracts.Applications;
using Beacon.Contracts.Ask;
using Beacon.Contracts.Documents;
using System.Runtime.CompilerServices;

namespace Beacon.Web.Services;

public interface IBeaconApiClient
{
    Task<IReadOnlyList<ApplicationResponse>> GetApplicationsAsync(CancellationToken ct = default);
    Task<ApplicationResponse?> GetApplicationAsync(Guid id, CancellationToken ct = default);
    Task<MoveResult> ChangeStatusAsync(Guid id, string status, uint version, CancellationToken ct = default);
    Task<CreateResult> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentResponse>> GetDocumentsAsync(Guid id, CancellationToken ct = default);
    Task<DocumentUploadResult> UploadDocumentAsync(Stream content, string fileName, string kind, Guid applicationId, CancellationToken ct = default);
    IAsyncEnumerable<AskEvent> AskAsync(string question, Guid? applicationId, CancellationToken ct = default);
}
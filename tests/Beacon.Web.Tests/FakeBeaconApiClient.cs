using Beacon.Contracts.Applications;
using Beacon.Contracts.Ask;
using Beacon.Contracts.Documents;
using Beacon.Web.Services;

namespace Beacon.Web.Tests;

public sealed class FakeBeaconApiClient : IBeaconApiClient
{
    public List<ApplicationResponse> Applications { get; set; } = [];
    public MoveResult NextMoveResult { get; set; } = new(MoveOutcome.Moved);
    public int GetApplicationsCalls { get; private set; }

    public Task<IReadOnlyList<ApplicationResponse>> GetApplicationsAsync(CancellationToken ct = default)
    {
        GetApplicationsCalls++;
        return Task.FromResult<IReadOnlyList<ApplicationResponse>>(Applications.ToList());
    }

    public Task<MoveResult> ChangeStatusAsync(Guid id, string status, uint version, CancellationToken ct = default)
        => Task.FromResult(NextMoveResult);

    public Task<ApplicationResponse?> GetApplicationAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<CreateResult> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<IReadOnlyList<DocumentResponse>> GetDocumentsAsync(Guid applicationId, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<DocumentUploadResult> UploadDocumentAsync(Stream content, string fileName, string kind, Guid applicationId, CancellationToken ct = default) => throw new NotImplementedException();
    public IAsyncEnumerable<AskEvent> AskAsync(string question, Guid? applicationId, CancellationToken ct = default) => throw new NotImplementedException();
}
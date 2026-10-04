using Beacon.Contracts.Documents;

namespace Beacon.Application.Documents;

public sealed record UploadResult(DocumentResponse? Document, string? Error)
{
    public static UploadResult Ok(DocumentResponse document) => new(document, null);
    public static UploadResult Fail(string error) => new(null, error);
}
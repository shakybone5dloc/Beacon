using Beacon.Contracts.Documents;

namespace Beacon.Web.Services;

public sealed record DocumentUploadResult(DocumentResponse? Document, string? Error);
using Beacon.Contracts.Applications;

namespace Beacon.Web.Services;

public enum MoveOutcome { Moved, Conflict, NotFound }

public sealed record MoveResult(MoveOutcome Outcome, ApplicationResponse? Application = null, string? Message = null);
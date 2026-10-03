using Beacon.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Beacon.Api.Errors;

// Turns "business rule broken" and "someone else changed it" into 409 Conflict
internal sealed class ConflictExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        var detail = exception switch
        {
            DomainException e => e.Message,
            DbUpdateConcurrencyException => "This application was changed by someone else. Reload it and try again.",
            _ => null
        };

        if (detail is null) return false;  // not ours; let the default handler make it a 500

        http.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = { Title = "Conflict", Detail = detail, Status = StatusCodes.Status409Conflict }
        });
    }
}
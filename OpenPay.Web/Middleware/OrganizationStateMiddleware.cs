using OpenPay.Application.Common;

namespace OpenPay.Web.Middleware;

public class OrganizationStateMiddleware
{
    private readonly RequestDelegate _next;

    public OrganizationStateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OrganizationInactiveException)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.Redirect("/OrganizationDisabled");
                return;
            }

            throw;
        }
    }
}
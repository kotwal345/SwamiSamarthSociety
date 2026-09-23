using SwamiSamarthSociety.Web.Identity;

namespace SwamiSamarthSociety.Web.Middleware
{
    // Any signed-in user whose MustChangePassword claim is set (baked in at login, cleared and
    // refreshed by ChangePasswordModel on success -- see AppUserClaimsPrincipalFactory) is
    // redirected to the change-password page on every request until they change it, even across
    // a "remember me" session that skips the login page entirely.
    public class RequirePasswordChangeMiddleware
    {
        private const string ChangePasswordPath = "/Identity/Account/ChangePassword";
        private const string LogoutPath = "/Identity/Account/Logout";

        private readonly RequestDelegate _next;

        public RequirePasswordChangeMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path;
            var isExempt = path.StartsWithSegments(ChangePasswordPath)
                || path.StartsWithSegments(LogoutPath)
                || path.StartsWithSegments("/lib")
                || path.StartsWithSegments("/css")
                || path.StartsWithSegments("/js");

            if (!isExempt
                && context.User.Identity?.IsAuthenticated == true
                && context.User.HasClaim(AppUserClaimsPrincipalFactory.MustChangePasswordClaimType, "true"))
            {
                context.Response.Redirect(ChangePasswordPath + "?forced=true");
                return;
            }

            await _next(context);
        }
    }
}

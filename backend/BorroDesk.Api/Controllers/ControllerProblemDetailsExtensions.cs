using Microsoft.AspNetCore.Mvc;

namespace BorroDesk.Api.Controllers;

internal static class ControllerProblemDetailsExtensions
{
    public static ObjectResult ApiProblem(
        this ControllerBase controller,
        int statusCode,
        string title,
        string? detail = null)
    {
        return controller.Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            type: $"https://httpstatuses.com/{statusCode}",
            instance: controller.HttpContext.Request.Path);
    }
}

namespace Resumaire.Api.Infrastructure.Api;

public sealed record ApiResponse<T>(T Data, string? Message = null);

public static class ApiResponses
{
    public static IResult Ok<T>(T data, string? message = null) =>
        Results.Ok(new ApiResponse<T>(data, message));

    public static IResult Created<T>(string location, T data, string? message = null) =>
        Results.Created(location, new ApiResponse<T>(data, message));
}

using Resumaire.Api.Configuration;
using Resumaire.Api.Endpoints;
using Microsoft.AspNetCore.WebUtilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddResumaireOptions(builder.Configuration)
    .AddResumaireDatabase(builder.Configuration)
    .AddResumaireIdentity()
    .AddResumaireApiServices(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();

    await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
    {
        HttpContext = httpContext,
        ProblemDetails =
        {
            Status = httpContext.Response.StatusCode,
            Title = ReasonPhrases.GetReasonPhrase(httpContext.Response.StatusCode)
        }
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors(CorsOptions.SectionName);

app.UseAuthentication();
app.UseAuthorization();

app.MapResumaireEndpoints();

app.Run();

public partial class Program;

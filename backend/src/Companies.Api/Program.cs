using System.Text.Json.Serialization;
using Companies.Api.Http;
using Companies.Application;
using Companies.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure();

builder.Services
    .AddControllers(options =>
        // Validation errors are keyed by JSON property name ("name"), matching the domain error targets.
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = InvalidRequestResponseFactory.Create);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "Companies API";
    document.Info.Description =
        "Register companies (name + website, validated for relevance), then list, filter, search and fetch them. "
        + "Errors are RFC 9457 problem details with a machine-readable `code`.";
    return Task.CompletedTask;
}));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .WithMethods("GET", "POST")
    .WithHeaders("Content-Type", "Accept")
    .WithExposedHeaders("Location")));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // API description (/openapi/v1.json) and an interactive reference over it (/scalar/v1).
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("Companies API"));
}
else
{
    // In Development both :5000 and :7000 serve the API (spec + Angular dev proxy).
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors();
app.MapControllers();

await app.RunAsync();

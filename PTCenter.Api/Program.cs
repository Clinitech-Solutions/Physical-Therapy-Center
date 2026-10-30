using PTCenter.Api;
using PTCenter.Application;
using PTCenter.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddApiExtensions(builder.Configuration)
    .AddApplicationExtensions(builder.Configuration)
    .AddInfrastructureExtensions(builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new PTCenter.Api.Converters.UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        );
    });
builder.Services.AddOpenApi();

var app = builder.Build();


// OpenAPI + Scalar
app.MapOpenApi();

app.MapScalarApiReference(options =>
{
    options
        .WithTitle("PT Center API");
});


app.UseHttpsRedirection();

// CORS
app.UseCors("MyPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.OpenApi;
using Oficina.Api.ExceptionHandling;

namespace Oficina.Api.Extensions;

public static class PresentationExtensions
{
    public const string BearerSchemeName = "Bearer";

    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<Oficina.Application.Abstractions.Realtime.IRealtimePublisher, Oficina.Api.Realtime.SignalRRealtimePublisher>();
        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);
        services.Configure<MvcOptions>(options =>
        {
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            options.OutputFormatters.RemoveType<StringOutputFormatter>();
            options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>().Single().SupportedMediaTypes.Remove("text/json");
            options.Filters.Add(new ProducesResponseTypeAttribute<ProblemDetails>(StatusCodes.Status401Unauthorized, ProblemResponses.ContentType));
            options.Filters.Add(new ProducesResponseTypeAttribute<ProblemDetails>(StatusCodes.Status500InternalServerError, ProblemResponses.ContentType));
        });
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Oficina API", Version = "v1" });
            options.AddSecurityDefinition(BearerSchemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(BearerSchemeName, document)] = new List<string>(),
            });
        });

        return services;
    }

    public static ILoggingBuilder AddStructuredLogging(this ILoggingBuilder logging)
    {
        logging.ClearProviders();
        logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.UseUtcTimestamp = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
            options.JsonWriterOptions = new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        });

        return logging;
    }
}

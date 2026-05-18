using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace ArchLens.Upload.Api.Configurations;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder, string serviceName = "upload-service")
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        builder.Host.UseSerilog((context, configuration) =>
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("Application", "archlens")
                .WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = context.Configuration["Otlp:Endpoint"]
                        ?? "http://otel-collector:4317";
                    options.Protocol = OtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object>
                    {
                        ["service.name"] = serviceName
                    };
                }));

        return builder;
    }
}

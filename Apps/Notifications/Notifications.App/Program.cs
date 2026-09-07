using MagicCSharp.App;

namespace Acme.Notifications.App;

/// <summary>
///     Named rather than top-level so integration tests can reference it as
///     <c>WebApplicationFactory&lt;NotificationsProgram&gt;</c>.
/// </summary>
public class NotificationsProgram
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Use cases, IClock, Snowflake IDs, request IDs, in-process events, scheduling defaults and
        // problem-details error handling. Pass MagicAppOptions to change any of it; swap in
        // RegisterMagicKafkaEvents before this line and the in-process dispatcher steps aside.
        builder.AddMagicApp();

        builder.Services.AddOpenApi();


        var app = builder.Build();

        // Request IDs, error handling and controllers, in the order they need — and a preflight that
        // resolves every registration now rather than on the first request that needs it.
        app.UseMagicApp(builder);

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.Run();
    }
}

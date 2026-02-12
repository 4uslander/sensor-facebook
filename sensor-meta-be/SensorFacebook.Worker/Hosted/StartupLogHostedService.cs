public sealed class StartupLogHostedService : IHostedService
{
    private readonly string? _host;
    private readonly string? _vhost;
    private readonly string? _user;
    private readonly ILogger<StartupLogHostedService> _log;

    public StartupLogHostedService(string? host, string? vhost, string? user)
    {
        _host = host; _vhost = vhost; _user = user;
        _log = LoggerFactory.Create(b => b.AddSimpleConsole()).CreateLogger<StartupLogHostedService>();
    }

    public Task StartAsync(CancellationToken ct)
    {
        _log.LogInformation("Worker Rabbit config host={Host} vhost={VHost} user={User}", _host, _vhost, _user);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

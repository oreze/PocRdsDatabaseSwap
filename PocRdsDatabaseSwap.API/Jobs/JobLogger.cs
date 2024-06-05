using Microsoft.EntityFrameworkCore;
using PocRdsDatabaseSwap.API.Data;
using PocRdsDatabaseSwap.API.Models;

namespace PocRdsDatabaseSwap.API.Jobs;

public class JobLogger(ILogger<JobLogger> logger, IServiceProvider serviceProvider) : IHostedService, IDisposable
{
    private readonly ILogger<JobLogger> _logger = logger ?? throw new ArgumentException(nameof(logger));
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentException(nameof(serviceProvider));
    
    private Timer _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job logger is starting");

        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

        return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
        var guid = Guid.NewGuid();
        _logger.LogInformation("Logging GUID: {Guid}", guid);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var log = new LogEntity()
        {
            Body = dbContext.Database.GetDbConnection().Database,
            LogDate = DateTime.UtcNow
        };

        dbContext.Logs.Add(log);
        dbContext.SaveChanges();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job logger is stopping");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace Debugger
{
    public class TrafficGenerator : IHostedService, IDisposable
    {
        private ILogger<TrafficGenerator> _logger;
        private Timer _timer;
        private int _executionCount = 0;

        public TrafficGenerator(ILogger<TrafficGenerator> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TrafficGenerator Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            _executionCount++;
            _logger.LogInformation($"TrafficGenerator Service is working. Count: {_executionCount}");
            // Generate some ASP traffic, which will generate some logs
            try
            {
                var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "Ditto");
                var result = http.GetAsync("http://localhost:5000/weatherforecast").Result;
            }
            catch (Exception e)
            {
                _logger.LogError("Caught Exception getting weather forecast", e);
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TrafficGenerator Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
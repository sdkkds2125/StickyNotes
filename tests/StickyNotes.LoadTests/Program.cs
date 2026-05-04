using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace StickyNotes.LoadTests;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting StickyNotes Load Test...");
        
        string targetUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
        int concurrentUsers = 50;
        int requestsPerUser = 20;
        
        Console.WriteLine($"Target URL: {targetUrl}");
        Console.WriteLine($"Concurrent Users: {concurrentUsers}");
        Console.WriteLine($"Requests per User: {requestsPerUser}");
        Console.WriteLine($"Total Requests: {concurrentUsers * requestsPerUser}");
        Console.WriteLine("Press any key to begin...");
        
        // Wait 2 seconds to allow user to read output instead of blocking on ReadKey in non-interactive terminal
        await Task.Delay(2000);
        
        var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10);
        
        var stopwatch = Stopwatch.StartNew();
        
        int successCount = 0;
        int errorCount = 0;
        
        var tasks = new Task[concurrentUsers];
        for (int i = 0; i < concurrentUsers; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                for (int j = 0; j < requestsPerUser; j++)
                {
                    try
                    {
                        var response = await httpClient.GetAsync(targetUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            Interlocked.Increment(ref successCount);
                        }
                        else
                        {
                            Interlocked.Increment(ref errorCount);
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref errorCount);
                    }
                }
            });
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        Console.WriteLine("\nLoad Test Completed!");
        Console.WriteLine($"Time elapsed: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"Successful requests: {successCount}");
        Console.WriteLine($"Failed requests: {errorCount}");
        
        double requestsPerSecond = (successCount + errorCount) / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Requests per second: {requestsPerSecond:F2}");
    }
}

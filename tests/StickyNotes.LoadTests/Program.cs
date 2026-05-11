using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace StickyNotes.LoadTests;

class Program
{
    // =============================================
    // Configuration — adjust these to control test intensity
    // =============================================
    static int ConcurrentUsers = 50;
    static int RequestsPerUser = 20;

    static async Task Main(string[] args)
    {
        // The target URL can be your live site or localhost
        // Usage: dotnet run -- https://your-live-site.com username password
        //   or:  dotnet run   (defaults to localhost:5000 with no auth)
        string baseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5000";
        string? username = args.Length > 1 ? args[1] : null;
        string? password = args.Length > 2 ? args[2] : null;

        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine("║       StickyNotes Load Test Suite        ║");
        Console.WriteLine("╚══════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"  Target:           {baseUrl}");
        Console.WriteLine($"  Concurrent Users: {ConcurrentUsers}");
        Console.WriteLine($"  Requests/User:    {RequestsPerUser}");
        Console.WriteLine($"  Total Requests:   {ConcurrentUsers * RequestsPerUser}");
        Console.WriteLine($"  Auth:             {(username != null ? "Yes" : "No (unauthenticated)")}");
        Console.WriteLine();

        // Create a shared cookie container so all requests share the auth session
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            AllowAutoRedirect = true,
            UseCookies = true
        };
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };

        // Step 1: Authenticate if credentials were provided
        if (username != null && password != null)
        {
            Console.Write("  Logging in... ");
            bool loggedIn = await LoginAsync(httpClient, username, password);
            if (!loggedIn)
            {
                Console.WriteLine("FAILED ✗");
                Console.WriteLine("  Could not authenticate. Check your username/password.");
                return;
            }
            Console.WriteLine("OK ✓");
        }

        // Step 2: Define the endpoints to test
        var endpoints = new List<(string Name, string Path)>
        {
            ("Dashboard (GET /)",        "/"),
            ("New Note Form (GET)",      "/NoteEditor"),
            ("Login Page (GET)",         "/Account/Login"),
        };

        Console.WriteLine();
        Console.WriteLine("  Starting load test in 2 seconds...");
        await Task.Delay(2000);

        // Step 3: Run the load test against each endpoint
        foreach (var (name, path) in endpoints)
        {
            await RunLoadTest(httpClient, name, path);
        }

        Console.WriteLine();
        Console.WriteLine("  All tests completed!");
    }

    // =============================================
    // Authenticate against the live site
    // =============================================
    static async Task<bool> LoginAsync(HttpClient client, string username, string password)
    {
        try
        {
            // First, GET the login page to extract the anti-forgery token
            var loginPage = await client.GetAsync("/Account/Login");
            var html = await loginPage.Content.ReadAsStringAsync();

            var tokenMatch = Regex.Match(html, @"name=""__RequestVerificationToken""\s+type=""hidden""\s+value=""([^""]+)""");
            if (!tokenMatch.Success)
            {
                // Try alternate attribute ordering
                tokenMatch = Regex.Match(html, @"__RequestVerificationToken""\s.*?value=""([^""]+)""");
            }

            string? token = tokenMatch.Success ? tokenMatch.Groups[1].Value : null;

            // POST the login form
            var formData = new Dictionary<string, string>
            {
                { "Username", username },
                { "Password", password },
                { "RememberMe", "true" }
            };
            if (token != null)
            {
                formData["__RequestVerificationToken"] = token;
            }

            var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(formData));

            // A successful login redirects to / (the dashboard)
            var finalUrl = response.RequestMessage?.RequestUri?.AbsolutePath ?? "";
            return response.IsSuccessStatusCode && (finalUrl == "/" || finalUrl == "/Index");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n  Login error: {ex.Message}");
            return false;
        }
    }

    // =============================================
    // Run concurrent load test against one endpoint
    // =============================================
    static async Task RunLoadTest(HttpClient client, string testName, string path)
    {
        int successCount = 0;
        int errorCount = 0;
        long totalLatencyMs = 0;
        long minLatencyMs = long.MaxValue;
        long maxLatencyMs = 0;

        Console.WriteLine($"  ── {testName} ({path}) ──");

        var stopwatch = Stopwatch.StartNew();

        var tasks = new Task[ConcurrentUsers];
        for (int i = 0; i < ConcurrentUsers; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                for (int j = 0; j < RequestsPerUser; j++)
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        var response = await client.GetAsync(path);
                        sw.Stop();
                        long ms = sw.ElapsedMilliseconds;

                        Interlocked.Add(ref totalLatencyMs, ms);
                        InterlockedMin(ref minLatencyMs, ms);
                        InterlockedMax(ref maxLatencyMs, ms);

                        if (response.IsSuccessStatusCode)
                            Interlocked.Increment(ref successCount);
                        else
                            Interlocked.Increment(ref errorCount);
                    }
                    catch
                    {
                        sw.Stop();
                        Interlocked.Increment(ref errorCount);
                    }
                }
            });
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        int total = successCount + errorCount;
        double rps = total / stopwatch.Elapsed.TotalSeconds;
        double avgLatency = total > 0 ? totalLatencyMs / (double)total : 0;

        Console.WriteLine($"     Requests:  {total} ({successCount} ok, {errorCount} failed)");
        Console.WriteLine($"     Duration:  {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"     Req/sec:   {rps:F1}");
        Console.WriteLine($"     Latency:   avg {avgLatency:F0}ms  |  min {minLatencyMs}ms  |  max {maxLatencyMs}ms");
        Console.WriteLine();
    }

    // Thread-safe minimum tracking
    static void InterlockedMin(ref long target, long value)
    {
        long current;
        do { current = Interlocked.Read(ref target); }
        while (value < current && Interlocked.CompareExchange(ref target, value, current) != current);
    }

    // Thread-safe maximum tracking
    static void InterlockedMax(ref long target, long value)
    {
        long current;
        do { current = Interlocked.Read(ref target); }
        while (value > current && Interlocked.CompareExchange(ref target, value, current) != current);
    }
}

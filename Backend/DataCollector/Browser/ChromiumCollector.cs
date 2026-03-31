using System.Net;
using System.Text.Json;
using Backend.Models;

namespace Backend.DataCollector.Browser;

public class ChromiumCollector : IBrowserDataCollector, IDisposable
{
    private BrowserRecord? _lastRecord;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _readTask;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly SemaphoreSlim _startupSemaphore = new(0, 1);
    private bool _isReady = false;

    public ChromiumCollector()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _readTask = Task.Run(() => StartHttpListenerAsync(_cancellationTokenSource.Token));

        // Wait for listener to start (with timeout)
        _startupSemaphore.Wait(TimeSpan.FromSeconds(5));
    }

    private async Task StartHttpListenerAsync(CancellationToken cancellationToken)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:8091/"); // Different port from Firefox

        try
        {
            listener.Start();
            _isReady = true;
            Console.WriteLine("Chromium collector: HTTP listener started on port 8091");
            _startupSemaphore.Release();

            await ListenForRequestsAsync(listener, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Chromium collector: HTTP listener failed to start: {ex}");
            _startupSemaphore.Release(); // Release even on error so constructor doesn't hang
        }
        finally
        {
            listener.Stop();
            _isReady = false;
            Console.WriteLine("Chromium collector: HTTP listener stopped");
        }
    }

    private async Task ListenForRequestsAsync(HttpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await listener.GetContextAsync();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Add CORS headers
                        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                        context.Response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
                        context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                        if (context.Request.HttpMethod == "OPTIONS")
                        {
                            // Handle preflight request
                            context.Response.StatusCode = 200;
                            context.Response.Close();
                            return;
                        }

                        if (context.Request.HttpMethod == "POST" && context.Request.Url?.AbsolutePath == "/tab")
                        {
                            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                            string json = await reader.ReadToEndAsync();

                            Console.WriteLine($"Chromium collector: Received data: {json}");

                            var data = JsonSerializer.Deserialize<JsonElement>(json);
                            if (data.TryGetProperty("url", out var urlProp))
                            {
                                var url = urlProp.GetString();
                                var record = HandleRequest(url);

                                if (record != null)
                                {
                                    await _semaphore.WaitAsync(cancellationToken);
                                    try
                                    {
                                        _lastRecord = record;
                                        Console.WriteLine($"Chromium collector: Updated last record to: {url}");
                                    }
                                    finally
                                    {
                                        _semaphore.Release();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"Chromium collector: Invalid URL received: {url}");
                                }
                            }

                            context.Response.StatusCode = 200;
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                        }

                        context.Response.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Chromium collector: Request handler failed: {ex}");
                        try
                        {
                            context.Response.StatusCode = 500;
                            context.Response.Close();
                        }
                        catch { }
                    }
                }, cancellationToken);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Listener error: {ex}");
            }
        }
    }

    public BrowserRecord? QueryTabs()
    {
        _semaphore.Wait();
        try
        {
            return _lastRecord;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void ClearState()
    {
        _semaphore.Wait();
        try
        {
            _lastRecord = null;
            Console.WriteLine("Chromium collector: State cleared");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    static BrowserRecord? HandleRequest(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        try
        {
            return new BrowserRecord
            {
                Url = url,
            };
        }
        catch (UriFormatException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _readTask.Wait(TimeSpan.FromSeconds(5));
        _cancellationTokenSource.Dispose();
        _semaphore.Dispose();
        _startupSemaphore.Dispose();
    }
}
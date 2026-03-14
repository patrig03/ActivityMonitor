using System.Net;
using System.Text;
using System.Text.Json;
using Backend.Models;

namespace Backend.DataCollector.Browser;

public class FirefoxCollector : IBrowserDataCollector, IDisposable
{
    private BrowserRecord? _lastRecord;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _readTask;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public FirefoxCollector()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _readTask = Task.Run(() => StartHttpListenerAsync(_cancellationTokenSource.Token));
    }

    private async Task StartHttpListenerAsync(CancellationToken cancellationToken)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:8090/");

        try
        {
            listener.Start();
            Console.WriteLine("HTTP listener started on http://127.0.0.1:8090/");

            await ListenForRequestsAsync(listener, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"HTTP listener failed to start: {ex}");
        }
        finally
        {
            listener.Stop();
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
                        if (context.Request.HttpMethod == "POST" && context.Request.Url?.AbsolutePath == "/tab")
                        {
                            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                            string json = await reader.ReadToEndAsync();

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
                                    }
                                    finally
                                    {
                                        _semaphore.Release();
                                    }
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
                        Console.Error.WriteLine($"Request handler failed: {ex}");
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
    }
}
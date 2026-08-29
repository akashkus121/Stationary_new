using System.Collections.Concurrent;
using System.Text.Json;

namespace Stationary.Services
{
    public interface IEventStreamService
    {
        Task SubscribeAsync(HttpContext context, CancellationToken cancellationToken);
        void BroadcastEvent(string eventType, object data);
    }

    public class EventStreamService : IEventStreamService
    {
        private static readonly ConcurrentBag<StreamWriter> _subscribers = new();
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public async Task SubscribeAsync(HttpContext context, CancellationToken cancellationToken)
        {
            context.Response.Headers.Append("Content-Type", "text/event-stream");
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("Connection", "keep-alive");

            var writer = new StreamWriter(context.Response.Body);
            _subscribers.Add(writer);

            // Send initial ping event
            try
            {
                await writer.WriteAsync("event: connected\ndata: {\"status\":\"SSE Connected\"}\n\n");
                await writer.FlushAsync();
            }
            catch { }

            try
            {
                // Keep the connection open until canceled
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(5000, cancellationToken);
                    // Periodic ping to keep stream alive
                    await writer.WriteAsync(":ping\n\n");
                    await writer.FlushAsync(cancellationToken);
                }
            }
            catch
            {
                // Client disconnected
            }
        }

        public void BroadcastEvent(string eventType, object data)
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var payload = $"event: {eventType}\ndata: {json}\n\n";

            foreach (var writer in _subscribers.ToArray())
            {
                try
                {
                    writer.Write(payload);
                    writer.Flush();
                }
                catch
                {
                    // Dead connection removal
                }
            }
        }
    }
}

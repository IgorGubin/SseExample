using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

using NLog;

using Server.Model;
using Server.Processing;

namespace Server.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("[controller]")]
    public class ApiController : ControllerBase
    {
        private static readonly NLog.ILogger Logger = LogManager.GetCurrentClassLogger();

        [HttpGet]
        public string TestAlive()
        {
            return "Ready";
        }

        [HttpGet("states")]
        public async Task StatesStream(CancellationToken cancellation)
        {
            async Task WriteEvent<T>(HttpResponse response, string? id, T data)
            {
                var eventName = typeof(T).Name;

                await response.WriteAsync($"id: {id}\n");
                await response.WriteAsync($"event: {eventName}\n");
                await response.WriteAsync($"data: ");
                await JsonSerializer.SerializeAsync(response.Body, data);
                await response.WriteAsync($"\n\n");
                await response.Body.FlushAsync();
            }

            Response.Headers.Append(HeaderNames.ContentType, "text/event-stream");
            while (!cancellation.IsCancellationRequested)
            {
                var fileCardArray = Conveyor.TotalFileCardCatalog.Values.Where(fc => fc?.StateChanges?.Any() ?? false).ToArray();

                foreach (var fc in fileCardArray)
                {
                    while (fc?.StateChanges.TryDequeue(out var historyState) ?? false)
                    {
                        await WriteEvent(this.Response, fc.SessionId, new StateInfo(fc.FileId, historyState));
                    }
                }
            }
        }
    }
}

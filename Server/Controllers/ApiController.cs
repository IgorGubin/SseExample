using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

using NLog;
using Server.Enums;
using Server.Model;
using Server.Processing;
using Server.Utilities;

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

        [HttpGet("upload/{id}")]
        public int Upload(string id)
        {
            var filesCount = 0;
            var sessionData = _cfg.SessionDataList.FirstOrDefault(sd => sd.SessionId == id);
            // Designation of the upload files result
            foreach (var fid in sessionData.Files)
            {
                filesCount++;
                var fc = new FileCard(sessionData.SessionId, fid, FileCardStateEnum.New);
                if (Conveyor.TotalFileCardCatalog.TryGetValue(fid, out var fileCard))
                {
                    fileCard.State = FileCardStateEnum.New;
                }
                else
                {
                    Conveyor.TotalFileCardCatalog.TryAdd(fid, fc);
                }
            }
            return filesCount;
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

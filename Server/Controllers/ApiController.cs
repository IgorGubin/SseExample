using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

using NLog;

using Server.Enums;
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

        /// <summary>
        /// Simulate files upload
        /// </summary>
        /// <param name="data">Object of type SessionData</param>
        /// <returns>Received quantity of files</returns>
        [HttpPost("upload")]
        public async Task<int> Upload([FromBody] SessionData data)
        {
            int successCount = 0;
            if (!data.Files.Any())
                throw new ApplicationException("No files received.");

            for (var i = 0; i < data.Files.Count; i++)
            {
                var fileId = data.Files[i];
                try
                {
                    var fc = new FileCard(data.SessionId, fileId, FileCardStateEnum.New);
                    Conveyor.TotalFileCardCatalog.TryAdd(fileId, fc);
                    Conveyor.ConveyorItemSingleton.In.Add(fc);
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Error by upload file {fileId} in session {data.SessionId}.");
                }
            }

            return successCount;
        }

        /// <summary>
        /// Simulate file download
        /// </summary>
        /// <param name="id">File id.</param>
        /// <returns></returns>
        [HttpGet("download/{id}")]
        [ProducesResponseType<StateInfo>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Download(string id)
        {
            Logger.Info($"Download file {id} / [rest files: {Conveyor.TotalFileCardCatalog.Count}]");
            StateInfo? res = null;
            if (Conveyor.TotalFileCardCatalog.TryGetValue(id, out var fileCard))
            {
                res = fileCard?.StateInfo;
            }
            
            return res == null ? NotFound() : Ok(res);
        }

        /// <summary>
        /// Simulate file delete
        /// </summary>
        /// <param name="id">File id.</param>
        /// <returns></returns>
        [HttpDelete("delete/{id}")]
        [ProducesResponseType<StateInfo>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Delete(string id)
        {
            StateInfo? res = null;
            if (Conveyor.TotalFileCardCatalog.TryRemove(id, out var fileCard))
            {
                res = fileCard?.StateInfo;
            }
            Logger.Info($"Deleted file {id} / [rest files: {Conveyor.TotalFileCardCatalog.Count}]");
            return res == null ? NotFound() : Ok(res);
        }


        /// <summary>
        /// SSE point
        /// </summary>
        /// <param name="id">SessionId</param>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        [HttpGet("states/{id}")]
        public async Task StatesStream(string id, CancellationToken cancellation)
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
            { // todo: it is necessary to do refactoring to unload the processor.
                var sessionDataArray = Conveyor.TotalFileCardCatalog.Values.Where(fc => (fc.SessionId.Equals(id, StringComparison.OrdinalIgnoreCase)) && (fc?.StateChanges?.Any() ?? false)).ToArray();
                foreach (var fc in sessionDataArray)
                {
                    while (fc?.StateChanges.TryDequeue(out var historyState) ?? false)
                    {
                        await WriteEvent(this.Response, fc.SessionId, new StateInfo(fc.FileId, historyState));
                    }
                }
                await Task.Delay(200);
            }
        }
    }
}

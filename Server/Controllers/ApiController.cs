﻿using System.Text.Json;
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
        /// <param name="Id">SessionId</param>
        /// <param name="data">Object of type SessionData</param>
        /// <returns>Received quantity of files</returns>
        [HttpPost("upload/{id}")]
        public async Task<int> Upload(string Id, [FromBody] List<string> files)
        {
            int successCount = 0;
            if (!files.Any())
                throw new ApplicationException("No files received.");

            var sessionId = Id;
            Conveyor.TryGetSessionChannel(sessionId, out var _);

            for (var i = 0; i < files.Count; i++)
            {
                var fileId = files[i];
                try
                {
                    var fc = new FileCard(sessionId, fileId, FileCardStateEnum.New);
                    Conveyor.TotalFileCardCatalog.TryAdd(fileId, fc);
                    Conveyor.ConveyorItemSingleton.In.Add(fc);
                    Interlocked.Increment(ref successCount);
                }
                catch (Exception ex)
                {
                    Conveyor.FileCardStateChanges.TryRemove(sessionId, out var _);
                    Logger.Error(ex, $"Error by upload file {fileId} in session {sessionId}.");
                }
            }

            Logger.Info($"--- added: {successCount} / total: {Conveyor.TotalFileCardCatalog.Count}");
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
        public async Task StatesStream(string id, CancellationToken token)
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
            Response.Headers.Append("Cache-Control", "no-cache");

            var sessionId = id;
            while (!token.IsCancellationRequested)
            {
                if (Conveyor.TryGetSessionChannel(sessionId, out var channel) && channel != null)
                {
                    try
                    {
                        await foreach (var stateInfo in channel.Reader.ReadAllAsync(token))
                        {
                            await WriteEvent(this.Response, sessionId, stateInfo);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Conveyor.FileCardStateChanges.TryRemove(sessionId, out var _);
                    }
                }
            }
        }
    }
}

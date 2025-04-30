using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Text.Json;

using NLog;

using Client.Enums;
using Client.Utilities;
using Client.Model;

namespace Client.Processing
{
    internal class WebClientApiSse
    {
        public static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private static ConcurrentDictionary<string, FileCardStateEnum> _waitStates = new ConcurrentDictionary<string, FileCardStateEnum>();

        private static string _pref = $"\r\n>>> Sessionid: \"{_cfg.ClientSessionId}\"";

        private static JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Main alhorithm
        /// </summary>
        public static async Task<int> Do(string? sessionId, string? srvApiUrl, CancellationToken token)
        {
            var retCode = 0;
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = token
            };

            using (var client = new HttpClient())
            {
                var files = new List<string>();
                #region [Upload]
                Logger.Info($"{_pref} - upload simulate - Start {{ ---");
                var srvUploadUrl = srvApiUrl + $"/upload/{sessionId}";
                for (var i = 0; i < 50; i++)
                {
                    var fileId = Guid.NewGuid().ToString("N");
                    files.Add(fileId);
                    _waitStates.TryAdd(fileId, FileCardStateEnum.Nothing);
                }

                using var jsonFiles = JsonContent.Create(files);
                using (var resp = await client.PostAsync(srvUploadUrl, jsonFiles))
                {
                    try
                    {
                        resp.EnsureSuccessStatusCode();
                        var countString = await resp.Content.ReadAsStringAsync();
                        var filesRecivedCount = int.TryParse(countString, out int tmpInt) ? tmpInt : 0;
                        Logger.Info($"{_pref} - successfully uploaded {filesRecivedCount} files.");
                    }
                    catch (Exception ex)
                    { 
                        Logger.Error(ex);
                    }
                    finally
                    {
                        resp.Content = null;
                    }
                }
                Logger.Info($"{_pref} - upload simulate - End }} ---");
                #endregion [Upload]

                //await Parallel.ForEachAsync(_waitStates.Keys.ToArray(), parallelOptions, async (fid, token) =>
                foreach (var fid in _waitStates.Keys.ToArray())
                {
                    if (token.IsCancellationRequested)
                        break;

                    var fileState = FileCardStateEnum.Nothing;
                    #region [State]
                    try
                    {
                        var startWaiting = DateTime.Now;
                        var waitFileFromServerTimeoutSec = _cfg.WaitFileFromServerTimeoutMin * 60;
                        while (fileState != FileCardStateEnum.Сompleted)
                        {
                            if (fileState != _waitStates[fid])
                            { // success
                                fileState = _waitStates[fid];
                                startWaiting = DateTime.Now;
                                Logger.Info($"{_pref} - State of file {fid} changed on: \"{fileState}\".");
                            }
                            else if ((DateTime.Now - startWaiting).TotalSeconds > waitFileFromServerTimeoutSec)
                            { // timeout
                                Logger.Error($"{_pref} - Waiting limit of file {fid} exceeded.");
                                //return;
                                break;
                            }
                            await Task.Delay(200);
                        }
                    }
                    finally
                    {
                        _waitStates.TryRemove(fid, out _);
                    }
                    #endregion [State]

                    if (fileState == FileCardStateEnum.Сompleted)
                    {
                        #region [Download]
                        var srvDownlUrl = srvApiUrl + $"/download/{fid}";
                        using (var resp = await client.GetAsync(srvDownlUrl, HttpCompletionOption.ResponseHeadersRead))
                        {
                            try
                            {
                                resp.EnsureSuccessStatusCode();
                                var jsonString = await resp.Content.ReadAsStringAsync();
                                if (jsonString != null)
                                {
                                    var stateInfo = JsonSerializer.Deserialize<StateInfo>(jsonString, _jsonOptions);
                                    Logger.Info($"{_pref} - successfully downloaded file: {stateInfo}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex);
                            }
                            finally
                            {
                                resp.Content = null;
                            }
                        }
                        #endregion [Download]

                        #region [Delete]
                        var srvDeleteUrl = srvApiUrl + $"/delete/{fid}";
                        using (var resp = await client.DeleteAsync(srvDeleteUrl))
                        {
                            try
                            {
                                resp.EnsureSuccessStatusCode();
                                var jsonString = await resp.Content.ReadAsStringAsync();
                                if (jsonString != null)
                                {
                                    var stateInfo = JsonSerializer.Deserialize<StateInfo>(jsonString, _jsonOptions);
                                    Logger.Info($"{_pref} - successfully deleted file: {stateInfo}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex);
                            }
                            finally
                            {
                                resp.Content = null;
                            }
                        }
                        #endregion [Delete]
                    }
                    //}, token);
                }
            }
            return retCode;
        }

        public static Task StatePolling(
            string? srvApiUrl,
            CancellationToken token
        )
        {
            var task = Task.Factory.StartNew(async () => {
                var srvStatesUrl = srvApiUrl + $"/states/{_cfg.ClientSessionId}";

                using var client = new HttpClient();
                using var stream = await client.GetStreamAsync(srvStatesUrl);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                await foreach (SseItem<StateInfo> item in SseParser.Create(
                    stream,
                    (eventType, bytes) => JsonSerializer.Deserialize<StateInfo>(bytes, options)).EnumerateAsync()
                )
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    if (item.Data != null)
                    {
                        var sessionId = item.EventId;
                        if (sessionId == _cfg.ClientSessionId)
                        {
                            var fileId = item.Data.FileId;
                            if (_waitStates.TryGetValue(fileId, out var curState))
                            {
                                if (curState != item.Data.State)
                                {
                                    _waitStates[fileId] = item.Data.State;
                                    Logger.Info($"{_pref} --- File \"{fileId}\" - state changed: {curState} -> {item.Data.State};");
                                }
                            }
                        }
                        else
                        {
                            Logger.Info($"{_pref} --- Error got jsonFiles from another session:\r\nEventId: {item.EventId};\r\nEventType: {item.EventType}];\r\nEventData: {{{item.Data}}};");
                        }
                    }
                }
            }, token);

            return task;
        }
    }
}

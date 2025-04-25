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

        private static string _pref = $"\r\n>>> Sessionid: \"{_cfg.SessionData.SessionId}\" ";

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
                #region [Upload action execution simulation]

                Logger.Info($"{_pref} - upload simulate for sessionId {_cfg.SessionData.SessionId} - Start {{ ---");
                for (var i = 0; i < _cfg.SessionData.Files.Count; i++)
                {
                    var fileId = _cfg.SessionData.Files[i];
                    _waitStates.TryAdd(fileId, FileCardStateEnum.Nothing);
                }

                var srvUploadurl = $"{srvApiUrl}/upload/{_cfg.SessionData.SessionId}";
                using var stream = await client.GetAsync(srvUploadurl);
                Logger.Info($"{_pref} - upload simulate for sessionId {_cfg.SessionData.SessionId} - End }} ---");
                #endregion [Upload action execution simulation]

                await Parallel.ForEachAsync(_waitStates.Keys.ToArray(), parallelOptions, async (fid, token) =>
                {
                    var fileState = FileCardStateEnum.Nothing;

                    #region [Wait Completed file state]
                    //Logger.Info($"{_pref} - file {fid} - state waiting - Start {{ ---");
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
                                Logger.Info($"{_pref} --- State of file {fid} changed on: \"{fileState}\".");
                            }
                            else if ((DateTime.Now - startWaiting).TotalSeconds > waitFileFromServerTimeoutSec)
                            { // timeout
                                Logger.Error($"{_pref} --- Waiting limit of file {fid} exceeded.");
                                return; // out from current file processing step
                            }
                            Task.Delay(200).Wait();
                        }
                    }
                    finally
                    {
                        _waitStates.TryRemove(fid, out _);
                    }
                    //Logger.Info($"{_pref} - file {fid} - state waiting - End }} ---");
                    #endregion [Wait Completed file state]

                    #region [Another actions execution designation]
                    //Logger.Info($"{_pref} - Actions execution designation to download and delete a file {fid} after changed it state on Complete...");
                    #endregion [Another actions execution designation]
                });

            }
            return retCode;
        }

        public static Task StatePolling(
            string? srvApiUrl,
            CancellationToken token
        )
        {
            var task = Task.Factory.StartNew(async () => {
                var clientSessionId = _cfg.SessionData.SessionId;
                var srvStatesUrl = srvApiUrl + $"/states/{clientSessionId}";

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
                        if (sessionId == clientSessionId)
                        {
                            var fileId = item.Data.FileId;
                            if (_waitStates.TryGetValue(fileId, out var curState))
                            {
                                if (curState != item.Data.State)
                                {
                                    _waitStates[fileId] = item.Data.State;
                                    //Logger.Info($"{_pref} - file \"{fileId}\" - state changed: {curState} -> {item.Data.State};");
                                }
                            }
                        }
                        else
                        { // output only what we catch
                            Logger.Info($"{_pref} - got data:\r\nEventId: \"{item.EventId}\";\r\nEventType: \"{item.EventType}\";\r\nEventData: {{{item.Data}}};");
                        }
                    }
                }
            }, token);

            return task;
        }
    }
}

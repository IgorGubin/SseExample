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

        private static string _pref = $"\r\n>>> Sessionid: \"{_cfg.ClientSessionId}\" ";

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
                var sessionData = new {sessionId = _cfg.ClientSessionId, files = new List<string>()};
                #region [Upload]
                Logger.Info($"{_pref} - upload simulate - Start {{ ---");
                var srvUploadUrl = srvApiUrl + "/upload";
                for (var i = 0; i < 50; i++)
                {
                    var fileId = Guid.NewGuid().ToString("N");
                    sessionData.files.Add(fileId);
                    _waitStates.TryAdd(fileId, FileCardStateEnum.Nothing);
                }

                using var data = JsonContent.Create(sessionData);
                var response = await client.PostAsync(srvUploadUrl, data);
                response.EnsureSuccessStatusCode();
                Logger.Info($"{_pref} - upload simulate - End }} ---");
                #endregion [Upload]

                await Parallel.ForEachAsync(_waitStates.Keys.ToArray(), parallelOptions, async (fid, token) =>
                {
                    var fileState = FileCardStateEnum.Nothing;
                    #region [State]
                    Logger.Info($"{_pref} - file {fid} - state waiting - Start {{ ---");
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
                                Logger.Info($"State of file {fid} changed on: \"{fileState}\".");
                            }
                            else if ((DateTime.Now - startWaiting).TotalSeconds > waitFileFromServerTimeoutSec)
                            { // timeout
                                Logger.Error($"Waiting limit of file {fid} exceeded.");
                                return;
                            }
                            Task.Delay(200).Wait();
                        }
                    }
                    finally
                    {
                        _waitStates.TryRemove(fid, out _);
                    }
                    Logger.Info($"{_pref} - file {fid} - state waiting - End }} ---");
                    #endregion [State]

                    if (fileState == FileCardStateEnum.Сompleted)
                    {
                        #region [Download]
                        Logger.Info($"{_pref} - file {fid} - download simulate - Start {{ ---");
                        var srvDownlUrl = srvApiUrl + $"/download/{fid}";
                        using (var resp = await client.GetAsync(srvDownlUrl, HttpCompletionOption.ResponseHeadersRead))
                        {
                            resp.EnsureSuccessStatusCode();
                            using (var respStream = await resp.Content.ReadAsStreamAsync())
                            {
                                
                            }
                            resp.Content = null;
                        }
                        Logger.Info($"{_pref} - file {fid} - download simulate - End }} ---");
                        #endregion [Download]

                        #region [Delete]
                        Logger.Info($"{_pref} - file {fid} - delete simulate - Start {{ ---");
                        var srvDeleteUrl = srvApiUrl + $"/delete/{fid}";
                        await client.DeleteAsync(srvDownlUrl);
                        Logger.Info($"{_pref} - file {fid} - delete simulate - End }} ---");
                        #endregion [Delete]
                    }
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
                        Logger.Info($"{_pref} - got data:\r\nEventId: {item.EventId};\r\nEventType: {item.EventType}];\r\nEventData: {{{item.Data}}};");
                        var sessionId = item.EventId;
                        if (sessionId == _cfg.ClientSessionId)
                        {
                            var fileId = item.Data.FileId;
                            if (_waitStates.TryGetValue(fileId, out var curState))
                            {
                                if (curState != item.Data.State)
                                {
                                    _waitStates[fileId] = item.Data.State;
                                    Logger.Info($"{_pref} - file \"{fileId}\" - state changed: {curState} -> {item.Data.State};");
                                }
                            }
                        }
                    }
                }
            }, token);

            return task;
        }
    }
}

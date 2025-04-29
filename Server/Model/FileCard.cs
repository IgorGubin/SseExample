using NLog;

using Server.Enums;
using Server.Processing;

namespace Server.Model
{
    internal class FileCard
    {
        private static NLog.ILogger Logger = LogManager.GetCurrentClassLogger();

        private StateInfo _state = new();

        public FileCard(string? sessionId, string? fileId, FileCardStateEnum state = FileCardStateEnum.Nothing)
        {
            SessionId = sessionId;
            _state.FileId = fileId;
            _state.State = state;
        }

        public string? SessionId { get; private set; }

        public string? FileId => _state.FileId;

        public StateInfo StateInfo => _state;

        /// <summary>
        /// State of file.
        /// </summary>
        public FileCardStateEnum State
        {
            get => _state.State;
            set
            {
                if (_state.State != value)
                {
                    _state.State = value;
                    if (Conveyor.TryGetSessionChannel(SessionId, out var channel) && channel != null)
                    {
                        channel.Writer.TryWrite(new StateInfo(FileId, _state.State));
                    }
                    else
                    {
                        Logger.Error($"Not found channel of session {SessionId}");
                    }
                }
            }
        }
    }
}

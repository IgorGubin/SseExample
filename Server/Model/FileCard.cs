using Server.Enums;
using System.Collections.Concurrent;

namespace Server.Model
{
    internal class FileCard
    {
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
        /// Queue of state changes history.
        /// </summary>
        public ConcurrentQueue<FileCardStateEnum> StateChanges { get; private set; } = new ConcurrentQueue<FileCardStateEnum>();

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
                    StateChanges.Enqueue(value);
                }
            }
        }
    }
}

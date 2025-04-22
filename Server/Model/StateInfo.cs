using Server.Enums;

namespace Server.Model
{
    public class StateInfo
    {
        public StateInfo() { }
        public StateInfo(string? fileId, FileCardStateEnum state = FileCardStateEnum.Nothing)
        {
            FileId = fileId;
            State = state;
        }

        public string? FileId { get; set; }

        public FileCardStateEnum State { get; set; }

        public override string ToString()
        {
            return $"Id: \"{FileId}\"; State: {State}";
        }
    }
}

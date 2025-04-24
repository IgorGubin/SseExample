using Client.Enums;

namespace Client.Model
{
    internal class StateInfo
    {
        public string? FileId { get; set; }

        public FileCardStateEnum State { get; set; }

        public override string ToString()
        {
            return $"FileId: \"{FileId}\"; State: {State}";
        }
    }
}

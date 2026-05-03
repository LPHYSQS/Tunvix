namespace Tunvix.Models
{
    public class AudioTrackRecord
    {
        public int ID { get; set; }

        public string TrackKey { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Artist { get; set; } = string.Empty;

        public long DurationMilliseconds { get; set; }

        public string SourceUri { get; set; } = string.Empty;

        public string SourcePath { get; set; } = string.Empty;

        public string MimeType { get; set; } = string.Empty;

        public string ImportScope { get; set; } = string.Empty;

        public string? FolderTreeUri { get; set; }
    }
}

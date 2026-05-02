namespace Tunvix.Services
{
    public sealed class AudioImportProgress
    {
        public AudioImportProgress(double progress, string status, int processed = 0, int total = 0)
        {
            Progress = progress;
            Status = status;
            Processed = processed;
            Total = total;
        }

        public double Progress { get; }

        public string Status { get; }

        public int Processed { get; }

        public int Total { get; }
    }
}

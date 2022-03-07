namespace Ant0nRocket.Lib.Dodb.Gateway
{
    public class ProgressEventArgs
    {
        public string JobDescription { get; init; }

        public ulong JobsTotalAmount { get; init; }

        public ulong JobsDone { get; init; }

        public double CompletePercents => JobsDone / JobsTotalAmount;
    }
}
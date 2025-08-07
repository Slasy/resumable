namespace ResumableFunctions.Data;

// TODO
internal class ResumableData<T> where T : struct
{
    internal class MetaData
    {
        public required string Id { get; init; }
        public required string MethodName { get; init; }
        public required DateTime StartTime { get; init; }
        public DateTime LastUpdateTime { get; } = DateTime.UtcNow;
        public required Type Type { get; init; }
    }

    public required MetaData Metadata { get; init; }
    public required IAsyncEnumerator<T> EnumeratorState { get; init; }
}

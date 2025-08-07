namespace ResumableFunctions.Data;

// TODO
internal class ResumableData<T> where T : struct
{
    internal class MetaData
    {
        public required string Name { get; init; }
        public DateTime CreateTime { get; } = DateTime.UtcNow;
        public required Type Type { get; init; }
    }

    public required MetaData Metadata { get; init; }
    public required IAsyncEnumerator<T> EnumeratorState { get; init; }
}

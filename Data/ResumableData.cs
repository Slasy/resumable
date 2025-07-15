namespace ResumableFunctions.Data;

// TODO
internal class ResumableData<T>
{
    internal class MData
    {
        public required string Name { get; init; }
        public DateTime CreateTime { get; } = DateTime.UtcNow;
    }

    public required MData Metadata { get; init; }
    public required IAsyncEnumerator<T> EnumeratorState { get; init; }
}

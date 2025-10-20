namespace AdministraAoImoveis.Domain.ValueObjects;

public readonly record struct TimeRange
{
    public TimeRange(DateTime start, DateTime end)
    {
        if (end < start)
        {
            throw new ArgumentException("End must be greater than or equal to Start", nameof(end));
        }

        Start = start;
        End = end;
    }

    public DateTime Start { get; }
    public DateTime End { get; }

    public bool Overlaps(TimeRange other) => Start < other.End && other.Start < End;
}

using AnkiBridge.Domain.SeedWork;
using AnkiBridge.Shared.Results;

namespace AnkiBridge.Domain.Aggregates.Dictionary;

public sealed class DictionaryDefinition : Entity<Guid>
{
    public string Text { get; private set; } = default!;
    public int OrderIndex { get; private set; }

    private readonly List<DictionaryExample> _examples = [];
    public IReadOnlyCollection<DictionaryExample> Examples => _examples.AsReadOnly();

    #region Constructors

    private DictionaryDefinition() { }

    private DictionaryDefinition(string definition, int orderIndex)
    {
        Id = Guid.CreateVersion7();
        Text = definition;
        OrderIndex = orderIndex;
    }

    #endregion

    #region Factory Method

    internal static Result<DictionaryDefinition> Create(string definition, int orderIndex)
    {
        if (string.IsNullOrWhiteSpace(definition))
            return Result.Failure<DictionaryDefinition>("Definition text must not be empty.");

        return new DictionaryDefinition(definition.Trim(), orderIndex);
    }

    #endregion

    #region Behavior Methods

    internal Result AddExample(string text)
    {
        var result = DictionaryExample.Create(text);
        if (result.IsFailure)
            return result;

        _examples.Add(result.Value);

        return Result.Success();
    }

    internal Result AddExamples(IReadOnlyList<string> texts)
    {
        var results = texts.Select(DictionaryExample.Create).ToList();

        var failure = results.FirstOrDefault(r => r.IsFailure);
        if (failure is not null)
            return Result.Failure(failure.Error);

        _examples.AddRange(results.Select(r => r.Value));

        return Result.Success();
    }

    #endregion
}
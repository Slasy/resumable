namespace ResumableFunctions;

public readonly struct ResumableFunctionState
{
    public enum State
    {
        Invalid,
        Checkpoint,
        CompleteSuccess,
        CompleteFail,
    }

    public readonly State stateValue;
    public readonly Exception? stateException;

    internal ResumableFunctionState(State state, Exception? exception = null)
    {
        stateValue = state;
        stateException = exception;
    }

    /// <summary>Continuation state</summary>
    public static ResumableFunctionState Checkpoint() => new(State.Checkpoint);

    /// <summary>Finish state</summary>
    public static ResumableFunctionState<T> Success<T>(T? result) => new(State.CompleteSuccess, result);

    /// <summary>Finish state</summary>
    public static ResumableFunctionState Success() => new(State.CompleteSuccess);

    /// <summary>Finish state</summary>
    public static ResumableFunctionState<T> Fail<T>(T? result) => new(State.CompleteFail, result);

    /// <summary>Finish state</summary>
    public static ResumableFunctionState Fail(Exception? exception) => new(State.CompleteFail, exception);

    /// <summary>Finish state</summary>
    public static ResumableFunctionState Fail() => new(State.CompleteFail);
}

public readonly struct ResumableFunctionState<T>
{
    public readonly ResumableFunctionState.State stateValue;
    public readonly T? resultValue;
    public readonly Exception? stateException;

    internal ResumableFunctionState(ResumableFunctionState.State state, T? result, Exception? exception = null)
    {
        stateValue = state;
        resultValue = result;
        stateException = exception;
    }

    public static implicit operator ResumableFunctionState<T>(ResumableFunctionState state)
    {
        return new ResumableFunctionState<T>(state.stateValue, default, state.stateException);
    }

    public static explicit operator ResumableFunctionState(ResumableFunctionState<T> state)
    {
        return new ResumableFunctionState(state.stateValue, state.stateException);
    }
}

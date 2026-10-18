namespace LeagueManager.Application.Abstractions;

// Kept separate from IQuery so reads and writes can get different cross-cutting behaviour later
// (for example, only commands invalidate the standings cache).
public interface ICommand<TResult>;

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

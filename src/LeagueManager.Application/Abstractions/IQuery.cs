namespace LeagueManager.Application.Abstractions;

// Marker interface: the generic argument ties a query to its result type at compile time,
// so the wrong handler or result can never be wired together.
public interface IQuery<TResult>;

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

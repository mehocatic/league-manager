namespace LeagueManager.Application.Teams;

// The API returns DTOs, never entities: it hides navigation properties and avoids JSON reference cycles.
public sealed record TeamDto(int Id, string Name, string City);

using LeagueManager.Domain.Entities;

namespace LeagueManager.Infrastructure.Persistence.Seed;

// Players and scorers are invented on purpose: the capstone may not contain real people's personal data.
internal static class FictionalPlayers
{
    private static readonly string[] FirstNames =
    [
        "Adnan", "Admir", "Almir", "Amar", "Armin", "Benjamin", "Damir", "Dario", "Dženan", "Edin",
        "Eldin", "Elvir", "Emir", "Faruk", "Haris", "Ismar", "Ivan", "Jasmin", "Josip", "Kenan",
        "Kerim", "Luka", "Marko", "Mario", "Mirza", "Nedim", "Nermin", "Nikola", "Omer", "Samir",
        "Selmir", "Senad", "Stjepan", "Tarik", "Vedad", "Zlatan"
    ];

    private static readonly string[] LastNames =
    [
        "Avdić", "Babić", "Begić", "Bešić", "Čolić", "Dedić", "Delić", "Fazlić", "Grgić", "Gušić",
        "Hadžić", "Halilović", "Hasanović", "Hodžić", "Ibrahimović", "Imamović", "Jurić", "Jusić", "Karić", "Knežević",
        "Kovačević", "Kurtović", "Lukić", "Marić", "Mehmedović", "Mujić", "Muratović", "Nuhanović", "Omerović", "Osmanović",
        "Pašić", "Perić", "Ramić", "Salihović", "Šarić", "Šehić", "Softić", "Tahirović", "Tomić", "Topić", "Zukić"
    ];

    public static List<Player> CreateSquad(Random random, int size)
    {
        // Shirt 1 goes to the goalkeeper; the rest are spread over 2-30 like a real squad list.
        var outfieldNumbers = Enumerable.Range(2, 29).ToArray();
        random.Shuffle(outfieldNumbers);

        // Unique names within a squad, otherwise two identical scorers would look like one player in the scorer list.
        var usedNames = new HashSet<string>();
        var squad = new List<Player>(size);

        for (var i = 0; i < size; i++)
        {
            string firstName, lastName;
            do
            {
                firstName = FirstNames[random.Next(FirstNames.Length)];
                lastName = LastNames[random.Next(LastNames.Length)];
            }
            while (!usedNames.Add($"{firstName} {lastName}"));

            squad.Add(new Player
            {
                FirstName = firstName,
                LastName = lastName,
                ShirtNumber = i == 0 ? 1 : outfieldNumbers[i - 1]
            });
        }

        return squad;
    }

    public static List<Goal> CreateGoals(Random random, Team home, int homeGoals, Team away, int awayGoals)
    {
        var scorers = Enumerable.Range(0, homeGoals).Select(_ => PickScorer(random, home.Players))
            .Concat(Enumerable.Range(0, awayGoals).Select(_ => PickScorer(random, away.Players)))
            .ToArray();

        // Shuffled before pairing with sorted minutes, otherwise the home team would always score first.
        random.Shuffle(scorers);

        // Distinct minutes keep the match timeline readable; at most 7 goals per match, so this never loops long.
        var minutes = new HashSet<int>();
        while (minutes.Count < scorers.Length)
        {
            minutes.Add(random.Next(1, 91));
        }

        return minutes.Order()
            .Zip(scorers, (minute, player) => new Goal { Minute = minute, Player = player })
            .ToList();
    }

    private static Player PickScorer(Random random, List<Player> squad)
    {
        // Weighted so a few forwards score most of the goals, which gives the top-scorers list a realistic shape.
        // Index 0 is the goalkeeper and never scores.
        static int Weight(int index) => index switch
        {
            0 => 0,
            <= 3 => 6,
            <= 8 => 3,
            _ => 1
        };

        var roll = random.Next(Enumerable.Range(0, squad.Count).Sum(Weight));
        for (var i = 0; i < squad.Count; i++)
        {
            roll -= Weight(i);
            if (roll < 0)
            {
                return squad[i];
            }
        }

        throw new InvalidOperationException("Weighted scorer pick fell through; the squad has no eligible scorers.");
    }
}

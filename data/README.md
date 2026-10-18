# Seed data: 2. liga FBiH - Zapad, season 2026/2027

Source: official fixtures and standings published by the Football Association of FBiH
(https://www.nsfbih.ba/takmicenja/druga-liga-fbih-zapad), collected on 2026-10-18.

| File | Contents |
|---|---|
| `teams.csv` | 14 clubs: name, city |
| `fixtures.csv` | Rounds 1-13. Rounds 1-9 are played (goals filled in), rounds 10-13 are scheduled (goals empty). `kickoff_local` is local time (Europe/Sarajevo). |
| `official-standings-after-round-9.csv` | The official table after round 9, used to check that our calculated standings are correct. |

Only public data (club names, fixtures, results) is used. Players and scorers in the app are fictional.

Note: there are two different clubs named Rudar: `NK RUDAR` (Sanski Most - Kamengrad) and `FK RUDAR` (Han Bila).

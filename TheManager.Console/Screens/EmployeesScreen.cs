using Spectre.Console;
using TheManager.Models;
using TheManager.Services;

namespace TheManager.ConsoleApp.Screens;

internal static class EmployeesScreen
{
    public static void Show(GameState state, Random rng)
    {
        while (true)
        {
            Ui.Header($"EMPLOYEES  ·  {state.Club.Name.Trim()}");
            AnsiConsole.Write(BuildStaffTable(state));
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(
                $"  [dim]Weekly staff wage bill:[/]  " +
                $"[cyan]{Ui.FormatMoney(StaffService.TotalStaffWageBill(state.Coach, state.Physio, state.Scouts, state.YouthTeam))}[/]");
            AnsiConsole.WriteLine();

            var choices = BuildMenuChoices(state);
            choices.Add("Back");

            var pick = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]What would you like to do?[/]")
                    .AddChoices(choices));

            if (pick == "Back") break;
            HandleAction(pick, state, rng);
        }
    }

    // ── Staff table ───────────────────────────────────────────────────────────

    private static Table BuildStaffTable(GameState state)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[dim]Role[/]")
            .AddColumn("[bold]Name[/]")
            .AddColumn(new TableColumn("[dim]Skill[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]Wage/wk[/]").RightAligned())
            .AddColumn("[dim]Notes[/]");

        // Coach
        if (state.Coach is { } coach)
            table.AddRow("Coach",
                Markup.Escape(coach.Name.Trim()),
                $"{coach.SkillPercent}%",
                Ui.FormatMoney(coach.WeeklySalary),
                "[dim]improves youth training[/]");
        else
            table.AddRow("[dim]Coach[/]", "[dim]—[/]", "[dim]—[/]", "[dim]—[/]", string.Empty);

        // Physio
        if (state.Physio is { } physio)
            table.AddRow("Physio",
                Markup.Escape(physio.Name.Trim()),
                $"{physio.SkillPercent}%",
                Ui.FormatMoney(physio.WeeklySalary),
                "[dim]reduces injury duration[/]");
        else
            table.AddRow("[dim]Physio[/]", "[dim]—[/]", "[dim]—[/]", "[dim]—[/]", string.Empty);

        // Scouts
        if (state.Scouts.Count == 0)
        {
            table.AddRow("[dim]Scout[/]", "[dim]—[/]", "[dim]—[/]", "[dim]—[/]", string.Empty);
        }
        else
        {
            foreach (var s in state.Scouts)
                table.AddRow(
                    "Scout",
                    Markup.Escape(s.Name.Trim()),
                    $"{s.SkillPercent}%",
                    Ui.FormatMoney(s.WeeklySalary),
                    s.HasCriteria
                        ? $"[dim]{PositionAbbr(s.LookingForPosition)} skill ≥{s.LookingForForm}[/]"
                        : "[dim]no criteria[/]");
        }

        // Youth players
        if (state.YouthTeam.Count == 0)
        {
            table.AddRow("[dim]Youth[/]", "[dim]—[/]", "[dim]—[/]", "[dim]—[/]", string.Empty);
        }
        else
        {
            foreach (var y in state.YouthTeam)
            {
                string potCell = y.HasReachedPotential
                    ? "[green]peak[/]"
                    : $"pot:{y.PotentialSkillPercent}%";
                table.AddRow(
                    $"Youth",
                    Markup.Escape(y.Name.Trim()),
                    $"{y.SkillPercent}%",
                    Ui.FormatMoney(y.WeeklySalary),
                    $"{Markup.Escape(PositionAbbr(y.Position))} · age {y.Age} · {potCell}");
            }
        }

        return table;
    }

    // ── Menu ──────────────────────────────────────────────────────────────────

    private static List<string> BuildMenuChoices(GameState state)
    {
        var list = new List<string>();
        list.Add(state.Club.HasCoach  ? "Sack coach"  : "Hire coach");
        list.Add(state.Club.HasPhysio ? "Sack physio" : "Hire physio");
        if (state.Club.ScoutCount < 3) list.Add("Hire scout");
        if (state.Club.ScoutCount > 0) list.Add("Sack a scout");
        if (state.Club.YouthPlayerCount < 7) list.Add("Sign youth player");
        if (state.Club.YouthPlayerCount > 0) list.Add("Release youth player");
        bool anyEligible = state.YouthTeam.Any(y => y.IsEligibleForPromotion);
        bool freeSlot    = Enumerable.Range(13, 8).Any(i => state.Squad[i] is null);
        if (anyEligible && freeSlot) list.Add("Promote youth player");
        return list;
    }

    // ── Action handlers ───────────────────────────────────────────────────────

    private static void HandleAction(string action, GameState state, Random rng)
    {
        switch (action)
        {
            case "Hire coach":
            {
                var c = StaffService.HireCoach(state.Club, rng);
                if (c != null) { state.Coach = c; ShowHired("coach", c.Name, c.SkillPercent, c.WeeklySalary); }
                break;
            }
            case "Sack coach":
                if (ConfirmSack(state.Coach!.Name)) { StaffService.SackCoach(state.Club); state.Coach = null; }
                break;

            case "Hire physio":
            {
                var p = StaffService.HirePhysio(state.Club, rng);
                if (p != null) { state.Physio = p; ShowHired("physio", p.Name, p.SkillPercent, p.WeeklySalary); }
                break;
            }
            case "Sack physio":
                if (ConfirmSack(state.Physio!.Name)) { StaffService.SackPhysio(state.Club); state.Physio = null; }
                break;

            case "Hire scout":
            {
                var s = StaffService.HireScout(state.Club, state.Scouts, rng);
                if (s != null)
                    ShowHired("scout — set criteria in Scout Reports",
                        s.Name, s.SkillPercent, s.WeeklySalary);
                break;
            }
            case "Sack a scout":
                SackScout(state);
                break;

            case "Sign youth player":
            {
                var posPick = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold]Which position are you recruiting for?[/]")
                        .AddChoices("Goalkeeper", "Defender", "Midfielder", "Attacker", "Cancel"));
                if (posPick == "Cancel") break;

                var position = posPick switch
                {
                    "Goalkeeper" => PlayerPosition.Goalkeeper,
                    "Defender"   => PlayerPosition.Defender,
                    "Midfielder" => PlayerPosition.Midfielder,
                    _            => PlayerPosition.Attacker
                };

                var y = StaffService.HireYouthPlayer(state.Club, state.YouthTeam, rng, position);
                if (y is not null)
                    ShowHired(
                        $"youth {PositionAbbr(y.Position)}, age {y.Age}, pot {y.PotentialSkillPercent}%",
                        y.Name, y.SkillPercent, y.WeeklySalary);
                else
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("  [yellow]Your youth team is already full.[/]");
                    Ui.Pause();
                }
                break;
            }
            case "Release youth player":
                ReleaseYouthPlayer(state);
                break;

            case "Promote youth player":
                PromoteYouthPlayer(state, rng);
                break;
        }
    }

    private static void SackScout(GameState state)
    {
        var options = state.Scouts
            .Select((s, i) =>
                $"{i + 1}. {s.Name.Trim()}  skill:{s.SkillPercent}%  " +
                (s.HasCriteria ? $"{PositionAbbr(s.LookingForPosition)} ≥{s.LookingForForm}" : "no criteria"))
            .Append("Cancel")
            .ToList();

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Which scout?[/]")
                .AddChoices(options));

        if (pick == "Cancel") return;
        int idx = int.Parse(pick.Split('.')[0]) - 1;
        if (ConfirmSack(state.Scouts[idx].Name))
            StaffService.SackScout(state.Club, state.Scouts, idx);
    }

    private static void ReleaseYouthPlayer(GameState state)
    {
        var options = state.YouthTeam
            .Select((y, i) =>
                $"{i + 1}. {y.Name.Trim()}  {PositionAbbr(y.Position)}  skill:{y.SkillPercent}%  age:{y.Age}")
            .Append("Cancel")
            .ToList();

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Which youth player?[/]")
                .AddChoices(options));

        if (pick == "Cancel") return;
        int idx = int.Parse(pick.Split('.')[0]) - 1;
        if (AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[yellow]Release {Markup.Escape(state.YouthTeam[idx].Name.Trim())}?[/]")
                    .AddChoices("Yes", "No")) == "Yes")
            StaffService.SackYouthPlayer(state.Club, state.YouthTeam, idx);
    }

    private static void PromoteYouthPlayer(GameState state, Random rng)
    {
        var eligible = state.YouthTeam
            .Select((y, i) => (youth: y, index: i))
            .Where(t => t.youth.IsEligibleForPromotion)
            .ToList();

        var options = eligible
            .Select(t =>
                $"{t.index + 1}. {t.youth.Name.Trim(),-9}  " +
                $"{PositionAbbr(t.youth.Position),-3}  " +
                $"skill:{t.youth.SkillPercent}%  " +
                $"pot:{t.youth.PotentialSkillPercent}%  " +
                $"age:{t.youth.Age}")
            .Append("Cancel")
            .ToList();

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Promote which youth player?[/]")
                .AddChoices(options));

        if (pick == "Cancel") return;

        int youthIdx = int.Parse(pick.Split('.')[0]) - 1;
        var result   = StaffService.PromoteYouthPlayer(state.Squad, state.YouthTeam, state.Club, youthIdx, rng);

        AnsiConsole.WriteLine();
        if (result is null)
        {
            AnsiConsole.MarkupLine("  [red]Promotion failed[/] — no free reserve slot available.");
        }
        else
        {
            var player = result.Value.player;
            var slot   = result.Value.slot;
            AnsiConsole.MarkupLine(
                $"  [green]Promoted![/] {Markup.Escape(player.Name.Trim())} joins the squad " +
                $"at slot {slot}  ({PositionAbbr(player.Position)}  skill {player.Skill:F1}).");
        }
        Ui.Pause();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void ShowHired(string roleDescription, string name, int skill, double wage)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"  [green]Hired:[/] {Markup.Escape(name.Trim())}  " +
            $"[dim]({Markup.Escape(roleDescription)})[/]  " +
            $"Skill: [bold]{skill}%[/]  " +
            $"Wage: [cyan]{Ui.FormatMoney(wage)}/wk[/]");
        Ui.Pause();
    }

    private static bool ConfirmSack(string name) =>
        AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[yellow]Sack {Markup.Escape(name.Trim())}?[/]")
                .AddChoices("Yes", "No")) == "Yes";

    private static string PositionAbbr(PlayerPosition pos) => pos switch
    {
        PlayerPosition.Goalkeeper => "GK",
        PlayerPosition.Defender   => "DEF",
        PlayerPosition.Midfielder => "MID",
        PlayerPosition.Attacker   => "ATK",
        _                         => pos.ToString()
    };
}

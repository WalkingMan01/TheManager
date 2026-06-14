using Spectre.Console;
using TheManager.Models;
using TheManager.Services;

namespace TheManager.ConsoleApp.Screens;

internal static class ScoutReportsScreen
{
    public static void Show(GameState state, Random rng)
    {
        while (true)
        {
            Ui.Header($"SCOUT REPORTS  ·  {state.Club.Name.Trim()}");
            AnsiConsole.Write(BuildAssignmentsTable(state));

            var findings = CurrentFindings(state);
            if (findings.Count > 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("  [bold]Current findings[/]");
                AnsiConsole.Write(BuildFindingsTable(findings));
            }

            AnsiConsole.WriteLine();

            if (state.Scouts.Count == 0)
            {
                AnsiConsole.MarkupLine("  [dim]No scouts employed — hire scouts from the Employees screen.[/]");
                Ui.Pause();
                break;
            }

            var choices = new List<string> { "Set scout criteria" };
            if (findings.Count > 0) choices.Add("Sign a scouted player");
            if (findings.Count > 0) choices.Add("Dismiss a finding");
            choices.Add("Back");

            var pick = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]What would you like to do?[/]")
                    .AddChoices(choices));

            switch (pick)
            {
                case "Set scout criteria":    AssignScout(state);                    break;
                case "Sign a scouted player": SignPlayer(state, rng, findings);      break;
                case "Dismiss a finding":     DismissFinding(state, findings);       break;
                default: return;
            }
        }
    }

    // ── Tables ────────────────────────────────────────────────────────────────

    private static Table BuildAssignmentsTable(GameState state)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[dim]#[/]")
            .AddColumn("[bold]Scout[/]")
            .AddColumn(new TableColumn("[dim]Skill[/]").RightAligned())
            .AddColumn("[dim]Looking for[/]");

        for (int i = 0; i < state.Scouts.Count; i++)
        {
            var s = state.Scouts[i];
            string criteria = s.HasCriteria
                ? $"{PositionAbbr(s.LookingForPosition)}  skill ≥{s.LookingForForm}"
                : "[dim]no criteria set[/]";
            table.AddRow(
                $"[dim]{i + 1}[/]",
                Markup.Escape(s.Name.Trim()),
                $"{s.SkillPercent}%",
                criteria);
        }

        return table;
    }

    private static Table BuildFindingsTable(List<ScoutFinding> findings)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[dim]Scout[/]")
            .AddColumn("[bold]Player[/]")
            .AddColumn("[dim]Pos[/]")
            .AddColumn(new TableColumn("[dim]Skill[/]").RightAligned())
            .AddColumn(new TableColumn("[dim]Age[/]").RightAligned())
            .AddColumn("[dim]Temper[/]")
            .AddColumn("[dim]From[/]");

        foreach (var f in findings)
        {
            table.AddRow(
                Markup.Escape(f.ScoutName),
                Markup.Escape(f.Player.Name.Trim()),
                PositionAbbr(f.Player.Position),
                $"{f.Player.Skill:F1}",
                f.Player.DisplayAge.ToString(),
                f.Player.Temper.ToString(),
                Markup.Escape(f.SourceClubName));
        }

        return table;
    }

    // ── Assign scout to team ──────────────────────────────────────────────────

    private static void AssignScout(GameState state)
    {
        // Step 1: pick which scout
        var scoutChoices = state.Scouts
            .Select((s, i) =>
            {
                string criteria = s.HasCriteria
                    ? $"{PositionAbbr(s.LookingForPosition)} skill ≥{s.LookingForForm}"
                    : "no criteria";
                return $"{i + 1}. {s.Name.Trim()}  ({s.SkillPercent}%)  {criteria}";
            })
            .Append("Cancel")
            .ToList();

        var scoutPick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Which scout to instruct?[/]")
                .AddChoices(scoutChoices));

        if (scoutPick == "Cancel") return;
        int scoutIdx = int.Parse(scoutPick.Split('.')[0]) - 1;
        var scout    = state.Scouts[scoutIdx];

        // Step 2: pick position
        var posPick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]What position should they look for?[/]")
                .AddChoices("Goalkeeper", "Defender", "Midfielder", "Attacker", "Cancel"));

        if (posPick == "Cancel") return;

        PlayerPosition position = posPick switch
        {
            "Goalkeeper" => PlayerPosition.Goalkeeper,
            "Defender"   => PlayerPosition.Defender,
            "Midfielder" => PlayerPosition.Midfielder,
            _            => PlayerPosition.Attacker
        };

        // Step 3: pick skill tier
        var tierPick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]What skill level?[/]")
                .AddChoices(
                    "2  — developing (Div 4)",
                    "4  — average    (Div 3)",
                    "6  — good       (Div 2)",
                    "8  — elite      (Div 1)",
                    "Cancel"));

        if (tierPick == "Cancel") return;

        int form = int.Parse(tierPick.Split(' ')[0]);

        scout.LookingForPosition = position;
        scout.LookingForForm     = form;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"  [green]{Markup.Escape(scout.Name.Trim())}[/] will now look for a " +
            $"[bold]{PositionAbbr(position)}[/] with skill ≥{form}.");
        Ui.Pause();
    }

    // ── Sign a scouted player ─────────────────────────────────────────────────

    private static void SignPlayer(GameState state, Random rng, List<ScoutFinding> findings)
    {
        // Step 1: pick which finding to pursue
        var options = findings
            .Select((f, i) =>
                $"{i + 1}. {f.Player.Name.Trim()}  " +
                $"{PositionAbbr(f.Player.Position)}  " +
                $"skill:{f.Player.Skill:F1}  age:{f.Player.DisplayAge}  " +
                $"from:{f.SourceClubName}")
            .Append("Cancel")
            .ToList();

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Which player to approach?[/]")
                .AddChoices(options));

        if (pick == "Cancel") return;
        int idx = int.Parse(pick.Split('.')[0]) - 1;
        var finding = findings[idx];
        var player  = finding.Player;

        // Step 2: show details + asking price
        double askingPrice = TransferService.CalculateAskingPrice(player, rng);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [bold]{Markup.Escape(player.Name.Trim())}[/]  " +
                               $"{PositionAbbr(player.Position)}  " +
                               $"skill [bold]{player.Skill:F1}[/]  age {player.DisplayAge}  temper {player.Temper}");
        AnsiConsole.MarkupLine($"  Asking price: [cyan]{Ui.FormatMoney(askingPrice)}[/]");
        AnsiConsole.MarkupLine($"  Your balance: [cyan]{Ui.FormatMoney(state.Finances.BankBalance)}[/]");
        AnsiConsole.WriteLine();

        // Step 3: check we have a free reserve slot first
        int freeSlot = FindFreeReserveSlot(state.Squad);
        if (freeSlot < 0)
        {
            AnsiConsole.MarkupLine("  [red]No free reserve slot — release a squad player first.[/]");
            Ui.Pause();
            return;
        }

        // Step 4: offer terms
        AnsiConsole.MarkupLine("  [dim]Enter your offer terms (0 to cancel each):[/]");
        AnsiConsole.WriteLine();

        int wage = PromptInt("  Weekly wage (£): ", 0, 99_999);
        if (wage == 0) return;

        int signingFee = PromptInt("  Signing fee (£): ", 0, 9_999_999);
        if (signingFee == 0) return;

        int contractWeeks = PromptInt("  Contract (weeks, 1–260): ", 0, 260);
        if (contractWeeks == 0) return;

        // Step 5: evaluate
        var refusal = TransferService.EvaluateContractOffer(
            player, wage, signingFee, contractWeeks,
            (int)state.Club.Division, rng);

        AnsiConsole.WriteLine();

        if (refusal != RefusalReason.None)
        {
            string reason = refusal switch
            {
                RefusalReason.WageTooLow      => "the wage offer is too low",
                RefusalReason.SigningFeeTooLow => "the signing fee is too low",
                RefusalReason.ContractTooLong  => "the contract length is too long",
                _                              => "he's decided to stay put"
            };
            AnsiConsole.MarkupLine($"  [red]Deal rejected[/] — {reason}.");
            Ui.Pause();
            return;
        }

        // Step 6: commit the deal
        TransferService.CommitDeal(player, state.Finances, askingPrice, signingFee, wage, contractWeeks);
        state.Squad[freeSlot] = player;
        state.Squad[finding.SquadSlot] = null;   // clear the scout market slot

        AnsiConsole.MarkupLine(
            $"  [green]Deal done![/] {Markup.Escape(player.Name.Trim())} joins the reserves (slot {freeSlot}).");
        AnsiConsole.MarkupLine(
            $"  Total cost: [cyan]{Ui.FormatMoney(askingPrice + signingFee)}[/]   " +
            $"Balance: [cyan]{Ui.FormatMoney(state.Finances.BankBalance)}[/]");
        Ui.Pause();
    }

    // ── Dismiss a finding ─────────────────────────────────────────────────────

    private static void DismissFinding(GameState state, List<ScoutFinding> findings)
    {
        var options = findings
            .Select((f, i) =>
                $"{i + 1}. {f.Player.Name.Trim()}  " +
                $"{PositionAbbr(f.Player.Position)}  " +
                $"skill:{f.Player.Skill:F1}  " +
                $"({Markup.Escape(f.ScoutName)})")
            .Append("Cancel")
            .ToList();

        var pick = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Dismiss which finding?[/]")
                .AddChoices(options));

        if (pick == "Cancel") return;
        int idx = int.Parse(pick.Split('.')[0]) - 1;
        state.Squad[findings[idx].SquadSlot] = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<ScoutFinding> CurrentFindings(GameState state)
    {
        var list = new List<ScoutFinding>();
        for (int slot = 21; slot <= 23; slot++)
        {
            var player = state.Squad[slot];
            if (player is null) continue;

            int scoutListIdx = slot - 21;
            string scoutName = scoutListIdx < state.Scouts.Count
                ? state.Scouts[scoutListIdx].Name.Trim()
                : $"Scout {scoutListIdx + 1}";

            list.Add(new ScoutFinding
            {
                ScoutIndex      = scoutListIdx,
                ScoutName       = scoutName,
                SquadSlot       = slot,
                Player          = player,
                SourceClubIndex = player.GamesPlayed,   // stored as team index per BASIC convention
                SourceClubName  = player.GamesPlayed > 0 && player.GamesPlayed < state.AllTeamNames.Length
                    ? state.AllTeamNames[player.GamesPlayed].Trim()
                    : "Unknown"
            });
        }
        return list;
    }

    private static int FindFreeReserveSlot(Player?[] squad)
    {
        for (int i = 13; i <= 20; i++)
            if (squad[i] is null) return i;
        return -1;
    }

    private static int PromptInt(string prompt, int min, int max)
    {
        while (true)
        {
            var raw = AnsiConsole.Prompt(new TextPrompt<string>(prompt).AllowEmpty());
            if (int.TryParse(raw, out int v) && v >= min && v <= max) return v;
            AnsiConsole.MarkupLine($"  [red]Enter a number between {min} and {max}.[/]");
        }
    }

    private static string PositionAbbr(PlayerPosition pos) => pos switch
    {
        PlayerPosition.Goalkeeper => "GK",
        PlayerPosition.Defender   => "DEF",
        PlayerPosition.Midfielder => "MID",
        PlayerPosition.Attacker   => "ATK",
        _                         => pos.ToString()
    };
}

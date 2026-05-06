using FootballBoss.Models;
using FootballBoss.Services;

namespace FootballBoss.MatchHarness;

/// <summary>
/// Windows Forms test harness for <see cref="MatchEngine"/>.
/// Lets you set all <see cref="MatchSetupInput"/> parameters, simulate a single
/// match or run a batch, and inspect goal events and incidents.
/// </summary>
internal sealed class MatchHarnessForm : Form
{
    // ── Home team inputs ──────────────────────────────────────────────────────
    private readonly NumericUpDown _homeGk     = NumSpin(0, 9,  4);
    private readonly NumericUpDown _homeDef    = NumSpin(0, 9,  4);
    private readonly NumericUpDown _homeMid    = NumSpin(0, 9,  4);
    private readonly NumericUpDown _homeAtk    = NumSpin(0, 9,  4);
    private readonly NumericUpDown _homeMorale = NumSpin(0, 99, 50);
    private readonly NumericUpDown _homeTemper = NumSpin(0, 99, 30);

    // ── Away team inputs ──────────────────────────────────────────────────────
    private readonly NumericUpDown _awayGk     = NumSpin(0, 9,  4);
    private readonly NumericUpDown _awayDef    = NumSpin(0, 9,  4);
    private readonly NumericUpDown _awayMid    = NumSpin(0, 9,  4);
    private readonly NumericUpDown _awayAtk    = NumSpin(0, 9,  4);
    private readonly NumericUpDown _awayMorale = NumSpin(0, 99, 50);
    private readonly NumericUpDown _awayTemper = NumSpin(0, 99, 30);

    // ── Match condition inputs ────────────────────────────────────────────────
    private readonly CheckBox      _chkHome          = new() { Text = "Home game",       Checked = true,  AutoSize = true };
    private readonly CheckBox      _chkLostLast      = new() { Text = "Lost last match", Checked = false, AutoSize = true };
    private readonly NumericUpDown _numLineupChanges = NumSpin(0, 9,  0);
    private readonly NumericUpDown _numDivision      = NumSpin(1, 4,  1);
    private readonly NumericUpDown _numPhysioSkill   = NumSpin(0, 99, 0);
    private readonly NumericUpDown _numPrevLegUs     = NumSpin(0, 9,  0);
    private readonly NumericUpDown _numPrevLegThem   = NumSpin(0, 9,  0);

    // ── Batch run controls ────────────────────────────────────────────────────
    private readonly NumericUpDown _numBatchSize = NumSpin(10, 10_000, 100);

    // ── Output ────────────────────────────────────────────────────────────────
    private readonly RichTextBox _output     = new();
    private readonly Label       _statsLabel = new();

    private readonly Random _rng = new();

    public MatchHarnessForm()
    {
        Text            = "Match Engine Test Harness — FootballBoss";
        Size            = new Size(960, 720);
        MinimumSize     = new Size(800, 640);
        StartPosition   = FormStartPosition.CenterScreen;
        Font            = new Font("Segoe UI", 9f);
        BackColor       = SystemColors.Control;

        BuildLayout();
        SetToolTips();
    }

    // ── Layout ────────────────────────────────────────────────────────────────

    private void BuildLayout()
    {
        var outer = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 4,
            Padding     = new Padding(8),
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 265)); // inputs row
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // button row
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // output
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));  // stats

        // ── Top: three group boxes ────────────────────────────────────────────
        var topRow = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 3,
            RowCount    = 1,
            AutoSize    = true,
        };
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));

        topRow.Controls.Add(BuildTeamGroup("Home Team (Us)",   _homeGk, _homeDef, _homeMid, _homeAtk, _homeMorale, _homeTemper), 0, 0);
        topRow.Controls.Add(BuildTeamGroup("Opponent (Away)",  _awayGk, _awayDef, _awayMid, _awayAtk, _awayMorale, _awayTemper), 1, 0);
        topRow.Controls.Add(BuildConditionsGroup(), 2, 0);

        outer.Controls.Add(topRow, 0, 0);

        // ── Button row ────────────────────────────────────────────────────────
        var btnPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            AutoSize      = true,
            Padding       = new Padding(0, 6, 0, 6),
            FlowDirection = FlowDirection.LeftToRight,
        };

        var btnSingle = MakeButton("▶  Simulate Match", Color.FromArgb(0, 120, 215));
        btnSingle.Click += OnSimulateSingle;

        var btnBatch = MakeButton("▶▶  Run", Color.FromArgb(16, 124, 16));
        btnBatch.Click += OnRunBatch;

        var batchLabel1 = new Label { Text = "  times:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };

        var btnClear = MakeButton("Clear", SystemColors.ControlDarkDark);
        btnClear.Click += (_, _) => { _output.Clear(); _statsLabel.Text = string.Empty; };

        btnPanel.Controls.Add(btnSingle);
        btnPanel.Controls.Add(new Label { Width = 16 });
        btnPanel.Controls.Add(btnBatch);
        btnPanel.Controls.Add(_numBatchSize);
        btnPanel.Controls.Add(batchLabel1);
        btnPanel.Controls.Add(new Label { Width = 16 });
        btnPanel.Controls.Add(btnClear);

        outer.Controls.Add(btnPanel, 0, 1);

        // ── Output ────────────────────────────────────────────────────────────
        var outputGroup = new GroupBox { Text = "Output", Dock = DockStyle.Fill, Padding = new Padding(4) };

        _output.Dock      = DockStyle.Fill;
        _output.ReadOnly  = true;
        _output.BackColor = Color.FromArgb(15, 15, 15);
        _output.ForeColor = Color.FromArgb(220, 220, 220);
        _output.Font      = new Font("Consolas", 10f);
        _output.ScrollBars = RichTextBoxScrollBars.Vertical;
        _output.WordWrap  = false;

        outputGroup.Controls.Add(_output);
        outer.Controls.Add(outputGroup, 0, 2);

        // ── Stats bar ────────────────────────────────────────────────────────
        _statsLabel.Dock      = DockStyle.Fill;
        _statsLabel.AutoSize  = false;
        _statsLabel.Height    = 22;
        _statsLabel.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
        _statsLabel.ForeColor = SystemColors.ControlDarkDark;
        outer.Controls.Add(_statsLabel, 0, 3);

        Controls.Add(outer);
    }

    private static GroupBox BuildTeamGroup(
        string label,
        NumericUpDown gk, NumericUpDown def, NumericUpDown mid,
        NumericUpDown atk, NumericUpDown morale, NumericUpDown temper)
    {
        var gb = new GroupBox { Text = label, Dock = DockStyle.Fill, Padding = new Padding(6) };

        var grid = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 6,
            AutoSize    = true,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        for (int i = 0; i < 6; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        AddRow(grid, 0, "GK Skill (0–9):",   gk);
        AddRow(grid, 1, "Defence (0–9):",     def);
        AddRow(grid, 2, "Midfield (0–9):",    mid);
        AddRow(grid, 3, "Attack (0–9):",      atk);
        AddRow(grid, 4, "Morale (0–99):",     morale);
        AddRow(grid, 5, "Temper (0–99):",     temper);

        gb.Controls.Add(grid);
        return gb;
    }

    private GroupBox BuildConditionsGroup()
    {
        var gb = new GroupBox { Text = "Match Conditions", Dock = DockStyle.Fill, Padding = new Padding(6) };

        var grid = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 9,
            AutoSize    = true,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        for (int i = 0; i < 9; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Checkboxes span both columns
        grid.SetColumnSpan(_chkHome,     2);
        grid.SetColumnSpan(_chkLostLast, 2);
        _chkHome.Dock     = DockStyle.Fill;
        _chkLostLast.Dock = DockStyle.Fill;
        grid.Controls.Add(_chkHome,     0, 0);
        grid.Controls.Add(_chkLostLast, 0, 1);

        AddRow(grid, 2, "Lineup changes:",  _numLineupChanges);
        AddRow(grid, 3, "Division (1–4):",  _numDivision);
        AddRow(grid, 4, "Physio skill:",    _numPhysioSkill);

        var sep = new Label
        {
            Text      = "── Cup (prev leg) ──",
            Dock      = DockStyle.Fill,
            ForeColor = SystemColors.GrayText,
            Font      = new Font("Segoe UI", 8f),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        grid.SetColumnSpan(sep, 2);
        grid.Controls.Add(sep, 0, 5);

        AddRow(grid, 6, "Our score:", _numPrevLegUs);
        AddRow(grid, 7, "Their score:", _numPrevLegThem);

        gb.Controls.Add(grid);
        return gb;
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    private void OnSimulateSingle(object? sender, EventArgs e)
    {
        var engine = new MatchEngine(_rng);
        var input  = BuildInput();
        var sim    = engine.SetupMatch(input);

        _output.Clear();
        _statsLabel.Text = string.Empty;

        int physioSkill = (int)_numPhysioSkill.Value;

        AppendLine($"{'=',-52}", Color.DimGray);
        AppendLine($"  MATCH SIMULATION  (length: {sim.MatchLength} min)", Color.White);
        AppendLine($"{'=',-52}", Color.DimGray);
        AppendLine();

        AppendLine($"  Home ratings:  GK:{input.OurGoalkeeperSkill}  " +
                   $"DEF:{input.OurDefence}  MID:{input.OurMid}  ATK:{input.OurAttack}  " +
                   $"Mor:{input.OurMorale}  Tmp:{input.OurTemper}", Color.LightSkyBlue);
        AppendLine($"  Away ratings:  GK:{input.OpponentGoalkeeperSkill}  " +
                   $"DEF:{input.OpponentDefence}  MID:{input.OpponentMid}  ATK:{input.OpponentAttack}  " +
                   $"Mor:{input.OpponentMorale}  Tmp:{input.OpponentTemper}", Color.LightSalmon);

        string venue  = input.IsHomeGame     ? "Home game"     : "Away game";
        string recent = input.LostLastMatch  ? "  Lost last"   : string.Empty;
        AppendLine($"  {venue}{recent}  |  Div {input.Division}  |  " +
                   $"Lineup changes: {input.LineupChanges}  |  Physio: {physioSkill}%");
        AppendLine();
        AppendLine($"  {'─',50}", Color.DimGray);

        // Play-by-play
        var events = sim.GoalEvents.OrderBy(g => g.Minute).ToList();
        int homeScore = 0, awayScore = 0;

        int incidentMinute = sim.IncidentMinute;
        bool incidentShown = false;

        foreach (var g in events)
        {
            // Show incident inline if it falls before this goal
            if (incidentMinute > 0 && !incidentShown && incidentMinute < g.Minute)
            {
                ShowIncident(engine, input, incidentMinute, physioSkill);
                incidentShown = true;
            }

            if (g.Scorer == 1)
            {
                homeScore++;
                AppendLine($"  {g.Minute,2}'  GOAL  ► Home  {homeScore}–{awayScore}", Color.LightGreen);
            }
            else
            {
                awayScore++;
                AppendLine($"  {g.Minute,2}'  GOAL  ► Away  {homeScore}–{awayScore}", Color.Tomato);
            }
        }

        // Incident after last goal
        if (incidentMinute > 0 && !incidentShown)
        {
            ShowIncident(engine, input, incidentMinute, physioSkill);
        }

        AppendLine();
        AppendLine($"  {'─',50}", Color.DimGray);
        string result    = sim.OurGoalCount > sim.OpponentGoalCount ? "HOME WIN"
                         : sim.OurGoalCount < sim.OpponentGoalCount ? "AWAY WIN" : "DRAW";
        Color  resultCol = result == "HOME WIN" ? Color.LightGreen
                         : result == "AWAY WIN" ? Color.Tomato : Color.Yellow;
        AppendLine($"  FULL TIME:  Home {sim.OurGoalCount} – {sim.OpponentGoalCount} Away  " +
                   $"[{result}]", resultCol);
    }

    private void ShowIncident(MatchEngine engine, MatchSetupInput input, int minute, int physioSkill)
    {
        var squad = BuildDummySquad();
        var inc   = engine.ResolveIncident(
            squad, incidentBeforeMinute81: minute < 81, hasSubstituted: false, physioSkillPercent: physioSkill);

        if (inc == null) return;

        string detail = inc.Type == IncidentType.Injury
            ? $"INJURED — {inc.WeeksOut} wk out  (physio: {physioSkill}%)"
            : "RED CARD";
        AppendLine($"  {minute,2}'  *** {inc.PlayerName.TrimEnd()} — {detail} ***", Color.Orange);
    }

    private void OnRunBatch(object? sender, EventArgs e)
    {
        int count = (int)_numBatchSize.Value;
        int homeWins = 0, draws = 0, awayWins = 0;
        int totalHomeGoals = 0, totalAwayGoals = 0;
        int totalIncidents = 0;
        var scorelines = new Dictionary<string, int>();

        for (int i = 0; i < count; i++)
        {
            var engine = new MatchEngine();
            var sim    = engine.SetupMatch(BuildInput());

            if      (sim.OurGoalCount > sim.OpponentGoalCount) homeWins++;
            else if (sim.OurGoalCount < sim.OpponentGoalCount) awayWins++;
            else                                               draws++;

            totalHomeGoals += sim.OurGoalCount;
            totalAwayGoals += sim.OpponentGoalCount;
            if (sim.IncidentMinute > 0) totalIncidents++;

            string key = $"{sim.OurGoalCount}–{sim.OpponentGoalCount}";
            scorelines[key] = scorelines.GetValueOrDefault(key) + 1;
        }

        double homeWinPct = 100.0 * homeWins / count;
        double drawPct    = 100.0 * draws    / count;
        double awayWinPct = 100.0 * awayWins / count;

        _output.Clear();
        AppendLine($"{'=',-52}", Color.DimGray);
        AppendLine($"  BATCH: {count} SIMULATIONS", Color.White);
        AppendLine($"{'=',-52}", Color.DimGray);
        AppendLine();
        AppendLine($"  Home wins:  {homeWins,5}  ({homeWinPct:F1}%)", Color.LightGreen);
        AppendLine($"  Draws:      {draws,5}  ({drawPct:F1}%)");
        AppendLine($"  Away wins:  {awayWins,5}  ({awayWinPct:F1}%)", Color.Tomato);
        AppendLine();
        AppendLine($"  Avg goals — Home: {(double)totalHomeGoals / count:F2}   " +
                   $"Away: {(double)totalAwayGoals / count:F2}");
        AppendLine($"  Avg total goals:  {(double)(totalHomeGoals + totalAwayGoals) / count:F2}");
        AppendLine($"  Incidents fired:  {totalIncidents} ({100.0 * totalIncidents / count:F1}%)");

        AppendLine();
        AppendLine($"  Top scorelines:", Color.LightYellow);
        foreach (var (line, freq) in scorelines.OrderByDescending(kv => kv.Value).Take(10))
            AppendLine($"    {line,-8}  {freq,5} x  ({100.0 * freq / count:F1}%)");

        _statsLabel.Text =
            $"W {homeWins} ({homeWinPct:F0}%)  D {draws} ({drawPct:F0}%)  " +
            $"L {awayWins} ({awayWinPct:F0}%)  |  " +
            $"Avg goals: {(double)totalHomeGoals/count:F2} – {(double)totalAwayGoals/count:F2}";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private MatchSetupInput BuildInput() => new()
    {
        OurGoalkeeperSkill      = (int)_homeGk.Value,
        OurDefence              = (int)_homeDef.Value,
        OurMid                  = (int)_homeMid.Value,
        OurAttack               = (int)_homeAtk.Value,
        OurMorale               = (int)_homeMorale.Value,
        OurTemper               = (int)_homeTemper.Value,
        OpponentGoalkeeperSkill = (int)_awayGk.Value,
        OpponentDefence         = (int)_awayDef.Value,
        OpponentMid             = (int)_awayMid.Value,
        OpponentAttack          = (int)_awayAtk.Value,
        OpponentMorale          = (int)_awayMorale.Value,
        OpponentTemper          = (int)_awayTemper.Value,
        IsHomeGame              = _chkHome.Checked,
        LostLastMatch           = _chkLostLast.Checked,
        LineupChanges           = (int)_numLineupChanges.Value,
        Division                = (int)_numDivision.Value,
        PreviousLegOurScore     = (int)_numPrevLegUs.Value,
        PreviousLegTheirScore   = (int)_numPrevLegThem.Value,
    };

    /// <summary>
    /// Builds a minimal 12-player squad for incident resolution.
    /// Players are named P1–P11 plus a sub; positions mirror the standard 4-4-2.
    /// </summary>
    private static Player?[] BuildDummySquad()
    {
        var squad = new Player?[29];

        var positions = new[]
        {
            PlayerPosition.Goalkeeper,
            PlayerPosition.Defender, PlayerPosition.Defender,
            PlayerPosition.Defender, PlayerPosition.Defender,
            PlayerPosition.Midfielder, PlayerPosition.Midfielder,
            PlayerPosition.Midfielder, PlayerPosition.Midfielder,
            PlayerPosition.Attacker, PlayerPosition.Attacker,
            PlayerPosition.Attacker,   // sub
        };

        for (int i = 1; i <= 12; i++)
        {
            squad[i] = new Player
            {
                Name     = $"Player{i:D2}",
                Position = positions[i - 1],
                Skill    = 5.0,
                Temper   = 3,
            };
        }

        return squad;
    }

    private void AppendLine(string text = "", Color color = default)
    {
        _output.SuspendLayout();
        _output.SelectionStart  = _output.TextLength;
        _output.SelectionLength = 0;
        _output.SelectionColor  = color == default ? _output.ForeColor : color;
        _output.AppendText(text + Environment.NewLine);
        _output.SelectionColor  = _output.ForeColor;
        _output.ResumeLayout();
    }

    private static void AddRow(TableLayoutPanel grid, int row, string labelText, Control control)
    {
        var lbl = new Label
        {
            Text      = labelText,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        control.Dock = DockStyle.Fill;
        grid.Controls.Add(lbl,     0, row);
        grid.Controls.Add(control, 1, row);
    }

    private static NumericUpDown NumSpin(decimal min, decimal max, decimal value) => new()
    {
        Minimum = min,
        Maximum = max,
        Value   = value,
    };

    private static Button MakeButton(string text, Color backColor) => new()
    {
        Text      = text,
        BackColor = backColor,
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        AutoSize  = true,
        Padding   = new Padding(6, 2, 6, 2),
        Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
    };

    private void SetToolTips()
    {
        var tips = new ToolTip();

        tips.SetToolTip(_homeGk,     "Goalkeeper integer skill (BA). Range 0–9. Derived from GK's H(I)/10.");
        tips.SetToolTip(_homeDef,    "Defence rating (bc). Sum of defender skills ÷ divisor, capped 0–9.");
        tips.SetToolTip(_homeMid,    "Midfield rating (bb). Min(mid, atk) used for shot count.");
        tips.SetToolTip(_homeAtk,    "Attack rating (bd). Controls shot count: Min(mid,atk) − opp GK/2.");
        tips.SetToolTip(_homeMorale, "Team morale (me). >80 = chance of +1 shot. <30 = chance of −1 shot.");
        tips.SetToolTip(_homeTemper, "Team temper (pu). Combined temper drives incident probability.");

        tips.SetToolTip(_awayGk,     "Opponent GK skill. Reduces home shots that become goals.");
        tips.SetToolTip(_awayDef,    "Opponent defence (ej). Reduces opponent shot count.");
        tips.SetToolTip(_awayMid,    "Opponent midfield (ek).");
        tips.SetToolTip(_awayAtk,    "Opponent attack (el).");
        tips.SetToolTip(_awayMorale, "Opponent morale (mm).");
        tips.SetToolTip(_awayTemper, "Opponent temper (pv).");

        tips.SetToolTip(_chkHome,          "Home advantage adds +1 to our shot count (BK%=1 in BASIC).");
        tips.SetToolTip(_chkLostLast,      "Losing last match subtracts 1 from our shot count (aa=1).");
        tips.SetToolTip(_numLineupChanges, "Positional moves this week (bj). >3 changes = −1 shot.");
        tips.SetToolTip(_numDivision,      "Division (1–4). Used as divisionBonus on opponent goal rolls.");
        tips.SetToolTip(_numPhysioSkill,   "Physio skill % (0=no physio). Reduces match injury duration.\nFormula: RA=INT(60/100×skill); reduction=INT(weeks/100×RA).");
        tips.SetToolTip(_numPrevLegUs,     "Goals from the first leg (cup ties). Subtracted from final count.");
        tips.SetToolTip(_numPrevLegThem,   "Opponent goals from first leg.");
        tips.SetToolTip(_numBatchSize,     "Number of simulations to run for statistical output.");
    }
}

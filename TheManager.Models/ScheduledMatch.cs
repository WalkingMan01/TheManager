namespace TheManager.Models
{
    public class ScheduledMatch
    {
        public MatchType MatchType { get; set; }

        /// <summary>Matchday index (1–54). Property name kept as Week for save compatibility.</summary>
        public int Week { get; set; }

        public string OpponentName { get; set; } = string.Empty;
        public int OpponentTeamIndex { get; set; }
        public bool IsHomeGame { get; set; }
        public OpponentRatings? OpponentRatings { get; set; }

        // ── Result (null until played) ─────────────────────────────────────────

        /// <summary>Our score after the match has been played. Null if not yet played.</summary>
        public int? OurScore { get; set; }

        /// <summary>Opponent score after the match has been played. Null if not yet played.</summary>
        public int? TheirScore { get; set; }

        /// <summary>True when this cup tie was level and won/lost on penalties.</summary>
        public bool WonOnPenalties { get; set; }

        /// <summary>Our shootout tally. Null unless the tie went to penalties.</summary>
        public int? OurPenalties { get; set; }

        /// <summary>Opponent shootout tally. Null unless the tie went to penalties.</summary>
        public int? TheirPenalties { get; set; }

        public bool WasPlayed => OurScore.HasValue;
    }
}

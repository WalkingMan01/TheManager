namespace TheManager.Models
{
    public class ScheduledMatch
    {
        public MatchType MatchType { get; set; }
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

        public bool WasPlayed => OurScore.HasValue;
    }
}

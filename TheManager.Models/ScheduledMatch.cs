namespace TheManager.Models
{
    public class ScheduledMatch
    {
        public MatchType MatchType { get; set; }
        public int Week { get; set; }
        public string OpponentName { get; set; } = string.Empty;
        public int OpponentTeamIndex { get; set; }
        public bool IsHomeGame { get; set; }
    }
}

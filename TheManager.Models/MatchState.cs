using System;
using System.Collections.Generic;
using System.Text;

namespace TheManager.Models
{
    /// <summary>
    /// Transient state for a match currently in progress.
    /// Populated from BASIC variables at the start of the PLAYMATCH section.
    /// </summary>
    public class MatchState
    {
        public string OurTeamName { get; set; } = string.Empty;
        public string OpponentTeamName { get; set; } = string.Empty;

        /// <summary>True = we are at home. Corresponds to BK%=1.</summary>
        public bool IsHomeGame { get; set; }

        public int OurScore { get; set; }  // R or S depending on BK%
        public int OpponentScore { get; set; }

        /// <summary>Current match minute (1–90+). Corresponds to N.</summary>
        public int CurrentMinute { get; set; }

        /// <summary>Maximum match minutes (90–93). Corresponds to md.</summary>
        public int MaxMinutes { get; set; }

        /// <summary>True once the substitute (player 12) has been used. Corresponds to MP.</summary>
        public bool HasSubstituted { get; set; }

        /// <summary>Attendance for this match. Corresponds to dn.</summary>
        public double Attendance { get; set; }

        /// <summary>
        /// Scheduled goal minutes for up to 8 goals (0 = not used).
        /// Corresponds to B(8).
        /// </summary>
        public int[] GoalMinutes { get; set; } = new int[8];

        /// <summary>
        /// Who scored each goal: 1 = us, 2 = opponent.
        /// Corresponds to c(8).
        /// </summary>
        public int[] GoalScorer { get; set; } = new int[8];

        /// <summary>
        /// Minute at which a crowd incident / booking event fires.
        /// Corresponds to BU (0 = none this match).
        /// </summary>
        public int IncidentMinute { get; set; }
    }

}

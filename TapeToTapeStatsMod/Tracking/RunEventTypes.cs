namespace TapeToTapeStatsMod.Tracking;

/// <summary>
/// Event types recorded during a run. The web app labels these, so keep them stable.
/// </summary>
public static class RunEventTypes
{
    /// <summary>The player picked the next node on the map.</summary>
    public const string NodeSelected = "node_selected";

    /// <summary>A match finished (from the game's own end-of-game analytics event).</summary>
    public const string MatchEnded = "match_ended";

    /// <summary>Post-match talent/ability/bench power assigned to a skater.</summary>
    public const string PowerupRewardApplied = "powerup_reward_applied";

    /// <summary>Post-match or elite relic chosen.</summary>
    public const string RelicRewardApplied = "relic_reward_applied";

    /// <summary>Post-match reward picked or skipped, as the game's analytics reports it.</summary>
    public const string GameRewardSelected = "game_reward_selected";

    /// <summary>A choice made at an event node (nurse, coach, general manager, training, chaos, ...).</summary>
    public const string EventChoiceMade = "event_choice_made";

    /// <summary>The player left an event node without choosing anything.</summary>
    public const string EventChoiceSkipped = "event_choice_skipped";

    /// <summary>An event node reward chosen or skipped, as the game's analytics reports it.</summary>
    public const string EventRewardSelected = "event_reward_selected";

    /// <summary>The player rerolled the choices on a reward or event screen.</summary>
    public const string ChoicesRerolled = "choices_rerolled";

    /// <summary>Something unlocked (squad, relic, talent, benchwarmer, ...).</summary>
    public const string UnlockEarned = "unlock_earned";

    /// <summary>The run ended (from the game's analytics).</summary>
    public const string RunCompleted = "run_completed";
}

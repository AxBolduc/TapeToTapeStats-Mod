using Il2CppData;

namespace TapeToTapeMods.Domain.DTOs;

[Serializable]
public record RunInfoDto
{
    public RunInfoDto(string RunId,
        int DifficultyLevel,
        int CurrentAct,
        int CurrentFloor,
        bool IsVictory,
        RunTeamDataDto PlayerTeamData,
        List<RunMatchScoreDto> MatchScores)
    {
        this.RunId = RunId;
        this.DifficultyLevel = DifficultyLevel;
        this.CurrentAct = CurrentAct;
        this.CurrentFloor = CurrentFloor;
        this.IsVictory = IsVictory;
        this.PlayerTeamData = PlayerTeamData;
        this.MatchScores = MatchScores;
    }

    public RunInfoDto()
    {
    }

    public string RunId { get; init; }
    public int DifficultyLevel { get; init; }
    public int CurrentAct { get; init; }
    public int CurrentFloor { get; init; }
    public bool IsVictory { get; init; }
    public RunTeamDataDto PlayerTeamData { get; init; }
    public List<RunMatchScoreDto> MatchScores { get; init; }
}
using Il2Cpp;

namespace TapeToTapeMods.Domain.DTOs;

public record RunMatchScoreDto
{
        public RunMatchScoreDto(int teamScore, int opponentScore, bool isShootout, bool isBoss, int numAct, string opponentId)
        {
                TeamScore = teamScore;
                OpponentScore = opponentScore;
                IsShootout = isShootout;
                IsBoss = isBoss;
                NumAct = numAct;
                OpponentId = opponentId;
        }

        public RunMatchScoreDto(MatchScore matchScore)
        {
            IsBoss = matchScore.IsBoss;
            IsShootout = matchScore.Shootout;
            NumAct = matchScore.act;
            OpponentScore = matchScore.OtherScore;
            TeamScore = matchScore.SelfScore;
            OpponentId = matchScore.teamId;
        }

        public RunMatchScoreDto()
        {
        }

        public int TeamScore { get; init; }
        public int OpponentScore { get; init; }
        public bool IsShootout { get; init; }
        public bool IsBoss { get; init; }
        public int NumAct { get; init; }
        public string OpponentId { get; init; }
}
using Il2CppData;
using Il2CppRogue;

namespace TapeToTapeMods.Domain.DTOs;

public record RunSkaterInfoDto
{
    public RunSkaterInfoDto(string FirstName, string LastName, int Number, List<TalentDto> Powerups, int ShotAccuracy, int ShotPower, int Speed, int SkaterSize)
    {
        this.FirstName = FirstName;
        this.LastName = LastName;
        this.Number = Number;
        this.Powerups = Powerups;
        this.ShotAccuracy = ShotAccuracy;
        this.ShotPower = ShotPower;
        this.Speed = Speed;
        this.SkaterSize = SkaterSize;
    }

    public RunSkaterInfoDto(ForwardData forwardData)
    {
        this.FirstName = forwardData.firstName;
        this.LastName = forwardData.lastName;
        this.Number = forwardData.number;
        this.ShotAccuracy = forwardData.shotAccuracy;
        this.ShotPower = forwardData.shotPower;
        this.Speed = forwardData.speed;
        this.SkaterSize = (int)forwardData.skaterSize;

        this.Powerups = forwardData.powerups.ToArray().Select(powerUp => new TalentDto(powerUp)).ToList();
    }

    public RunSkaterInfoDto()
    {
    }

    public string FirstName { get; init; }
    public string LastName { get; init; }
    public int Number { get; init; }
    public List<TalentDto> Powerups { get; init; }
    public int ShotAccuracy { get; init; }
    public int ShotPower { get; init; }
    public int Speed { get; init; }
    public int SkaterSize { get; init; }
}
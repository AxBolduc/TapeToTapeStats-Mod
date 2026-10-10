using Il2CppRogue;

namespace TapeToTapeMods.Domain.DTOs;

public record TalentDto
{
    public TalentDto(string Id, string Name, int Level, int Rarity)
    {
        this.Id = Id;
        this.Name = Name;
        this.Level = Level;
        this.Rarity = Rarity;
    }

    public TalentDto(Talent talent)
    {
        this.Id = talent.id;
        this.Name = talent.localizedPowerupName;
        this.Level = talent.level;
        this.Rarity = (int)talent.rarity;
    }

    public TalentDto()
    {
    }

    public string Id { get; init; }
    public string Name { get; init; }
    public int Level { get; init; }
    public int Rarity { get; init; }
}
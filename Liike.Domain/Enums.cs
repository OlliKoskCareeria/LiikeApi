namespace Liike.Domain;

public enum AccountRole
{
    Organizer = 1,
    Admin = 2
}

public enum OrganizerType
{
    Unspecified = 0,
    Student = 1,
    Company = 2,
    StudentOrganization = 3,
    Municipality = 4
}

public enum EventTag
{
    Unspecified = 0,
    BeginnerFriendly = 1,
    Indoor = 2,
    Outdoor = 3,
    TeamSport = 4,
    IndividualSport = 5,
    OlympicSport = 6,
    StudentEvent = 7,
    FreeTrial = 8,
    EquipmentProvided = 9
}

public enum SportCategory
{
    Unspecified = 0,
    BallSports = 1,
    RacketSports = 2,
    Running = 3,
    Gym = 4,
    CombatSports = 5,
    Dance = 6,
    WaterSports = 7,
    WinterSports = 8,
    OutdoorActivities = 9,
    Other = 10
}

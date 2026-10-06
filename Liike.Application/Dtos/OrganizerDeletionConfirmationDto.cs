namespace Liike.Application.Dtos;

public sealed class OrganizerDeletionConfirmationDto
{
    public int EventCount { get; init; }

    public required string Message { get; init; }

    public bool ConfirmationRequired { get; init; }
}
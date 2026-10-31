using System.ComponentModel.DataAnnotations;

namespace ChasingLight.Api.DTOs.Records;

public record CompleteStageRequest()
{
    [Required] public Guid StageId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Time done must be greater than 0")]
    public int TimeDoneSeconds { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Step count must be greater than 0")]
    public int StepCount { get; init; }

    [Range(1, 3, ErrorMessage = "Stars earned must be between 1 and 3")]
    public short StarsEarned { get; init; }
}
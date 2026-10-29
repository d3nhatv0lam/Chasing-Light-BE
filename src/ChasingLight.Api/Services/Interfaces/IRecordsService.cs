using ChasingLight.Api.DTOs.Records;

namespace ChasingLight.Api.Services.Interfaces;

public interface IRecordsService
{
    Task<CompleteStageResponse> CompleteStageAsync(
        Guid userId, 
        CompleteStageRequest request, 
        CancellationToken cancellationToken = default);
}
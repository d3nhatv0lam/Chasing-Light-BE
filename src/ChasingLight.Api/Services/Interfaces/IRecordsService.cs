using ChasingLight.Api.DTOs.Records;

namespace ChasingLight.Api.Services.Interfaces;

public interface IRecordsService
{
    Task<CompleteStageResponse> CompleteStageAsync(
        Guid userId, 
        CompleteStageRequest request, 
        CancellationToken cancellationToken = default);

    Task<UserRecordsSummaryResponse> GetUserRecordsAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    Task<SyncRecordsResponse> SyncRecordsAsync(
        Guid userId, 
        SyncRecordsRequest request, 
        CancellationToken cancellationToken = default);
}

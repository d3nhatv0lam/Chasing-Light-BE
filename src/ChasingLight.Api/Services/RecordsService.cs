using ChasingLight.Api.DTOs.Records;
using ChasingLight.Api.Infrastructures.Data;
using ChasingLight.Api.Services.Interfaces;

namespace ChasingLight.Api.Services;

public class RecordsService : IRecordsService
{
    private readonly ChasingLightDbContext _db;
    private readonly ILogger<RecordsService> _logger;

    public RecordsService(ChasingLightDbContext db, ILogger<RecordsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<CompleteStageResponse> CompleteStageAsync(Guid userId, CompleteStageRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
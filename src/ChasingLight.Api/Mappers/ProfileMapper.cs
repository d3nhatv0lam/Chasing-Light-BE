using ChasingLight.Api.DTOs.Profiles;
using ChasingLight.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace ChasingLight.Api.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ProfileMapper
{
    public static partial UserProfileResponse ToResponse(this Profile profile);
}

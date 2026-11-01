using NTA.Api.Contracts;
using NTA.Domain.Models;

namespace NTA.Api.Management.Mappings;

internal static class ManagementContractMapper
{
    public static UserResponse ToContract(User user) => new(user.Id.Value, user.Name);

    public static TeamResponse ToContract(Team team) =>
        new(team.Id.Value, team.Name, team.Members.Select(member => member.Value).Order().ToArray());
}

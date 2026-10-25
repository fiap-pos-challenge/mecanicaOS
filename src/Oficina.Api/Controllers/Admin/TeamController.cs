using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Team;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Team.Commands.UpdateRolePermissions;
using Oficina.Application.Team.Commands.UpdateTeamProfile;
using Oficina.Application.Team.Dtos;
using Oficina.Application.Team.Queries.GetProductivity;
using Oficina.Application.Team.Queries.GetTeamProfile;
using Oficina.Application.Team.Queries.ListRoles;
using Oficina.Application.Team.Queries.ListTeamProfiles;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin")]
[Tags("Team")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class TeamController(ISender sender) : ApiControllerBase
{
    [HttpPost("team/profiles/query")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("List team profiles")]
    [EndpointDescription("Returns specialties, commission, discount limit and weekly schedule for each user id in the body (up to 500). Users without a profile get an empty one. Roles: Admin, Manager.")]
    [ProducesResponseType<TeamProfileListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Profiles(TeamProfilesQueryRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ListTeamProfilesQuery(request.UserIds ?? []), cancellationToken), Ok);

    [HttpGet("team/{userId:guid}/profile")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Get team profile")]
    [EndpointDescription("Returns the professional profile of one user, or an empty profile when none was saved. Roles: Admin, Manager.")]
    [ProducesResponseType<TeamProfileDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Profile(Guid userId, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetTeamProfileQuery(userId), cancellationToken), Ok);

    [HttpPut("team/{userId:guid}/profile")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Update team profile")]
    [EndpointDescription("Saves up to 8 specialties (2 to 40 characters), commission from 0 to 50 percent, discount limit from 0 to 30 percent (always 0 for mechanics: 422 team.mechanicDiscount) and one shift per weekday in HH:mm with start before end. Role: Admin.")]
    [ProducesResponseType<TeamProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> UpdateProfile(Guid userId, UpdateTeamProfileRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new UpdateTeamProfileCommand(userId, request.Specialties, request.CommissionPercent, request.DiscountLimitPercent, request.Schedule), cancellationToken), Ok);

    [HttpPost("team/productivity")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Team productivity")]
    [EndpointDescription("Returns, for each user id in the body and the last 30 or 90 days, the service orders worked, hours recorded on the timer, occupancy against the weekly schedule (44 hours when none), revenue of those orders, rework percent and the orders per week. Roles: Admin, Manager.")]
    [ProducesResponseType<ProductivityListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Productivity(ProductivityRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetProductivityQuery(request.UserIds ?? [], request.PeriodDays), cancellationToken), Ok);

    [HttpGet("roles")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("List role permissions")]
    [EndpointDescription("Returns the permissions of each role. Roles are fixed in this version (customizable false). Roles: Admin, Manager.")]
    [ProducesResponseType<RoleListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Roles(CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ListRolesQuery(), cancellationToken), Ok);

    [HttpPut("roles/{role}/permissions")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Update role permissions")]
    [EndpointDescription("Role permissions are fixed in this version: always 422 roles.customizationUnavailable. Role: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> UpdateRole(string role, UpdateRolePermissionsRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new UpdateRolePermissionsCommand(role, request.Permissions ?? []), cancellationToken));
}

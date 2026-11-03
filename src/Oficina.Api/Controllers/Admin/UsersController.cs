using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.Contracts.Users;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Users.Commands.CreateUser;
using Oficina.Application.Users.Commands.DeactivateUser;
using Oficina.Application.Users.Commands.DeleteUser;
using Oficina.Application.Users.Commands.ReactivateUser;
using Oficina.Application.Users.Commands.UpdateUser;
using Oficina.Application.Users.Dtos;
using Oficina.Application.Users.Queries.GetUserById;
using Oficina.Application.Users.Queries.ListUsers;
using Oficina.Application.Users.Queries.SearchUserByEmail;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/users")]
[Tags("Usuários")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class UsersController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Listar usuários")]
    [EndpointDescription("Retorna uma página de usuários ordenados por nome. Perfil: Admin.")]
    [ProducesResponseType<PagedResult<UserDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> List(
        [FromQuery] int page = PageRequest.FirstPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await sender.Send(new ListUsersQuery(page, pageSize), cancellationToken), Ok);

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Cadastrar usuário")]
    [EndpointDescription("Cadastra um usuário com qualquer perfil (Admin, Attendant ou Mechanic), e-mail único e senha forte. Perfil: Admin.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(request.Name, request.Email, request.Password, request.Role);
        return Respond(await sender.Send(command, cancellationToken), user => CreatedAtAction(nameof(GetById), new { id = user.Id }, user));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Consultar usuário")]
    [EndpointDescription("Retorna um usuário pelo id. Perfil: Admin.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new GetUserByIdQuery(id), cancellationToken), Ok);

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Atualizar usuário")]
    [EndpointDescription("Altera nome e perfil. Um Admin não pode rebaixar a si mesmo, e o último Admin ativo não pode ser rebaixado. Perfil: Admin.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new UpdateUserCommand(id, request.Name, request.Role), cancellationToken), Ok);

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Excluir usuário")]
    [EndpointDescription("Desativa e exclui logicamente um usuário. Um Admin não pode excluir a si mesmo nem o último Admin ativo. Perfil: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeleteUserCommand(id), cancellationToken));

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Desativar usuário")]
    [EndpointDescription("Impede o usuário de fazer login. Um Admin não pode desativar a si mesmo nem o último Admin ativo. Perfil: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, ProblemResponses.ContentType)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new DeactivateUserCommand(id), cancellationToken));

    [HttpPost("{id:guid}/reactivate")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Reativar usuário")]
    [EndpointDescription("Permite que um usuário inativo volte a fazer login. Perfil: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, ProblemResponses.ContentType)]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new ReactivateUserCommand(id), cancellationToken));

    [HttpPost("search")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Buscar usuário por e-mail")]
    [EndpointDescription("Busca um usuário pelo e-mail enviado no corpo da requisição (nunca na URL). Perfil: Admin.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> Search(SearchUserRequest request, CancellationToken cancellationToken) =>
        Respond(await sender.Send(new SearchUserByEmailQuery(request.Email), cancellationToken), Ok);
}

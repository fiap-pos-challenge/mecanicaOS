using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oficina.Api.Authorization;
using Oficina.Api.ExceptionHandling;
using Oficina.Application.Common.Pagination;
using Oficina.Application.Settings;
using Oficina.Application.Settings.Dtos;
using Oficina.Application.Settings.Models;

namespace Oficina.Api.Controllers.Admin;

[Route("api/admin/settings")]
[Tags("Settings")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, ProblemResponses.ContentType)]
public sealed class SettingsController(SettingsService settings) : ApiControllerBase
{
    [HttpGet("workshop")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Get workshop")]
    [EndpointDescription("Returns name, CNPJ, address, logo, business hours and plan of the shop. Roles: Admin, Manager.")]
    [ProducesResponseType<WorkshopSettings>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Workshop(CancellationToken cancellationToken) =>
        Respond(await settings.WorkshopAsync(cancellationToken), Ok);

    [HttpPut("workshop")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Save workshop")]
    [EndpointDescription("Saves name, CNPJ, address and business hours. Requires a fresh X-Reauthentication token (403 settings.reauthenticationRequired). Roles: Admin.")]
    [ProducesResponseType<WorkshopSettings>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> SaveWorkshop([FromHeader(Name = SessionCookies.ReauthenticationHeader)] string? reauthentication, WorkshopInput request, CancellationToken cancellationToken) =>
        Respond(await settings.SaveWorkshopAsync(reauthentication, request, cancellationToken), Ok);

    [HttpPost("workshop/logo")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Upload logo")]
    [EndpointDescription("Saves the shop logo sent as a PNG or JPG data URL up to 512 KB. Requires a fresh X-Reauthentication token (403 settings.reauthenticationRequired). Roles: Admin.")]
    [ProducesResponseType<LogoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Logo([FromHeader(Name = SessionCookies.ReauthenticationHeader)] string? reauthentication, LogoInput request, CancellationToken cancellationToken) =>
        Respond(await settings.SaveLogoAsync(reauthentication, request, cancellationToken), Ok);

    [HttpGet("discounts")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Get discount rules")]
    [EndpointDescription("Returns the automatic discount rules and the discount limit of each role. Roles: Admin, Manager.")]
    [ProducesResponseType<DiscountSettings>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Discounts(CancellationToken cancellationToken) =>
        Respond(await settings.DiscountsAsync(cancellationToken), Ok);

    [HttpPut("discounts")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Save discount rules")]
    [EndpointDescription("Replaces the discount rules and role limits. Requires a fresh X-Reauthentication token (403 settings.reauthenticationRequired). Roles: Admin.")]
    [ProducesResponseType<DiscountSettings>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> SaveDiscounts([FromHeader(Name = SessionCookies.ReauthenticationHeader)] string? reauthentication, DiscountSettings request, CancellationToken cancellationToken) =>
        Respond(await settings.SaveDiscountsAsync(reauthentication, request, cancellationToken), Ok);

    [HttpGet("payments")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Get payment settings")]
    [EndpointDescription("Returns accepted methods, installments, interest, fees and the Pix key. Roles: Admin, Manager.")]
    [ProducesResponseType<PaymentSettings>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Payments(CancellationToken cancellationToken) =>
        Respond(await settings.PaymentsAsync(cancellationToken), Ok);

    [HttpPut("payments")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Save payment settings")]
    [EndpointDescription("Saves methods, installments, interest, fees and the Pix key (null keeps the current key). Requires a fresh X-Reauthentication token (403 settings.reauthenticationRequired). Roles: Admin.")]
    [ProducesResponseType<PaymentSettings>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> SavePayments([FromHeader(Name = SessionCookies.ReauthenticationHeader)] string? reauthentication, PaymentInput request, CancellationToken cancellationToken) =>
        Respond(await settings.SavePaymentsAsync(reauthentication, request, cancellationToken), Ok);

    [HttpGet("messages")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Get message templates")]
    [EndpointDescription("Returns the message templates and the send window. Roles: Admin, Manager.")]
    [ProducesResponseType<MessageSettings>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Messages(CancellationToken cancellationToken) =>
        Respond(await settings.MessagesAsync(cancellationToken), Ok);

    [HttpPut("messages")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Save message templates")]
    [EndpointDescription("Saves the templates (only known variables) and the send window. Requires a fresh X-Reauthentication token (403 settings.reauthenticationRequired). Roles: Admin.")]
    [ProducesResponseType<MessageSettings>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> SaveMessages([FromHeader(Name = SessionCookies.ReauthenticationHeader)] string? reauthentication, MessageSettings request, CancellationToken cancellationToken) =>
        Respond(await settings.SaveMessagesAsync(reauthentication, request, cancellationToken), Ok);

    [HttpGet("security")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Get security policy")]
    [EndpointDescription("Returns the two-factor policy, idle timeout and lockout rules. Roles: Admin.")]
    [ProducesResponseType<SecuritySettings>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Security(CancellationToken cancellationToken) =>
        Respond(await settings.SecurityAsync(cancellationToken), Ok);

    [HttpPut("security")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Save security policy")]
    [EndpointDescription("Saves the two-factor policy, idle timeout (5-60 min) and lockout rules. Requires a fresh X-Reauthentication token (403 settings.reauthenticationRequired). Roles: Admin.")]
    [ProducesResponseType<SecuritySettings>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> SaveSecurity([FromHeader(Name = SessionCookies.ReauthenticationHeader)] string? reauthentication, SecuritySettings request, CancellationToken cancellationToken) =>
        Respond(await settings.SaveSecurityAsync(reauthentication, request, cancellationToken), Ok);

    [HttpGet("sessions")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Active sessions")]
    [EndpointDescription("Returns the active sessions of every user with device and last activity; the caller's session is marked current. Roles: Admin.")]
    [ProducesResponseType<SessionListDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Sessions(CancellationToken cancellationToken) =>
        Respond(await settings.SessionsAsync(cancellationToken), Ok);

    [HttpDelete("sessions/{id:guid}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("End session")]
    [EndpointDescription("Ends another active session; its refresh token stops working. The current session cannot be ended here (404). Requires a fresh X-Reauthentication token (403 settings.reauthenticationRequired). Roles: Admin.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemResponses.ContentType)]
    public async Task<IActionResult> EndSession([FromHeader(Name = SessionCookies.ReauthenticationHeader)] string? reauthentication, Guid id, CancellationToken cancellationToken) =>
        Respond(await settings.RevokeSessionAsync(reauthentication, id, cancellationToken));

    [HttpPost("audit/search")]
    [Authorize(Policy = Policies.AdminOnly)]
    [EndpointSummary("Search audit log")]
    [EndpointDescription("Searches settings changes, discount approvals, revealed personal data and stock adjustments by area, period and actor, newest first. Roles: Admin.")]
    [ProducesResponseType<PagedResult<AuditEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, ProblemResponses.ContentType)]
    public async Task<IActionResult> Audit(AuditFilter request, CancellationToken cancellationToken) =>
        Respond(await settings.AuditAsync(request, cancellationToken), Ok);

    [HttpGet("fiscal")]
    [Authorize(Policy = Policies.AdminOrManager)]
    [EndpointSummary("Fiscal integrations")]
    [EndpointDescription("Returns the status of NFS-e, NF-e and NFC-e integrations. Roles: Admin, Manager.")]
    [ProducesResponseType<FiscalSettings>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Fiscal(CancellationToken cancellationToken) =>
        Respond(await settings.FiscalAsync(cancellationToken), Ok);
}

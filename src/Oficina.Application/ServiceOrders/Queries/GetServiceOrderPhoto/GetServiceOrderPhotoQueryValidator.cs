using FluentValidation;
using Oficina.Domain.ServiceOrders.Errors;

namespace Oficina.Application.ServiceOrders.Queries.GetServiceOrderPhoto;

public sealed class GetServiceOrderPhotoQueryValidator : AbstractValidator<GetServiceOrderPhotoQuery>
{
    public GetServiceOrderPhotoQueryValidator()
    {
        RuleFor(query => query.ServiceOrderId).NotEmpty().WithErrorCode(ServiceOrderErrors.NotFound.Code).WithMessage(ServiceOrderErrors.NotFound.Message);
        RuleFor(query => query.PhotoId).NotEmpty().WithErrorCode(ServiceOrderErrors.PhotoNotFound.Code).WithMessage(ServiceOrderErrors.PhotoNotFound.Message);
    }
}

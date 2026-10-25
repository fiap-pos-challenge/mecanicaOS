namespace Oficina.Application.ServiceOrders;

public static class ServiceOrderPhotoPaths
{
    public static string Admin(Guid serviceOrderId, Guid photoId) => $"/api/admin/service-orders/{serviceOrderId}/photos/{photoId}";

    public static string Portal(Guid photoId) => $"/api/public/portal/photos/{photoId}";
}

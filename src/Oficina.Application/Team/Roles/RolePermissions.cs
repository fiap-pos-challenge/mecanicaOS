namespace Oficina.Application.Team.Roles;

public static class RolePermissions
{
    private static readonly string[] StaffOperation =
    [
        "dashboard.view", "serviceOrders.view", "serviceOrders.manage", "quotes.view", "quotes.manage",
        "customers.view", "customers.manage", "customers.reveal", "customers.import", "vehicles.manage",
        "catalog.view", "inventory.view", "inventory.count", "documents.view",
    ];

    public static readonly string[] All =
    [
        "dashboard.view", "serviceOrders.view", "serviceOrders.manage", "serviceOrders.viewOwn", "quotes.view",
        "quotes.manage", "quotes.viewOwn", "quotes.respondOwn", "quotes.approveDiscount", "mechanic.view",
        "customers.view", "customers.manage", "customers.delete", "customers.reveal", "customers.import",
        "customers.export", "vehicles.manage", "catalog.view", "catalog.viewServices", "catalog.viewCosts",
        "catalog.manage", "team.view", "team.manage", "inventory.view", "inventory.manage", "inventory.count",
        "inventory.approve", "reports.view", "finance.view", "documents.view", "automations.view",
        "automations.reprocess", "portal.view", "settings.view", "settings.manage", "platform.view",
    ];

    public static readonly IReadOnlyList<(string Role, IReadOnlyList<string> Permissions)> Roles =
    [
        ("Admin", All.Except(["serviceOrders.viewOwn", "quotes.viewOwn", "quotes.respondOwn", "portal.view"]).ToArray()),
        ("Manager", [.. StaffOperation, "mechanic.view", "customers.export", "catalog.viewCosts", "catalog.manage", "team.view", "inventory.manage", "inventory.approve", "quotes.approveDiscount", "reports.view", "finance.view", "automations.view"]),
        ("Attendant", StaffOperation),
        ("Mechanic", ["mechanic.view", "serviceOrders.view", "catalog.view", "inventory.view", "inventory.count"]),
        ("Customer", ["portal.view", "quotes.viewOwn", "quotes.respondOwn", "serviceOrders.viewOwn", "catalog.viewServices"]),
    ];
}

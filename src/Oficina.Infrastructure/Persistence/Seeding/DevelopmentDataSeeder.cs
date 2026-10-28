using Microsoft.Extensions.Options;
using Oficina.Application.Abstractions.Persistence;
using Oficina.Application.Abstractions.Security;
using Oficina.Domain.Catalog.Entities;
using Oficina.Domain.Catalog.Repositories;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.ValueObjects;
using Oficina.Domain.Identity.Entities;
using Oficina.Domain.Identity.Enums;
using Oficina.Domain.Identity.Repositories;
using Oficina.Domain.Inventory.Entities;
using Oficina.Domain.Inventory.Enums;
using Oficina.Domain.Inventory.Repositories;
using Oficina.Domain.Inventory.ValueObjects;

namespace Oficina.Infrastructure.Persistence.Seeding;

public sealed class DevelopmentDataSeeder(
    IUserRepository users,
    ICustomerRepository customers,
    IVehicleRepository vehicles,
    IServiceRepository services,
    IPartRepository parts,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<DevelopmentAdminOptions> options)
{
    private const string StaffPassword = "Oficina@2026";
    private const string MarkerDocument = "39053344705";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.Email))
        {
            return;
        }

        if (await customers.GetByDocumentAsync(Document.Create(MarkerDocument).Value, cancellationToken) is not null)
        {
            return;
        }

        await SeedUsersAsync(cancellationToken);
        SeedCustomersAndVehicles();
        SeedServices();
        SeedParts();
        await unitOfWork.CommitAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        foreach (var (name, address, role) in new[]
        {
            ("Ana Atendente", "ana@oficina.local", Role.Attendant),
            ("Caio Mecânico", "caio@oficina.local", Role.Mechanic),
        })
        {
            var email = Email.Create(address).Value;
            if (await users.GetByEmailAsync(email, cancellationToken) is null)
            {
                users.Add(User.Create(name, email, passwordHasher.Hash(StaffPassword), role).Value);
            }
        }
    }

    private void SeedCustomersAndVehicles()
    {
        var today = timeProvider.GetUtcNow();
        var data = new[]
        {
            (Name: "João Pereira", Document: MarkerDocument, Email: "joao.pereira@exemplo.com", Phone: "11988887777", Plate: "QWE4R56", Brand: "Fiat", Model: "Uno", Year: 2018),
            (Name: "Carla Mendes", Document: "11144477735", Email: "carla.mendes@exemplo.com", Phone: "21977776666", Plate: "RIO2A34", Brand: "Chevrolet", Model: "Onix", Year: 2021),
            (Name: "Transportes Silva Ltda", Document: "11222333000181", Email: "contato@silvatransportes.com", Phone: "1133334444", Plate: "FRT7B89", Brand: "Mercedes-Benz", Model: "Sprinter", Year: 2019),
        };

        foreach (var item in data)
        {
            var customer = Customer.Create(item.Name, Document.Create(item.Document).Value, Email.Create(item.Email).Value, Phone.Create(item.Phone).Value, null).Value;
            customers.Add(customer);
            vehicles.Add(Vehicle.Create(customer.Id, LicensePlate.Create(item.Plate).Value, item.Brand, item.Model, VehicleYear.Create(item.Year).Value, today).Value);
        }
    }

    private void SeedServices()
    {
        foreach (var (name, description, price, minutes) in new[]
        {
            ("Alinhamento e balanceamento", "Alinhamento das quatro rodas e balanceamento", 120m, 60),
            ("Troca de pastilhas de freio", "Mão de obra para troca do jogo dianteiro ou traseiro", 180m, 90),
            ("Revisão completa", "Inspeção de 30 itens com relatório", 350m, 180),
            ("Diagnóstico eletrônico", "Leitura de falhas com scanner", 90m, 30),
        })
        {
            services.Add(Service.Create(name, description, Money.Create(price).Value, minutes).Value);
        }
    }

    private void SeedParts()
    {
        foreach (var (code, name, type, unit, price, minimum, stock) in new[]
        {
            ("OLE-5W30", "Óleo 5W30 sintético", PartType.Supply, "L", 42.5m, 10m, 40m),
            ("FLT-AR01", "Filtro de ar", PartType.Part, "un", 35m, 3m, 12m),
            ("PAS-DIA01", "Pastilha de freio dianteira", PartType.Part, "jg", 95m, 4m, 8m),
            ("VEL-IGN01", "Vela de ignição", PartType.Part, "un", 28m, 8m, 3m),
            ("ADT-RAD01", "Aditivo de radiador", PartType.Supply, "L", 22m, 5m, 15m),
        })
        {
            var part = Part.Create(PartCode.Create(code).Value, name, type, unit, Money.Create(price).Value, minimum).Value;
            part.RegisterEntry(Quantity.Create(stock).Value, "Estoque inicial", null);
            parts.Add(part);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oficina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "serviceOrderNumbers");

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    document = table.Column<string>(type: "varchar(14)", unicode: false, maxLength: 14, nullable: false),
                    documentType = table.Column<string>(type: "varchar(4)", unicode: false, maxLength: 4, nullable: false),
                    email = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    addressZipCode = table.Column<string>(type: "char(8)", nullable: true),
                    addressStreet = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    addressNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    addressComplement = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    addressDistrict = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    addressCity = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    addressState = table.Column<string>(type: "char(2)", nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outboxMessages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    occurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    processedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    attempts = table.Column<int>(type: "int", nullable: false),
                    error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outboxMessages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    type = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    unitOfMeasure = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    unitPrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    stockQuantity = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    minimumStock = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    rowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    basePrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    estimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_services", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    email = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    passwordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    licensePlate = table.Column<string>(type: "varchar(7)", unicode: false, maxLength: 7, nullable: false),
                    brand = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    model = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    year = table.Column<short>(type: "smallint", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehicles", x => x.id);
                    table.ForeignKey(
                        name: "fk_vehicles_customers_customerId",
                        column: x => x.customerId,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "serviceOrders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false),
                    customerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    vehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    problemDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    totalAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    executionStartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    completedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deliveredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    rowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serviceOrders", x => x.id);
                    table.ForeignKey(
                        name: "fk_serviceOrders_customers_customerId",
                        column: x => x.customerId,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_serviceOrders_vehicles_vehicleId",
                        column: x => x.vehicleId,
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "partItems",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    serviceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    partId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    description = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    unitPrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_partItems", x => x.id);
                    table.ForeignKey(
                        name: "fk_partItems_parts_partId",
                        column: x => x.partId,
                        principalTable: "parts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_partItems_serviceOrders_serviceOrderId",
                        column: x => x.serviceOrderId,
                        principalTable: "serviceOrders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "quotes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    serviceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: false),
                    servicesTotal = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    partsTotal = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    tokenHash = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    sentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    respondedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    rejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotes", x => x.id);
                    table.ForeignKey(
                        name: "fk_quotes_serviceOrders_serviceOrderId",
                        column: x => x.serviceOrderId,
                        principalTable: "serviceOrders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "serviceItems",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    serviceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    serviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    description = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    unitPrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    quantity = table.Column<int>(type: "int", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serviceItems", x => x.id);
                    table.ForeignKey(
                        name: "fk_serviceItems_serviceOrders_serviceOrderId",
                        column: x => x.serviceOrderId,
                        principalTable: "serviceOrders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_serviceItems_services_serviceId",
                        column: x => x.serviceId,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "serviceOrderStatusHistory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    serviceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    previousStatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    newStatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    changedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    changedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_serviceOrderStatusHistory", x => x.id);
                    table.ForeignKey(
                        name: "fk_serviceOrderStatusHistory_serviceOrders_serviceOrderId",
                        column: x => x.serviceOrderId,
                        principalTable: "serviceOrders",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_serviceOrderStatusHistory_users_changedByUserId",
                        column: x => x.changedByUserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stockMovements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    partId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    serviceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    userId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    type = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    quantity = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    balanceAfter = table.Column<decimal>(type: "decimal(12,3)", precision: 12, scale: 3, nullable: false),
                    reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    deletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stockMovements", x => x.id);
                    table.ForeignKey(
                        name: "fk_stockMovements_parts_partId",
                        column: x => x.partId,
                        principalTable: "parts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_stockMovements_serviceOrders_serviceOrderId",
                        column: x => x.serviceOrderId,
                        principalTable: "serviceOrders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_stockMovements_users_userId",
                        column: x => x.userId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_customers_document",
                table: "customers",
                column: "document",
                unique: true,
                filter: "[deletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outboxMessages_processedAt",
                table: "outboxMessages",
                column: "processedAt");

            migrationBuilder.CreateIndex(
                name: "ix_partItems_partId",
                table: "partItems",
                column: "partId");

            migrationBuilder.CreateIndex(
                name: "ix_partItems_serviceOrderId",
                table: "partItems",
                column: "serviceOrderId");

            migrationBuilder.CreateIndex(
                name: "ux_parts_code",
                table: "parts",
                column: "code",
                unique: true,
                filter: "[deletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_quotes_serviceOrderId_version",
                table: "quotes",
                columns: new[] { "serviceOrderId", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_quotes_tokenHash",
                table: "quotes",
                column: "tokenHash",
                unique: true,
                filter: "[tokenHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_serviceItems_serviceId",
                table: "serviceItems",
                column: "serviceId");

            migrationBuilder.CreateIndex(
                name: "ix_serviceItems_serviceOrderId",
                table: "serviceItems",
                column: "serviceOrderId");

            migrationBuilder.CreateIndex(
                name: "ix_serviceOrders_customerId",
                table: "serviceOrders",
                column: "customerId");

            migrationBuilder.CreateIndex(
                name: "ix_serviceOrders_vehicleId",
                table: "serviceOrders",
                column: "vehicleId");

            migrationBuilder.CreateIndex(
                name: "ux_serviceOrders_number",
                table: "serviceOrders",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_serviceOrders_openPerVehicle",
                table: "serviceOrders",
                column: "vehicleId",
                unique: true,
                filter: "[status] <> 'Delivered' AND [deletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_serviceOrderStatusHistory_changedByUserId",
                table: "serviceOrderStatusHistory",
                column: "changedByUserId");

            migrationBuilder.CreateIndex(
                name: "ix_serviceOrderStatusHistory_serviceOrderId",
                table: "serviceOrderStatusHistory",
                column: "serviceOrderId");

            migrationBuilder.CreateIndex(
                name: "ix_stockMovements_partId",
                table: "stockMovements",
                column: "partId");

            migrationBuilder.CreateIndex(
                name: "ix_stockMovements_serviceOrderId",
                table: "stockMovements",
                column: "serviceOrderId");

            migrationBuilder.CreateIndex(
                name: "ix_stockMovements_userId",
                table: "stockMovements",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "ux_users_email",
                table: "users",
                column: "email",
                unique: true,
                filter: "[deletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_vehicles_customerId",
                table: "vehicles",
                column: "customerId");

            migrationBuilder.CreateIndex(
                name: "ux_vehicles_licensePlate",
                table: "vehicles",
                column: "licensePlate",
                unique: true,
                filter: "[deletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outboxMessages");

            migrationBuilder.DropTable(
                name: "partItems");

            migrationBuilder.DropTable(
                name: "quotes");

            migrationBuilder.DropTable(
                name: "serviceItems");

            migrationBuilder.DropTable(
                name: "serviceOrderStatusHistory");

            migrationBuilder.DropTable(
                name: "stockMovements");

            migrationBuilder.DropTable(
                name: "services");

            migrationBuilder.DropTable(
                name: "parts");

            migrationBuilder.DropTable(
                name: "serviceOrders");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropSequence(
                name: "serviceOrderNumbers");
        }
    }
}

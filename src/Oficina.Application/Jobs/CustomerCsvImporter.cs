using Oficina.Application.Jobs.Dtos;
using Oficina.Domain.Common.Results;
using Oficina.Domain.Common.ValueObjects;
using Oficina.Domain.Customers.Entities;
using Oficina.Domain.Customers.Repositories;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Jobs;

public sealed class CustomerCsvImporter(ICustomerRepository customers)
{
    private static readonly string[] Header = ["nome", "documento", "telefone", "email"];

    public async Task<ImportReport> ImportAsync(string csv, CancellationToken cancellationToken)
    {
        var lines = csv.TrimStart('﻿').Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.Trim().Length > 0).ToList();
        var header = lines.Count > 0 ? lines[0] : string.Empty;
        var separator = header.Contains(';', StringComparison.Ordinal) ? ';' : ',';
        var columns = header.Split(separator).Select(column => column.Trim().ToLowerInvariant()).ToArray();
        var rows = lines.Skip(1).ToList();
        if (columns.Length < Header.Length || Header.Where((column, index) => columns[index] != column).Any())
        {
            return new ImportReport(rows.Count, 0, [new ImportErrorDto(1, "cabecalho", JobMessages.Header)]);
        }

        var errors = new List<ImportErrorDto>();
        var documents = new HashSet<string>(StringComparer.Ordinal);
        var valid = 0;
        for (var index = 0; index < rows.Count; index++)
        {
            var line = index + 2;
            var cells = rows[index].Split(separator).Select(cell => cell.Trim()).ToArray();
            string Cell(int position) => position < cells.Length ? cells[position] : string.Empty;
            var lineErrors = new List<ImportErrorDto>();
            var name = Cell(0);
            if (name.Length < 2)
            {
                lineErrors.Add(new ImportErrorDto(line, "nome", JobMessages.Name));
            }

            var document = Document.Create(Cell(1));
            if (document.IsFailure)
            {
                lineErrors.Add(new ImportErrorDto(line, "documento", JobMessages.Document));
            }

            var phone = Phone.Create(Cell(2));
            if (phone.IsFailure)
            {
                lineErrors.Add(new ImportErrorDto(line, "telefone", JobMessages.Phone));
            }

            Result<Email>? email = Cell(3).Length == 0 ? null : Email.Create(Cell(3));
            if (email is { IsFailure: true })
            {
                lineErrors.Add(new ImportErrorDto(line, "email", JobMessages.Email));
            }

            if (lineErrors.Count == 0 && (!documents.Add(document.Value.Value) || await customers.GetByDocumentAsync(document.Value, cancellationToken) is not null))
            {
                lineErrors.Add(new ImportErrorDto(line, "documento", JobMessages.DuplicateDocument));
            }

            if (lineErrors.Count == 0 && Customer.Create(name, document.Value, email?.Value, phone.Value, null) is { IsSuccess: true } customer)
            {
                customers.Add(customer.Value);
                valid++;
                continue;
            }

            errors.AddRange(lineErrors.Count > 0 ? lineErrors : [new ImportErrorDto(line, "nome", JobMessages.Invalid)]);
        }

        return new ImportReport(rows.Count, valid, errors);
    }
}

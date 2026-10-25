using Oficina.Application.Common.Pagination;
using Oficina.Domain.Customers.Enums;
using Oficina.Domain.Customers.ValueObjects;

namespace Oficina.Application.Customers.Queries;

public sealed record CustomerFilter(string? Name, Document? Document, DocumentType? DocumentType, CustomerSort Sort, SortDirection Direction);

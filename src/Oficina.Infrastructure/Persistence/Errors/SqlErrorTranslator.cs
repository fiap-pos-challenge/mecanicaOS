using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Oficina.Domain.Common.Errors;
using Oficina.Domain.Common.Results;

namespace Oficina.Infrastructure.Persistence.Errors;

public static class SqlErrorTranslator
{
    private const int DuplicateKeyInUniqueIndex = 2601;
    private const int UniqueConstraintViolation = 2627;

    public static Error? Translate(DbUpdateException exception) => exception switch
    {
        DbUpdateConcurrencyException => CommonErrors.ConcurrencyConflict,
        { InnerException: SqlException { Number: DuplicateKeyInUniqueIndex or UniqueConstraintViolation } } => CommonErrors.DuplicateRecord,
        _ => null,
    };
}

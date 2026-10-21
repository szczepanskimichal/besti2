using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace besti2.Infrastructure.Persistence;

public static class DbUpdateExceptionExtensions
{
    // True when SQL Server refused a duplicate in a unique index (2601) or unique key (2627).
    // Only these become a friendly message; any other database error stays an error (and is logged)
    public static bool IsUniqueViolation(this DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
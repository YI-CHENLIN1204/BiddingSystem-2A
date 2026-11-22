namespace Microsoft.EntityFrameworkCore
{
    public static class ReadOnlyFixtureExtensions
    {
        public static IEnumerable<T> AsNoTracking<T>(this IEnumerable<T> rows) => rows;
    }
}

namespace 崴正.Data
{
    public sealed record Category(bool AllowConversion);
    public sealed record Unit(int UnitId, string UnitName, int UnitCategoryId,
        Category UnitCategory, decimal? ToBaseFactor, bool IsBaseUnit);
    public sealed record ApplicationDbContext(IEnumerable<Unit> UnitMasters);
}

namespace Ceiba.Core.Interfaces;

/// <summary>
/// Search criteria for report queries.
/// Encapsulates all filter, pagination, and sorting parameters.
/// </summary>
public record ReportSearchCriteria
{
    public int? Estado { get; init; }
    public int? ZonaId { get; init; }
    public string? Delito { get; init; }
    public DateTime? FechaDesde { get; init; }
    public DateTime? FechaHasta { get; init; }
    public Guid? UsuarioId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string SortBy { get; init; } = "createdAt";
    public bool SortDesc { get; init; } = true;
}

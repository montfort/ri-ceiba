using Ceiba.Core.Entities;

namespace Ceiba.Core.Interfaces;

/// <summary>
/// Repository interface for ReporteIncidencia entity.
/// Data access layer abstraction.
/// </summary>
public interface IReportRepository
{
    Task<ReporteIncidencia?> GetByIdAsync(int id);
    Task<ReporteIncidencia?> GetByIdWithRelationsAsync(int id);
    Task<List<ReporteIncidencia>> GetByUsuarioIdAsync(Guid usuarioId);
    Task<(List<ReporteIncidencia> Items, int TotalCount)> SearchAsync(ReportSearchCriteria criteria);
    Task<ReporteIncidencia> AddAsync(ReporteIncidencia report);
    Task<ReporteIncidencia> UpdateAsync(ReporteIncidencia report);
    Task DeleteAsync(int id);
}

namespace Ceiba.Core.Entities;

/// <summary>
/// Configuración para la generación automática de reportes
/// </summary>
public class ConfiguracionReportesAutomatizados : BaseEntityWithUser
{
    /// <summary>
    /// Identificador único de la configuración
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Indica si la generación automática está habilitada
    /// </summary>
    public bool Habilitado { get; set; }

    /// <summary>
    /// Hora del día para generar reportes automáticamente (formato HH:mm:ss)
    /// Por defecto: 06:00:00
    /// </summary>
    public TimeSpan HoraGeneracion { get; set; } = new TimeSpan(6, 0, 0);

    /// <summary>
    /// Lista de destinatarios de correo electrónico separados por comas
    /// Ejemplo: "supervisor@example.com,director@example.com"
    /// </summary>
    public string Destinatarios { get; set; } = string.Empty;

    /// <summary>
    /// Ruta donde se guardan los archivos generados
    /// Por defecto: ./generated-reports
    /// </summary>
    public string RutaSalida { get; set; } = "./generated-reports";

    /// <summary>
    /// Última vez que se modificó esta configuración
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Obtiene la lista de destinatarios como array
    /// </summary>
    public string[] GetDestinatariosArray()
    {
        if (string.IsNullOrWhiteSpace(Destinatarios))
            return Array.Empty<string>();

        return Destinatarios
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim())
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .ToArray();
    }

    /// <summary>
    /// Establece los destinatarios desde un array
    /// </summary>
    public void SetDestinatariosArray(string[] destinatarios)
    {
        Destinatarios = string.Join(",", destinatarios.Where(d => !string.IsNullOrWhiteSpace(d)));
    }
}

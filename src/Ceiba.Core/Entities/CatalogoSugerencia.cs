namespace Ceiba.Core.Entities;

/// <summary>
/// Configurable suggestion catalog for text field autocomplete.
/// Allows ADMIN users to configure suggestion lists for specific fields.
/// Fields: sexo, delito, tipo_de_atencion
/// </summary>
public class CatalogoSugerencia : BaseCatalogEntity
{
    /// <summary>
    /// Unique identifier for the suggestion.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Target field name for this suggestion.
    /// Valid values: "sexo", "delito", "tipo_de_atencion"
    /// Required, max 50 characters.
    /// </summary>
    public string Campo { get; set; } = string.Empty;

    /// <summary>
    /// Suggestion value to display in autocomplete.
    /// Example: "Masculino", "Robo", "Orientación"
    /// Required, max 200 characters.
    /// </summary>
    public string Valor { get; set; } = string.Empty;

    /// <summary>
    /// Display order for suggestions (ascending).
    /// Lower numbers appear first in dropdown.
    /// Default: 0
    /// </summary>
    public int Orden { get; set; } = 0;
}

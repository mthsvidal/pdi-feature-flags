namespace PdiContracts.Domain;

/// <summary>
/// Configuracoes para conexao com o Flipt.
/// </summary>
public class FliptSettings
{
    /// <summary>
    /// URL base do servidor Flipt (ex: http://localhost:8080).
    /// </summary>
    public string FliptUrl { get; set; } = string.Empty;

    /// <summary>
    /// API token para autenticacao Bearer.
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Namespace utilizado na avaliacao de flags.
    /// </summary>
    public string NamespaceKey { get; set; } = string.Empty;

    /// <summary>
    /// Timeout para requisicoes HTTP (em segundos).
    /// </summary>
    public int TimeoutSeconds { get; set; }
}
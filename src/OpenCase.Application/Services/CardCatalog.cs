namespace OpenCase.Application.Services;

/// <summary>
/// Nomes canônicos das cartas do MVP. Usado pela engine e pelo seed do banco.
/// </summary>
public static class CardCatalog
{
    public static readonly string[] SuspectNames =
    [
        "Coronel Mostarda", "Dona Violeta", "Professor Black", "Senhorita Rosa",
        "Doutor Marinho", "Madame Café", "Capitão Cinza", "Jovem Verde",
    ];

    public static readonly string[] WeaponNames =
    [
        "Castiçal", "Adaga", "Corda", "Revólver",
        "Cano de Chumbo", "Chave Inglesa", "Veneno", "Abridor de Cartas",
    ];
}

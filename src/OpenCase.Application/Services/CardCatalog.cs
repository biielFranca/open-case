namespace OpenCase.Application.Services;

/// <summary>
/// Nomes canônicos das cartas do MVP. Usado pela engine e pelo seed do banco.
/// </summary>
public static class CardCatalog
{
    public static readonly string[] SuspectNames =
    [
        .. RoomService.AvailablePawns.Select(pawn => pawn.Name),
    ];

    public static readonly string[] WeaponNames =
    [
        "Castiçal", "Adaga", "Corda", "Revólver",
        "Cano de Chumbo", "Chave Inglesa", "Veneno", "Abridor de Cartas",
        "Troféu de Bronze", "Bengala-Espada", "Machado Cerimonial", "Fio de Piano",
    ];
}

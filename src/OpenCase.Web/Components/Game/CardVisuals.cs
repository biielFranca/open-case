using OpenCase.Shared.Dtos;

namespace OpenCase.Web.Components.Game;

public static class CardVisuals
{
    private static readonly Dictionary<string, string> SuspectImages = new()
    {
        ["Detetive Arthur Vale"] = "images/characters/arthur-vale.jpg",
        ["Condessa Helena Vesper"] = "images/characters/helena-vesper.jpg",
        ["Professor Otávio Lacerda"] = "images/characters/otavio-lacerda.jpg",
        ["Doutora Cecília Marinho"] = "images/characters/cecilia-marinho.jpg",
        ["Capitão Raul Ferraz"] = "images/characters/raul-ferraz.jpg",
        ["Madame Amélia Bellini"] = "images/characters/amelia-bellini.jpg",
        ["Jornalista Clara Vidal"] = "images/characters/clara-vidal.jpg",
        ["Juiz Afonso Brandão"] = "images/characters/afonso-brandao.jpg",
        ["Mordomo Sebastião Leme"] = "images/characters/sebastiao-leme.jpg",
        ["Cantora Íris Montenegro"] = "images/characters/iris-montenegro.jpg",
        ["Empresário Vicente Dourado"] = "images/characters/vicente-dourado.jpg",
        ["Jardineira Elisa Campos"] = "images/characters/elisa-campos.jpg",
        ["Coronel Mostarda"] = "images/characters/raul-ferraz.jpg",
        ["Dona Violeta"] = "images/characters/helena-vesper.jpg",
        ["Professor Black"] = "images/characters/otavio-lacerda.jpg",
        ["Senhorita Rosa"] = "images/characters/amelia-bellini.jpg",
        ["Doutor Marinho"] = "images/characters/cecilia-marinho.jpg",
        ["Madame Café"] = "images/characters/clara-vidal.jpg",
        ["Capitão Cinza"] = "images/characters/afonso-brandao.jpg",
        ["Jovem Verde"] = "images/characters/vicente-dourado.jpg",
    };

    private static readonly Dictionary<string, string> LocationImages = new()
    {
        ["Biblioteca"] = "biblioteca",
        ["Salão de Festas"] = "salao-de-festas",
        ["Cozinha"] = "cozinha",
        ["Escritório"] = "escritorio",
        ["Jardim de Inverno"] = "jardim-de-inverno",
        ["Sala de Jantar"] = "sala-de-jantar",
        ["Salão de Jogos"] = "salao-de-jogos",
        ["Hall"] = "hall",
        ["Estufa"] = "estufa",
        ["Porão"] = "porao",
        ["Observatório"] = "observatorio",
        ["Quarto de Hóspedes"] = "quarto-de-hospedes",
    };

    private static readonly Dictionary<string, string> WeaponImages = new()
    {
        ["Castiçal"] = "candlestick",
        ["Adaga"] = "dagger",
        ["Corda"] = "rope",
        ["Revólver"] = "revolver",
        ["Cano de Chumbo"] = "lead-pipe",
        ["Chave Inglesa"] = "adjustable-wrench",
        ["Veneno"] = "poison-bottle",
        ["Abridor de Cartas"] = "letter-opener",
        ["Troféu de Bronze"] = "bronze-trophy",
        ["Bengala-Espada"] = "cane-sword",
        ["Machado Cerimonial"] = "ceremonial-axe",
        ["Fio de Piano"] = "piano-wire",
    };

    public static string ImageFor(CardDto card) => card.Type switch
    {
        "Suspect" => SuspectImages.GetValueOrDefault(card.Name, "images/login-detective.jpg"),
        "Location" => $"images/cards/locations/{LocationImages.GetValueOrDefault(card.Name, "hall")}.png",
        "Weapon" => $"images/cards/weapons/{WeaponImages.GetValueOrDefault(card.Name, "letter-opener")}.png",
        _ => "images/login-detective.jpg",
    };

    public static string TypeLabel(string type) => type switch
    {
        "Suspect" => "Suspeito",
        "Location" => "Local",
        "Weapon" => "Arma",
        _ => type,
    };
}

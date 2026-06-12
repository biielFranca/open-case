namespace OpenCase.Web.Components.Lobby;

public static class CharacterVisuals
{
    public static string ImageFor(string? characterName) => characterName switch
    {
        "Detetive Arthur Vale" => "images/characters/arthur-vale.jpg",
        "Condessa Helena Vesper" => "images/characters/helena-vesper.jpg",
        "Professor Otávio Lacerda" => "images/characters/otavio-lacerda.jpg",
        "Doutora Cecília Marinho" => "images/characters/cecilia-marinho.jpg",
        "Capitão Raul Ferraz" => "images/characters/raul-ferraz.jpg",
        "Madame Amélia Bellini" => "images/characters/amelia-bellini.jpg",
        "Jornalista Clara Vidal" => "images/characters/clara-vidal.jpg",
        "Juiz Afonso Brandão" => "images/characters/afonso-brandao.jpg",
        "Mordomo Sebastião Leme" => "images/characters/sebastiao-leme.jpg",
        "Cantora Íris Montenegro" => "images/characters/iris-montenegro.jpg",
        "Empresário Vicente Dourado" => "images/characters/vicente-dourado.jpg",
        "Jardineira Elisa Campos" => "images/characters/elisa-campos.jpg",
        _ => "images/login-detective.jpg",
    };

    public static string ShortName(string? characterName)
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            return "Sem personagem";
        }

        var firstSpace = characterName.IndexOf(' ');
        return firstSpace < 0 ? characterName : characterName[(firstSpace + 1)..];
    }
}

using OpenCase.Domain.Entities;

namespace OpenCase.Application.Services;

internal static class HintCatalog
{
    private sealed record HintLines(string[] Suspicion, string[] Innocence);

    private static readonly Dictionary<string, HintLines> LinesByCard = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Detetive Arthur Vale"] = new(
            [
                "A figura ergueu algo redondo diante do rosto, como se examinasse a propria cena.",
                "Um tecido pesado rocou a poeira junto a passagem, comprido demais para uma jaqueta comum.",
            ],
            [
                "Quem fugiu nao carregava instrumento de investigacao algum.",
                "A testemunha descreveu roupas leves, sem o peso de um sobretudo escuro.",
            ]),
        ["Condessa Helena Vesper"] = new(
            [
                "Um estalo seco, parecido com um leque se fechando, cortou o silencio.",
                "Um fio em tom de vinho ficou preso perto da macaneta.",
            ],
            [
                "A pessoa usava roupas simples, sem seda, perolas ou sinal de nobreza.",
                "Nenhum leque foi visto ou ouvido naquela parte da mansao.",
            ]),
        ["Professor Otavio Lacerda"] = new(
            [
                "Um reflexo circular brilhou entre as estantes, como lentes pegando pouca luz.",
                "Havia po de livro antigo em uma manga escura perto da janela.",
            ],
            [
                "Quem passou enxergava bem no escuro e nao parecia usar oculos.",
                "A pessoa nao carregava livros, papeis ou qualquer objeto de estudo.",
            ]),
        ["Doutora Cecilia Marinho"] = new(
            [
                "Um pequeno frasco brilhou entre os dedos antes que a porta se fechasse.",
                "Ficou no ar um cheiro limpo e amargo, quase medicinal.",
            ],
            [
                "A testemunha nao viu frascos, luvas ou qualquer instrumento medico.",
                "Quem fugiu demonstrou panico diante da cena, sem a calma de alguem treinado.",
            ]),
        ["Capitao Raul Ferraz"] = new(
            [
                "Os passos eram firmes e ritmados, como de alguem acostumado a marchar.",
                "Um pequeno brilho de latao apareceu junto a roupa escura.",
            ],
            [
                "A pessoa se movia sem disciplina e quase tropecou no corredor.",
                "Nao havia uniforme, botoes metalicos ou postura militar na silhueta.",
            ]),
        ["Madame Amelia Bellini"] = new(
            [
                "Uma joia magenta refletiu a luz antes que a cortina voltasse ao lugar.",
                "O perfume deixado no aposento era intenso demais para ser acidental.",
            ],
            [
                "A figura nao usava joias, perfume marcante ou qualquer detalhe chamativo.",
                "As roupas vistas eram opacas e discretas, sem brilho de palco.",
            ]),
        ["Jornalista Clara Vidal"] = new(
            [
                "Um clarao rapido, como flash de camera, veio do corredor.",
                "Uma folha pequena cheia de anotacoes apressadas foi encontrada no chao.",
            ],
            [
                "A testemunha viu claramente a cabeca da pessoa; nao havia chapeu.",
                "Nenhuma camera, caderno ou anotacao apareceu perto da cena.",
            ]),
        ["Juiz Afonso Brandao"] = new(
            [
                "Tres batidas curtas ecoaram, como um pequeno martelo pedindo silencio.",
                "A figura se mantinha rigida, como se ainda estivesse diante de um tribunal.",
            ],
            [
                "Quem passou falava de modo informal demais para uma autoridade solene.",
                "Nenhum som de batida, martelo ou objeto cerimonial foi ouvido.",
            ]),
        ["Mordomo Sebastiao Leme"] = new(
            [
                "Luvas claras tocaram a macaneta sem deixar marcas visiveis.",
                "A pessoa conhecia uma porta de servico que poucos convidados notariam.",
            ],
            [
                "Foram encontradas digitais nitidas; a pessoa nao usava luvas.",
                "A figura se perdeu nos corredores e tentou abrir a porta errada.",
            ]),
        ["Cantora Iris Montenegro"] = new(
            [
                "Alguem cantarolava baixo enquanto esperava o corredor esvaziar.",
                "Uma luva longa foi deixada perto das cortinas.",
            ],
            [
                "A voz ouvida era rouca e sem qualquer controle musical.",
                "Quem passou estava com as maos descobertas e nao usava luvas longas.",
            ]),
        ["Empresario Vicente Dourado"] = new(
            [
                "Uma corrente dourada brilhou por baixo do paleto quando a figura virou.",
                "A pessoa verificou o relogio varias vezes, como se seguisse um plano.",
            ],
            [
                "O suspeito nao usava relogio, corrente ou acessorio dourado.",
                "Documentos ficaram espalhados; quem passou nao protegia pasta alguma.",
            ]),
        ["Jardineira Elisa Campos"] = new(
            [
                "Terra fresca apareceu no corredor, embora ninguem admitisse ter vindo do jardim.",
                "Um pequeno espinho ficou preso em um tecido de tom quente.",
            ],
            [
                "Os sapatos vistos estavam limpos, sem terra ou marcas de jardim.",
                "Nao havia folhas, flores ou cheiro de planta na roupa descrita.",
            ]),

        ["Biblioteca"] = new(
            [
                "A testemunha ouviu uma escada deslizar pouco antes do silencio.",
                "Poeira de livros antigos apareceu misturada as marcas da cena.",
            ],
            [
                "Nao havia cheiro de papel antigo, estantes ou livros por perto.",
                "O teto descrito era baixo demais para janelas altas ou escadas de biblioteca.",
            ]),
        ["Salao de Festas"] = new(
            [
                "Um grande lustre se refletiu no piso antes das luzes falharem.",
                "O eco dos passos denunciava um espaco amplo e quase vazio.",
            ],
            [
                "O ambiente era apertado demais para recepcao ou pista de danca.",
                "Nao havia tacas, cortinas pesadas ou sinais de festa interrompida.",
            ]),
        ["Cozinha"] = new(
            [
                "Ainda havia vapor no ar quando a porta foi aberta.",
                "Reflexos de cobre se espalhavam por superficies proximas a cena.",
            ],
            [
                "O local estava frio, sem fogao aceso, vapor ou cheiro de tempero.",
                "Nada parecia bancada, panela ou utensilio de preparo.",
            ]),
        ["Jardim de Inverno"] = new(
            [
                "Pegadas umidas cruzavam o piso mesmo sem chuva entrando pela porta.",
                "Folhas largas escondiam parte da passagem usada por alguem.",
            ],
            [
                "Nao havia agua, fonte, folhas ou bancos de jardim na cena.",
                "O teto era fechado demais para revelar lua ou vidro acima.",
            ]),
        ["Sala de Jantar"] = new(
            [
                "Uma cadeira ficou afastada da mesa, como se alguem tivesse levantado depressa.",
                "Velas refletiam em uma superficie longa e escura.",
            ],
            [
                "Nao havia mesa posta, pratos, talheres ou restos de cera.",
                "O espaco era pequeno demais para uma mesa comprida.",
            ]),
        ["Hall"] = new(
            [
                "Os passos ecoaram alto, dividindo-se como se houvesse escadas dos dois lados.",
                "Uma luz ambar refletiu em um piso brilhante e simetrico.",
            ],
            [
                "Nao havia escadas, corrimaos ou acesso ao andar superior.",
                "O ambiente descrito era estreito e irregular, sem grande entrada.",
            ]),
        ["Escritorio"] = new(
            [
                "Papeis estavam espalhados como se alguem procurasse algo urgente.",
                "Uma gaveta de arquivos tinha marcas recentes perto da fechadura.",
            ],
            [
                "Nao havia documentos, arquivos ou escrivaninha perto da cena.",
                "O chao estava limpo demais para uma busca em papeis.",
            ]),
        ["Salao de Jogos"] = new(
            [
                "Uma bola pequena rolou para longe da mesa quando todos ficaram em silencio.",
                "Fichas foram recolhidas as pressas, deixando marcas circulares.",
            ],
            [
                "Nao havia mesa de jogo, tacos, cartas ou fichas no ambiente.",
                "A iluminacao vinha de janelas abertas, nao de luminarias baixas.",
            ]),
        ["Estufa"] = new(
            [
                "Um vaso quebrado espalhou terra fresca pelo chao.",
                "O ar estava quente e umido, carregado de folhas cortadas.",
            ],
            [
                "Nenhum vaso, terra ou instrumento de jardinagem apareceu na cena.",
                "O ambiente era seco e frio, sem umidade de vidro fechado.",
            ]),
        ["Porao"] = new(
            [
                "O som parecia abafado por paredes grossas e arcos de pedra.",
                "Caixas e barris cobertos de poeira cercavam a passagem.",
            ],
            [
                "A cena recebia luz direta da lua e parecia acima do jardim.",
                "Nao havia barris, caixas ou poeira antiga por perto.",
            ]),
        ["Observatorio"] = new(
            [
                "Uma lente quebrada espalhou pequenos pontos de luz pelo chao.",
                "O teto parecia aberto para o ceu, com metal apontado para as estrelas.",
            ],
            [
                "O teto era plano e fechado, sem abertura para o ceu.",
                "Nenhuma lente, telescopio ou instrumento cientifico foi encontrado.",
            ]),
        ["Quarto de Hospedes"] = new(
            [
                "Uma chave esquecida sobre o criado-mudo nao pertencia aos empregados.",
                "Malas antigas ficaram fechadas aos pes de uma cama arrumada.",
            ],
            [
                "Nao havia cama, malas, criado-mudo ou sinal de hospedagem.",
                "A janela estava lacrada e nenhuma cortina poderia se mover.",
            ]),

        ["Castical"] = new(
            [
                "A luz revelou uma coluna dourada com base pesada.",
                "Uma gota de cera endurecida ficou perto da marca principal.",
            ],
            [
                "Nao havia cera, pavio ou formato capaz de sustentar vela.",
                "O objeto visto nao tinha base larga nem brilho ornamental.",
            ]),
        ["Adaga"] = new(
            [
                "Um reflexo curto e vermelho percorreu uma lamina simetrica.",
                "A marca sugeria ponta central e corte dos dois lados.",
            ],
            [
                "O objeto nao tinha ponta, fio ou reflexo de lamina.",
                "A testemunha descreveu uma peca sem cabo e sem ornamento.",
            ]),
        ["Corda"] = new(
            [
                "Fibras asperas ficaram presas na madeira perto da cena.",
                "Um no apertado parecia ter sido feito com cuidado demais.",
            ],
            [
                "Nenhuma fibra, no ou material trancado foi encontrado.",
                "O objeto era rigido e manteve a mesma forma ao ser carregado.",
            ]),
        ["Revolver"] = new(
            [
                "Um clique mecanico veio de algo pequeno o bastante para uma mao.",
                "A silhueta mostrava um tambor metalico acima de um cabo escuro.",
            ],
            [
                "Nao havia cano, gatilho ou mecanismo visivel.",
                "Nada girava ou produzia cliques quando foi movido.",
            ]),
        ["Cano de Chumbo"] = new(
            [
                "A marca no chao tinha o formato circular de um tubo pesado.",
                "Uma superficie cinzenta e fosca refletiu pouca luz.",
            ],
            [
                "O objeto tinha cabo e detalhes decorativos, nao uma peca simples.",
                "Nenhuma abertura circular apareceu entre as evidencias.",
            ]),
        ["Chave Inglesa"] = new(
            [
                "Duas mandibulas metalicas apareceram abertas sob luz de cobre.",
                "Uma pequena engrenagem brilhou junto a cabeca da ferramenta.",
            ],
            [
                "O objeto nao tinha mandibulas, engrenagem ou ajuste movel.",
                "A peca parecia delicada demais para ferramenta pesada.",
            ]),
        ["Veneno"] = new(
            [
                "Um brilho verde atravessou vidro pequeno quando foi erguido.",
                "Liquido se moveu dentro de um recipiente fechado.",
            ],
            [
                "Nao havia vidro, liquido ou recipiente entre os objetos vistos.",
                "A peca era opaca e pesada demais para um frasco delicado.",
            ]),
        ["Abridor de Cartas"] = new(
            [
                "Uma lamina fina e polida apareceu junto a papeis espalhados.",
                "Um cabo claro contrastava com o brilho frio do metal.",
            ],
            [
                "O objeto tinha lamina larga e pesada, nada parecido com item de escritorio.",
                "Nao havia cabo claro nem ponta discreta na descricao.",
            ]),
        ["Trofeu de Bronze"] = new(
            [
                "Uma taca pesada com alcas deixou marca circular no po.",
                "O bronze envelhecido brilhou sobre uma base solida.",
            ],
            [
                "O objeto nao tinha alcas, taca ou base de exposicao.",
                "A testemunha descreveu algo fino e leve demais para premio pesado.",
            ]),
        ["Bengala-Espada"] = new(
            [
                "Uma lamina estreita surgiu de dentro de uma haste escura.",
                "O cabo curvo parecia inofensivo ate revelar outra peca.",
            ],
            [
                "O objeto era uma peca unica e nao escondia nada por dentro.",
                "Nao havia cabo curvo, bainha ou aparencia de bengala.",
            ]),
        ["Machado Cerimonial"] = new(
            [
                "Uma lamina larga e curva surgiu cercada por brilho violeta.",
                "Gravuras elaboradas cobriam uma cabeca metalica pesada.",
            ],
            [
                "Nao havia lamina larga, curva ou cabeca pesada.",
                "A peca era simples demais para objeto cerimonial.",
            ]),
        ["Fio de Piano"] = new(
            [
                "Um arco metalico quase invisivel cortou a luz por um instante.",
                "Dois pequenos cabos pareciam unidos por algo fino e brilhante.",
            ],
            [
                "O objeto era grosso e facil de enxergar mesmo longe da luz.",
                "Nao havia dois cabos pequenos nem fio ligando as extremidades.",
            ]),
    };

    public static string? PickSuspicion(Card card, Random random) =>
        Pick(card, lines => lines.Suspicion, random);

    public static string? PickInnocence(Card card, Random random) =>
        Pick(card, lines => lines.Innocence, random);

    private static string? Pick(Card card, Func<HintLines, string[]> select, Random random)
    {
        if (!LinesByCard.TryGetValue(Normalize(card.Name), out var lines))
        {
            return null;
        }

        var candidates = select(lines);
        return candidates.Length == 0 ? null : candidates[random.Next(candidates.Length)];
    }

    private static string Normalize(string value) =>
        value
            .Replace("á", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("ã", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("â", "a", StringComparison.OrdinalIgnoreCase)
            .Replace("ç", "c", StringComparison.OrdinalIgnoreCase)
            .Replace("é", "e", StringComparison.OrdinalIgnoreCase)
            .Replace("ê", "e", StringComparison.OrdinalIgnoreCase)
            .Replace("í", "i", StringComparison.OrdinalIgnoreCase)
            .Replace("ó", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("õ", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("ô", "o", StringComparison.OrdinalIgnoreCase)
            .Replace("ú", "u", StringComparison.OrdinalIgnoreCase);
}

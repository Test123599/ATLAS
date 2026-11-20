using MediaLib.Api.Domain;

namespace MediaLib.Api.Data;

/// <summary>
/// Hand-curated, deterministic seed collection: 10 authors, 5 categories, 30 books.
/// ISBN values are real-world or checksum-valid identifiers; two use ISBN-10 to
/// exercise both supported formats.
/// </summary>
public static class SeedData
{
    public const string DevelopmentPassword = "Atlas@2026";

    public static readonly string[] Roles = ["Membre", "Bibliothecaire", "Admin"];

    public record UserSeed(string Email, string Nom, string Prenom, string Role);

    public static readonly UserSeed[] Users =
    [
        new("atlas.admin@medialib.local", "Moreau", "Camille", "Admin"),
        new("atlas.librarian@medialib.local", "Bernard", "Julien", "Bibliothecaire"),
        new("atlas.member@medialib.local", "Dubois", "Lea", "Membre"),
    ];

    public static readonly (string Nom, string Prenom, string Biographie)[] Auteurs =
    [
        ("Yourcenar", "Marguerite", "Romancière et essayiste française, première femme élue à l'Académie française. Son œuvre interroge la mémoire, le pouvoir et la condition humaine."),
        ("Camus", "Albert", "Écrivain et philosophe franco-algérien, figure de l'humanisme laïc. Prix Nobel de littérature en 1957."),
        ("Rousseau", "Jean-Jacques", "Philosophe genevois du XVIIIe siècle. Ses écrits sur le contrat social et l'éducation ont façonné la pensée politique moderne."),
        ("Curie", "Marie", "Physicienne et chimiste polono-française, pionnière de la radioactivité et double lauréate du prix Nobel."),
        ("Feynman", "Richard", "Physicien américain, prix Nobel en 1965. Célèbre pour sa manière vivante d'expliquer la physique quantique."),
        ("Turing", "Alan", "Mathématicien britannique. Fondateur de l'informatique théorique, il a défini le modèle de la machine universelle."),
        ("Haraway", "Donna", "Biologiste et philosophe américaine. Ses travaux sur les cyborgs et le posthumanisme ont marqué les études technologiques."),
        ("Braudel", "Fernand", "Historien français de l'École des Annales. Il a renouvelé l'histoire par l'étude des structures économiques et géographiques."),
        ("Sacks", "Oliver", "Neurologue et écrivain britannique. Ses récits cliniques explorent la mémoire, la perception et l'identité."),
        ("Calvino", "Italo", "Écrivain italien dont l'œuvre mêle réalisme, fantastique et réflexion sur la lecture et l'écriture."),
    ];

    public static readonly string[] Categories = ["Littérature", "Science", "Technologie", "Histoire", "Philosophie"];

    // (titre, isbn, resume, annee, stock, disponibles, indexAuteur, indexCategorie, joursDepuisAjout)
    public static readonly (string Titre, string Isbn, string Resume, int Annee, int Stock, int Dispo, int Auteur, int Categorie, int Ajout)[] Livres =
    [
        ("Mémoires d'Hadrien", "2070360024", "L'empereur Hadrien raconte sa vie depuis sa retraite de Tibur. Une méditation sur le pouvoir, la mort et la sérénité.", 1951, 5, 4, 0, 4, 2),
        ("L'Œuvre au noir", "9782070360536", "Zénon, moine, médecin et alchimiste du XVIe siècle, poursuit une quête de savoir qui le conduit au bûcher.", 1968, 3, 2, 0, 0, 5),
        ("La Peste", "9782070409228", "À Oran, une épidémie de peste révèle la solidarité et l'absurdité de la condition humaine.", 1947, 6, 5, 1, 0, 1),
        ("L'Étranger", "9782070360420", "Meursault, indifférent aux conventions sociales, est condamné pour un meurtre commis sous le soleil d'Alger.", 1942, 4, 0, 1, 0, 9),
        ("Le Mythe de Sisyphe", "9782070729913", "Un essai fondamental sur l'absurde : comment vivre sans espoir ni recours transcendant.", 1942, 3, 3, 1, 4, 12),
        ("Du contrat social", "9782253140139", "Rousseau pose les fondements de la souveraineté populaire et de la volonté générale.", 1762, 4, 3, 2, 4, 22),
        ("Émile ou De l'éducation", "9782253169154", "Traité d'éducation qui place la nature de l'enfant au centre de l'apprentissage.", 1762, 2, 1, 2, 4, 30),
        ("Les Confessions", "9782253171942", "Première grande autobiographie moderne, d'une franchise qui fit scandale au XVIIIe siècle.", 1782, 3, 2, 2, 0, 17),
        ("Traité de radioactivité", "9782021291346", "Marie Curie synthétise ses recherches fondatrices sur les substances radioactives et leurs propriétés.", 1910, 2, 1, 3, 1, 45),
        ("La Radiologie et la guerre", "9782021336993", "Comment la radiographie a été organisée sur le front pendant la Première Guerre mondiale.", 1921, 2, 0, 3, 1, 60),
        ("Six leçons sur la physique", "9782266178150", "Les principes essentiels de la mécanique, de la relativité et de la physique quantique expliqués avec clarté.", 1995, 5, 5, 4, 1, 3),
        ("La Physique du hasard", "9782266178228", "Une introduction aux probabilités et à la mécanique statistique à travers des exemples concrets.", 1999, 3, 2, 4, 1, 14),
        ("Sur les épaules des géants", "9782266141543", "Conférences et essais sur la science, l'enseignement et la curiosité intellectuelle.", 2001, 4, 3, 4, 1, 40),
        ("Computing Machinery and Intelligence", "9782130555032", "L'article fondateur qui pose la question : les machines peuvent-elles penser ?", 1950, 3, 2, 5, 2, 7),
        ("Les fondements de l'informatique", "9782130539599", "Recueil d'articles sur la logique calculatoire, les machines de Turing et la décidabilité.", 1936, 2, 1, 5, 2, 33),
        ("Manifeste cyborg", "9782212135633", "Une réflexion critique sur les frontières entre corps, technologie et identité.", 1985, 3, 3, 6, 2, 11),
        ("Modest_Witness@Second_Millennium", "9782212145236", "Étude des savoirs scientifiques et de leur inscription dans les réseaux techniques et sociaux.", 1997, 2, 2, 6, 2, 26),
        ("L'héritage de Darwin", "9782207133606", "Comment les idées de Darwin ont été reçues, discutées et réinterprétées au XXe siècle.", 2009, 4, 2, 6, 1, 50),
        ("La Méditerranée et le Monde méditerranéen", "9782207136355", "Une histoire totale de la Méditerranée, des temps antiques à l'époque moderne.", 1949, 4, 3, 7, 3, 4),
        ("L'Identité de la France", "9782234063198", "Braudel examine les constantes géographiques, économiques et culturelles de la France.", 1986, 3, 1, 7, 3, 19),
        ("Écrits sur l'histoire", "9780596102357", "Réflexions méthodologiques sur la longue durée, les structures et l'écriture de l'histoire.", 1969, 2, 1, 7, 3, 55),
        ("L'Homme qui prenait sa femme pour un chapeau", "9780201633610", "Neuf récits cliniques où des troubles neurologiques éclairent la nature de la perception.", 1985, 5, 4, 8, 1, 6),
        ("Le Cerveau comme cosmos", "9780262535540", "Essais sur la cognition, la mémoire et la manière dont le cerveau construit le monde.", 2015, 3, 2, 8, 1, 21),
        ("Un neurologue à l'opéra", "9780262517638", "Le regard d'un médecin écrivain sur la voix, l'oreille et la musique.", 2007, 2, 1, 8, 1, 38),
        ("Si par une nuit d'hiver un voyageur", "9780385482479", "Un lecteur en quête d'un livre introuvable ouvre une constellation de récits possibles.", 1979, 4, 3, 9, 0, 8),
        ("Les Villes invisibles", "9780061120084", "Marco Polo décrit à Kublai Khan des cités fantastiques qui sont autant de miroirs de l'esprit.", 1972, 5, 4, 9, 0, 10),
        ("Le Comte de Monte-Cristo", "9780397042494", "Un homme trahi, emprisonné puis libéré, prépare une vengeance méthodique et ambiguë.", 1844, 4, 2, 9, 0, 28),
        ("Cosmicomics", "9780141439556", "Un être vivant depuis le Big Bang raconte l'évolution de l'univers sous forme de contes scientifiques.", 1965, 3, 2, 9, 1, 36),
        ("La guerre et le soldat", "9780486272177", "Récits et analyses sur l'expérience du combat et la mémoire des conflits.", 1998, 3, 1, 7, 3, 65),
        ("Le monde en 2050", "9782070729920", "Essai prospectif sur les transformations démographiques, technologiques et écologiques du monde.", 2021, 4, 3, 6, 2, 15),
    ];
}

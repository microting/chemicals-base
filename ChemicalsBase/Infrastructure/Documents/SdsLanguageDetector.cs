/*
The MIT License (MIT)

Copyright (c) 2007 - 2026 Microting A/S

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Text;
using System.Text.RegularExpressions;

namespace ChemicalsBase.Infrastructure.Documents;

/// <param name="Language">ISO 639-1 code, or null when the text is too short to tell.</param>
/// <param name="Confidence">0..1.</param>
/// <param name="Uncertain">True when staff should check the language; the top language is reported anyway.</param>
public sealed record DetectedLanguage(string? Language, double Confidence, bool Uncertain);

/// <summary>
/// Detects the language of an SDS from the text of its first pages (pages 1–2). The one shared copy of the
/// algorithm (F31): chemicals-base itself (Tasks 5 and 9), the service (Task 8) and the plugin (Task 12g backfill)
/// all call this copy:
/// <list type="number">
/// <item>Headings first: the text contains a language's Annex II section-1 title, or its section-1 heading
/// (the language's HeadingPattern for n = 1, e.g. "punkt 1", "1. szakasz") followed within 200 characters by the
/// title's first three words → that language, confidence 1.0.
/// This is what separates "sv" and "no", which share the word "AVSNITT".</item>
/// <item>Otherwise stop-word profiles: about 30 high-frequency words per language; confidence = hits of the
/// top language / all profile hits.</item>
/// <item>Uncertain = confidence &lt; 0.6, or a second language has more than 0.3 of the hits.</item>
/// <item>Fewer than 20 profile hits → (null, 0, uncertain).</item>
/// </list>
/// A language listed in SdsHeadings.HeadingsUnavailable ("is") can only be found by its profile; it is then
/// always uncertain and its confidence is capped at 0.5.
/// </summary>
public static class SdsLanguageDetector
{
    private const int TitleWindow = 200;
    private const int MinProfileHits = 20;
    private const double MinConfidence = 0.6;
    private const double MaxSecondShare = 0.3;
    private const double NoHeadingsConfidenceCap = 0.5;

    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex NonLetters = new(@"[^\p{L}\p{M}]+", RegexOptions.Compiled);

    // Every word belongs to exactly one profile, and words that are frequent in another language's text are left
    // out (e.g. "av": Swedish and Norwegian; "ja": Estonian and Finnish), so a monolingual text scores near 1.0.
    private static readonly IReadOnlyDictionary<string, HashSet<string>> Profiles = new Dictionary<string, HashSet<string>>
    {
        ["da"] = Words("af mod uden mellem gennem øjne øjnene vand brug blandingen oplysninger egenskaber sundhed opbevares skyl nødvendigt hænder nogle hvad indånding indtagelse undgå læge sikkerhedsdatablad indholdsstoffer omgående selskabet virksomheden kemiske beskyttelseshandsker førstehjælp"),
        ["en"] = Words("the and of with from this that not be are or by if should may which other avoid wash water eyes skin product mixture handling storage keep after such any been has"),
        ["bg"] = Words("и на за от се в с да не при или са е може кожата очите вода сместа веществото раздел информационен лист безопасност опасностите информация идентификация дружеството състав ако които като след"),
        ["cs"] = Words("ve pro nebo při jsou být může kůže směsi oddíl bezpečnostní informace identifikace společnosti složení pokud který která které podle jako není nesmí ochranné nebezpečí přípravku"),
        ["de"] = Words("die und nicht mit von für ist oder bei auf das ein eine einer werden wird sind sich nach aus kann augen wasser gemisch gefahren angaben vermeiden zur zum durch sowie"),
        ["el"] = Words("και του της των το τη την σε με για από ή να δεν είναι μπορεί δέρμα μάτια νερό μείγματος ουσίας τμήμα δελτίο δεδομένων ασφαλείας κινδύνων πληροφορίες εταιρείας σύνθεση εάν στο στην"),
        ["es"] = Words("el los las y puede debe piel ojos agua mezcla producto sustancia sección datos seguridad peligros cuando también pueden inhalación ingestión información identificación sociedad composición otros estos hay siguientes"),
        ["et"] = Words("või kui mis ning nagu peab võib nahka silmi vett segu jagu ohutuskaart ohtude teave identifitseerimine ettevõtja koostis tuleb pärast puhul korral vältida kokkupuude ainult samuti üle seda selle"),
        ["fi"] = Words("tai kun jos että joka sekä myös ovat voi saa ihoa silmiin vedellä seoksen aineen käyttöturvallisuustiedote vaaran tiedot tunnistetiedot yrityksen koostumus ainesosista tulee jälkeen välttää kanssa mukaan tämä"),
        ["fr"] = Words("le les des du est pour dans une sur avec pas sont être ou au aux cette qui peut peau yeux eau mélange produit rubrique fiche données sécurité lors éviter doit"),
        ["hr"] = Words("ili kao koji koja koje može očiju vodom smjese tvari odjeljak sigurnosno tehnički opasnosti društvu poduzeću sastav također prema treba nije nakon mjere zaštite prve pomoći ukoliko odnosno"),
        ["hu"] = Words("az és nem vagy egy hogy kell lehet bőrrel szembe vízzel keverék anyag szakasz biztonsági adatlap veszély információk azonosítása vállalat összetétel esetén után szerint alatt mint pedig csak ezt"),
        ["is"] = Words("ekki að fyrir með frá eða þegar þetta þessi vera eru hefur getur skulu húð augu vatni blöndu efnið efnisins öryggisblað upplýsingar hættu fyrirtækið samsetning notkun eftir varúðarráðstafanir innöndun"),
        ["it"] = Words("il della delle dei degli che sono essere può pelle occhi acqua miscela prodotto sostanza sezione scheda dati sicurezza pericoli anche nel nella gli alla alle sul dal inalazione informazioni identificazione società composizione devono"),
        ["lt"] = Words("yra kad iš į nuo gali turi odos akių vandeniu mišinio medžiagos skirsnis saugos duomenų lapas pavojai informacija identifikavimas įmonės sudėtis jei kaip taip pat apie kuris arba"),
        ["lv"] = Words("ar uz vai kas ādu acīm ūdeni maisījuma vielas iedaļa drošības datu lapa bīstamības informācija identificēšana uzņēmuma sastāvs pēc kā arī tikai nav šo tiek jāizvairās"),
        ["nl"] = Words("het een van niet met voor zijn wordt worden bij ook dit deze uit naar aan door mengsel ogen huid gevaren gebruik bevat maatregelen onderneming veiligheidsinformatieblad indien tijdens moet geen"),
        ["no"] = Words("etter uten mellom gjennom øynene øyne vann bruk stoffblandingen opplysninger helse oppbevares skyll nødvendig noen hva dersom unngå lege sikkerhetsdatablad innånding svelging tiltak utslipp sammensetning selskapet foretaket kjemiske eksponeringskontroll"),
        ["pl"] = Words("w oraz nie się jest lub przez dla może należy skóry oczu wodą mieszaniny substancji sekcja charakterystyki zagrożeń informacje identyfikacja przedsiębiorstwa skład jeżeli jeśli są być który która które"),
        ["pt"] = Words("os não uma é são pode pele olhos água mistura produto substância secção dados segurança perigos também nos ao pela pelo às inalação ingestão informação identificação composição sociedade em deve"),
        ["ro"] = Words("și şi în pentru sau cu din care nu pe prin sunt poate trebuie pielea ochii apă amestecul produsul substanța secțiunea fișa securitate pericole informații identificarea societății compoziție ale"),
        ["sk"] = Words("alebo sa pre sú byť môže pokožky zmesi oddiel bezpečnostných údajov informácie identifikácia spoločnosti zloženie ak ktorý ktorá ktoré tiež podľa vo zo karty nebezpečenstvo"),
        ["sl"] = Words("ki so lahko vodo snovi oddelek varnostni nevarnosti družbe sestava če kot tudi ter skladu uporabo ukrepi prvo pomoč zmes podjetja mora kožo oziroma uporaba"),
        ["sv"] = Words("och inte är för att från vid ämnet blandningen bolaget företaget ögonen vatten användning uppgifter hälsa förvaras behållaren skölj nödvändigt några vad undvik läkare säkerhetsdatablad inandning förtäring åtgärder mycket också endast enligt"),
    };

    private static readonly Lazy<IReadOnlyList<Heading>> Headings = new(() => SdsHeadings.All
        .Where(kv => kv.Value.HasHeadings)
        .Select(kv => new Heading(kv.Key, Normalize(kv.Value.Section1Word!), Normalize(kv.Value.Section1Title!)))
        .ToList());

    public static DetectedLanguage Detect(string text)
    {
        var normalized = Normalize(text);
        if (normalized == "")
        {
            return new DetectedLanguage(null, 0, true);
        }

        return ByHeading(normalized) ?? ByProfile(normalized);
    }

    private static DetectedLanguage? ByHeading(string text)
    {
        var found = Headings.Value
            .Select(h => (h.Language, Position: h.FindIn(text)))
            .Where(m => m.Position >= 0)
            .OrderBy(m => m.Position)
            .ToList();

        return found.Count switch
        {
            0 => null,
            1 => new DetectedLanguage(found[0].Language, 1.0, false),
            // Headings of several languages (a multi-language SDS): report the first one, but flag it.
            _ => new DetectedLanguage(found[0].Language, 1.0 / found.Count, true),
        };
    }

    private static DetectedLanguage ByProfile(string text)
    {
        var hits = Profiles.Keys.ToDictionary(code => code, _ => 0);
        foreach (var word in NonLetters.Split(text))
        {
            foreach (var (code, profile) in Profiles)
            {
                if (profile.Contains(word))
                {
                    hits[code]++;
                }
            }
        }

        var total = hits.Values.Sum();
        if (total < MinProfileHits)
        {
            return new DetectedLanguage(null, 0, true);
        }

        var ranked = hits.OrderByDescending(kv => kv.Value).ToList();
        var top = ranked[0];
        var confidence = (double)top.Value / total;
        var uncertain = confidence < MinConfidence || (double)ranked[1].Value / total > MaxSecondShare;

        if (SdsHeadings.For(top.Key)?.HasHeadings != true)
        {
            return new DetectedLanguage(top.Key, Math.Min(confidence, NoHeadingsConfidenceCap), true);
        }

        return new DetectedLanguage(top.Key, confidence, uncertain);
    }

    /// <summary>NFC, lower case, final sigma folded to σ (upper-case Greek lowers to σ), whitespace collapsed.</summary>
    private static string Normalize(string text) =>
        Whitespace.Replace(text.Normalize(NormalizationForm.FormC).ToLowerInvariant().Replace('ς', 'σ'), " ").Trim();

    private static HashSet<string> Words(string words) =>
        [.. Normalize(words).Split(' ')];

    private sealed record Heading(string Language, string Word, string Title)
    {
        private readonly string _titleStart = string.Join(' ', Title.Split(' ').Take(3));

        /// <summary>Position of the heading in the text, or -1.</summary>
        public int FindIn(string text)
        {
            var position = text.IndexOf(Title, StringComparison.Ordinal);
            if (position >= 0)
            {
                return position;
            }

            for (var at = text.IndexOf(Word, StringComparison.Ordinal); at >= 0;
                 at = text.IndexOf(Word, at + 1, StringComparison.Ordinal))
            {
                var after = at + Word.Length;
                var window = text.Substring(after, Math.Min(TitleWindow + _titleStart.Length, text.Length - after));
                if (window.Contains(_titleStart, StringComparison.Ordinal))
                {
                    return at;
                }
            }

            return -1;
        }
    }
}

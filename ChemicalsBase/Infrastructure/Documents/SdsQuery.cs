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
namespace ChemicalsBase.Infrastructure.Documents;

/// <summary>
/// What the SDS harvester (service Task 8) and the staff bulk-upload matcher (plugin Task 12j) look for: one chemical,
/// by name, reg.nr. and trade names, in a target language.
/// </summary>
/// <param name="ChemicalId">The chemical the SDS is for.</param>
/// <param name="Name">Product name ("Kvit Q Myrelokkedåse").</param>
/// <param name="RegistrationNo">Danish reg.nr. ("579-14", "364-73-2-2").</param>
/// <param name="TradeNames">Other names the product is sold under (BMD AdditionalTradeNames).</param>
/// <param name="AuthorisationHolderId">The authorisation holder, when known.</param>
public sealed record SdsQuery(
    int ChemicalId,
    string Name,
    string RegistrationNo,
    IReadOnlyList<string> TradeNames,
    int? AuthorisationHolderId)
{
    /// <summary>ISO 639-1 code of the wanted SDS language. Non-positional on purpose (F33).</summary>
    public string TargetLanguage { get; init; } = "da";
}

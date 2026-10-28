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

using ChemicalsBase.Infrastructure.Documents;

namespace ChemicalBase.Tests;

/// <summary>An in-memory IChemicalBlobStore that counts uploads. CI never talks to S3.</summary>
public sealed class InMemoryBlobStore : IChemicalBlobStore
{
    public Dictionary<string, byte[]> Blobs { get; } = new();

    public int PutCount { get; private set; }

    /// <summary>Runs once, at the start of the next PutAsync: lets a test act between a writer's lookup and insert.</summary>
    public Func<Task>? BeforeNextPut { get; set; }

    public async Task PutAsync(Stream content, string key)
    {
        if (BeforeNextPut is { } hook)
        {
            BeforeNextPut = null;
            await hook();
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer);
        Blobs[key] = buffer.ToArray();
        PutCount++;
    }

    public Task<bool> ExistsAsync(string key) => Task.FromResult(Blobs.ContainsKey(key));

    public Task<byte[]?> GetAsync(string key) =>
        Task.FromResult(Blobs.TryGetValue(key, out var bytes) ? bytes : null);
}

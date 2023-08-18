/*
The MIT License (MIT)

Copyright (c) 2007 - 2022 Microting A/S

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

using ChemicalsBase.Infrastructure;
using ChemicalsBase.Infrastructure.Data.Factories;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Testcontainers.MariaDb;

namespace ChemicalBase.Tests
{
    [TestFixture]
    public abstract class DbTestFixture
    {
        private ChemicalsDbContext? _dbContext;
        private const string DatabaseName = "chemical-base-tests";

        private readonly MariaDbContainer _mariadbTestcontainer = new MariaDbBuilder()
            .WithDatabase(
                "chemical-base-tests").WithUsername("bla").WithPassword("secretpassword")
            .Build();

        private void GetContext(string? connectionStr)
        {
            var contextFactory = new ChemicalsContextFactory();
            _dbContext = contextFactory.CreateDbContext(new[] {connectionStr});

            _dbContext?.Database.Migrate();
            _dbContext?.Database.EnsureCreated();
        }

        [SetUp]
        public async Task Setup()
        {

            Console.WriteLine($"{DateTime.Now} : Starting MariaDb Container...");
            await _mariadbTestcontainer.StartAsync();
            Console.WriteLine($"{DateTime.Now} : Started MariaDb Container");

            GetContext(_mariadbTestcontainer.GetConnectionString());

        }

        [TearDown]
        public void TearDown()
        {
            ClearDb();

            _dbContext?.Dispose();
        }

        private void ClearDb()
        {
            var modelNames = new List<string>
            {
                "ActiveSubstances",
                "ActiveSubstanceVersions",
                "Chemicals",
                "ChemicalVersions",
                "ClassificationAndLabelings",
                "ClassificationAndLabelingVersions",
                "AuthorisationHolderVersions",
                "AuthorisationHolders",
                "Addresses",
                "AddressVersions",
                "Clps",
                "ClpVersions",
                "Dpds",
                "DpdVersions",
                "HazardStatements",
                "HazardStatementVersions",
                "LoadTaxes",
                "LoadTaxVersions",
                "Products",
                "ProductVersions",
            };

            var firstRunNotDone = true;

            foreach (var modelName in modelNames)
            {
                try
                {
                    if (firstRunNotDone)
                    {
                        _dbContext?.Database.ExecuteSqlRaw(
                            $"SET FOREIGN_KEY_CHECKS = 0;TRUNCATE `{DatabaseName}`.`{modelName}`");
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message == $"Unknown database '{DatabaseName}'")
                    {
                        firstRunNotDone = false;
                    }
                    else
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        protected virtual void DoSetup()
        {
        }
    }
}
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
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace ChemicalBase.Tests
{
    [TestFixture]
    public abstract class DbTestFixture
    {
        private ChemicalsDbContext? _dbContext;
        private string? _connectionString;
        private const string DatabaseName = "chemical-base-tests";

        private void GetContext(string? connectionStr)
        {
            var contextFactory = new ChemicalsContextFactory();
            _dbContext = contextFactory.CreateDbContext(new[] {connectionStr});

            _dbContext?.Database.Migrate();
            _dbContext?.Database.EnsureCreated();
        }

        [SetUp]
        public void Setup()
        {

            _connectionString =
                @$"Server = localhost; port = 3306; Database = {DatabaseName}; user = root; password = secretpassword; Convert Zero Datetime = true;";

            GetContext(_connectionString);

            _dbContext?.Database.SetCommandTimeout(300);

            try
            {
                ClearDb();
            }
            catch
            {
                _dbContext?.Database.Migrate();
            }

            DoSetup();
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
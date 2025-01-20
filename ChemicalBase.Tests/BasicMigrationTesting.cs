using NUnit.Framework;

namespace ChemicalBase.Tests;

[TestFixture]
public class BasicMigrationTesting  : DbTestFixture
{

    [Test]
    public void CanMigrate()
    {
        Assert.That(true);
    }
}
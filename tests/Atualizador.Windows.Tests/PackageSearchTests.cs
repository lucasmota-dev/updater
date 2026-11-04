using Atualizador.Windows.Models;
using Atualizador.Windows.Services;

namespace Atualizador.Windows.Tests;

[TestClass]
public sealed class PackageSearchTests
{
    // Trecho real da saída de "winget search firefox --source winget".
    private const string FirefoxSearchOutput = """
        Name                                   Id                                Version  Match
        ------------------------------------------------------------------------------------------------
        Mozilla Firefox                        Mozilla.Firefox                   156.0.1
        Mozilla Firefox (pt-BR)                Mozilla.Firefox.pt-BR             156.0.1
        Mozilla Firefox ESR (pt-BR)            Mozilla.Firefox.ESR.pt-BR         153.6.0
        Mozilla Firefox (de)                   Mozilla.Firefox.de                156.0.1
        Ablaze Floorp                          Ablaze.Floorp                     12.18.1  Tag: firefox-fork
        """;

    private static IReadOnlyList<InstallablePackage> FirefoxPackages =>
        WingetOutputParser.ParseSearchResults(FirefoxSearchOutput);

    [TestMethod]
    public void BuildFallbackTerms_UsesLongestWordThenItsBeginning()
    {
        CollectionAssert.AreEqual(new[] { "firefox", "fire", "fir" }, PackageSearch.BuildFallbackTerms("firefox br").ToArray());
        CollectionAssert.AreEqual(new[] { "firf", "fir" }, PackageSearch.BuildFallbackTerms("firfox").ToArray());
        CollectionAssert.AreEqual(new[] { "br" }, PackageSearch.BuildFallbackTerms("br pt").Take(1).ToArray());
    }

    [TestMethod]
    public void Rank_RequiresEveryWordAndAcceptsPartOfTheId()
    {
        var ranked = PackageSearch.Rank("firefox br", FirefoxPackages, requireAllWords: true);

        CollectionAssert.AreEqual(
            new[] { "Mozilla.Firefox.pt-BR", "Mozilla.Firefox.ESR.pt-BR" },
            ranked.Select(package => package.Id).ToArray());
    }

    [TestMethod]
    public void Rank_ToleratesTyposAndIgnoresAccentsAndCase()
    {
        var typo = PackageSearch.Rank("firfox", FirefoxPackages, requireAllWords: true);
        Assert.AreEqual("Mozilla.Firefox", typo[0].Id);

        var accents = PackageSearch.Rank("FÍREFOX ptbr", FirefoxPackages, requireAllWords: true);
        Assert.AreEqual("Mozilla.Firefox.pt-BR", accents[0].Id);
    }

    [TestMethod]
    public void Rank_WithoutRequirementKeepsEverythingAndPutsBestMatchFirst()
    {
        var ranked = PackageSearch.Rank("mozilla firefox", FirefoxPackages, requireAllWords: false);

        Assert.HasCount(FirefoxPackages.Count, ranked);
        Assert.AreEqual("Mozilla.Firefox", ranked[0].Id);
        Assert.AreEqual("Ablaze.Floorp", ranked[^1].Id);
    }

    [TestMethod]
    public async Task SearchAsync_FallsBackToShorterTermWhenExactSearchFindsNothing()
    {
        var notFound = new ProcessExecutionResult(true, -1978335212, "No package found matching input criteria.", string.Empty, false, false, null);
        var found = new ProcessExecutionResult(true, 0, FirefoxSearchOutput, string.Empty, false, false, null);
        var executor = new FakeProcessExecutor([notFound, found]);
        var service = new WingetService(executor, new FakeLocator("winget.exe"));

        var result = await service.SearchAsync("firefox br");

        Assert.AreEqual(WingetSearchStatus.Success, result.Status);
        Assert.HasCount(2, result.Packages);
        Assert.AreEqual("firefox br", executor.Requests[0].Arguments[1]);
        Assert.AreEqual("firefox", executor.Requests[1].Arguments[1]);
        StringAssert.Contains(result.Message, "parecido");
    }

    [TestMethod]
    public async Task SearchAsync_StopsAfterFallbackLimit()
    {
        var notFound = new ProcessExecutionResult(true, -1978335212, "No package found matching input criteria.", string.Empty, false, false, null);
        var executor = new FakeProcessExecutor(Enumerable.Repeat(notFound, 1 + PackageSearch.MaxFallbackTerms).ToArray());
        var service = new WingetService(executor, new FakeLocator("winget.exe"));

        var result = await service.SearchAsync("aplicativoinexistente");

        Assert.AreEqual(WingetSearchStatus.NoPackagesFound, result.Status);
        Assert.IsLessThanOrEqualTo(1 + PackageSearch.MaxFallbackTerms, executor.Requests.Count);
    }
}

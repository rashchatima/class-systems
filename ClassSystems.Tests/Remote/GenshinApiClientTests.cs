using System.Net;
using ClassSystems.Remote;
using ClassSystems.Remote.Models;
using ClassSystems.Tests.Doubles;

namespace ClassSystems.Tests.Remote;

/// <summary>
/// The HTTP client of the game database, run against saved real answers instead of the Internet.
/// </summary>
public class GenshinApiClientTests(GenshinApiFixture fixture) : IClassFixture<GenshinApiFixture>
{
    [Fact]
    public async Task GetCharacter_ParsesTheAnswer()
    {
        // Arrange
        var (client, handler) = fixture.CreateClient();

        // Act
        RemoteCharacter diluc = await client.GetCharacterAsync("diluc");

        // Assert
        Assert.Equal("diluc", diluc.Id);
        Assert.Equal("Diluc", diluc.Name);
        Assert.Equal("The Dark Side of Dawn", diluc.Title);
        Assert.Equal("Pyro", diluc.Vision);
        Assert.Equal("Claymore", diluc.Weapon);
        Assert.Equal("Mondstadt", diluc.Nation);
        Assert.Equal(5, diluc.Rarity);
        Assert.Equal(new[] { "Tempered Sword", "Searing Onslaught", "Dawn" }, diluc.SkillTalents!.Select(t => t.Name));
        Assert.Equal(new[] { "NORMAL_ATTACK", "ELEMENTAL_SKILL", "ELEMENTAL_BURST" }, diluc.SkillTalents!.Select(t => t.Type));
        Assert.Equal(new Uri("https://genshin.jmp.blue/characters/diluc"), Assert.Single(handler.Requests));
    }

    [Fact]
    public async Task GetCharacter_NormalizesTheId()
    {
        var (client, handler) = fixture.CreateClient();

        await client.GetCharacterAsync("  Diluc ");

        Assert.Equal("/characters/diluc", handler.Requests.Single().AbsolutePath);
    }

    [Fact]
    public async Task GetWeapon_ParsesTheAnswer()
    {
        var (client, _) = fixture.CreateClient();

        RemoteWeapon bow = await client.GetWeaponAsync("amos-bow");

        Assert.Equal("Amos' Bow", bow.Name);
        Assert.Equal("Bow", bow.Type);
        Assert.Equal(5, bow.Rarity);
        Assert.Equal(46, bow.BaseAttack);
        Assert.Equal("ATK", bow.SubStat);
        Assert.Equal("Strong-Willed", bow.PassiveName);
    }

    [Fact]
    public async Task Lists_AreReadWithOneRequestEach()
    {
        // Arrange
        var (client, handler) = fixture.CreateClient();

        // Act
        IReadOnlyList<RemoteCharacter> characters = await client.GetCharactersAsync();
        IReadOnlyList<RemoteWeapon> weapons = await client.GetWeaponsAsync();

        // Assert
        Assert.Equal(new[] { "diluc", "xiangling" }, characters.Select(c => c.Id));
        Assert.Equal(3, weapons.Count);
        Assert.Equal(new[] { "/characters/all", "/weapons/all" }, handler.Requests.Select(uri => uri.AbsolutePath));
    }

    [Fact]
    public async Task UnknownId_ThrowsNotFound()
    {
        var (client, _) = fixture.CreateClient();

        var error = await Assert.ThrowsAsync<RemoteDataException>(() => client.GetCharacterAsync("nobody"));

        Assert.Contains("characters/nobody", error.Message);
        Assert.Contains("not found", error.Message);
    }

    [Fact]
    public async Task NetworkFailure_IsReportedAsRemoteDataError()
    {
        // Arrange
        var (client, handler) = fixture.CreateClient();
        handler.Failure = new HttpRequestException("No such host is known.");

        // Act
        var error = await Assert.ThrowsAsync<RemoteDataException>(() => client.GetWeaponsAsync());

        // Assert
        Assert.Contains("not available", error.Message);
        Assert.IsType<HttpRequestException>(error.InnerException);
    }

    [Fact]
    public async Task BrokenJson_IsReportedAsRemoteDataError()
    {
        var (client, handler) = fixture.CreateClient();
        handler.Forced = (HttpStatusCode.OK, "{ this is not json");

        var error = await Assert.ThrowsAsync<RemoteDataException>(() => client.GetCharacterAsync("diluc"));

        Assert.Contains("unexpected format", error.Message);
    }

    [Fact]
    public async Task ServerError_IsReportedWithItsStatus()
    {
        var (client, handler) = fixture.CreateClient();
        handler.Forced = (HttpStatusCode.InternalServerError, "oops");

        var error = await Assert.ThrowsAsync<RemoteDataException>(() => client.GetCharacterAsync("diluc"));

        Assert.Contains("500", error.Message);
    }

    [Fact]
    public void RemoteDataException_IsAnInvalidOperation_SoTheShellReportsIt()
    {
        Assert.IsAssignableFrom<InvalidOperationException>(new RemoteDataException("x"));
    }
}

/// <summary>
/// The caching decorator of the game database.
/// </summary>
public class CachedGameDatabaseTests(GenshinApiFixture fixture) : IClassFixture<GenshinApiFixture>
{
    private (CachedGameDatabase Cache, MockGameDatabase Inner) Create()
    {
        var inner = new MockGameDatabase(fixture.CreateClient().Client);
        return (new CachedGameDatabase(inner), inner);
    }

    [Fact]
    public async Task List_IsDownloadedOnce()
    {
        var (cache, inner) = Create();

        await cache.GetCharactersAsync();
        await cache.GetCharactersAsync();

        Assert.Equal(1, inner.CharacterListCalls);
    }

    [Fact]
    public async Task SingleObject_IsTakenFromTheDownloadedList()
    {
        // Arrange
        var (cache, inner) = Create();
        await cache.GetWeaponsAsync();

        // Act
        RemoteWeapon bow = await cache.GetWeaponAsync("AMOS-BOW");

        // Assert
        Assert.Equal("Amos' Bow", bow.Name);
        Assert.Equal(0, inner.WeaponCalls);
    }

    [Fact]
    public async Task SingleObject_IsDownloadedOnce()
    {
        var (cache, inner) = Create();

        RemoteCharacter first = await cache.GetCharacterAsync("xiangling");
        RemoteCharacter second = await cache.GetCharacterAsync("xiangling");

        Assert.Same(first, second);
        Assert.Equal(1, inner.CharacterCalls);
    }

    [Fact]
    public async Task Errors_AreNotCached()
    {
        var (cache, inner) = Create();

        await Assert.ThrowsAsync<RemoteDataException>(() => cache.GetCharacterAsync("nobody"));
        await Assert.ThrowsAsync<RemoteDataException>(() => cache.GetCharacterAsync("nobody"));

        Assert.Equal(2, inner.CharacterCalls);
    }
}

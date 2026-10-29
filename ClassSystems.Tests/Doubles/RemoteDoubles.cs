using System.Net;
using System.Text;
using ClassSystems.Remote;
using ClassSystems.Remote.Models;

namespace ClassSystems.Tests.Doubles;

/// <summary>
/// Stub + mock of the network: answers HTTP requests with prepared JSON instead of going to the Internet
/// and remembers every request. It can also pretend that the network is down or the server is broken.
/// </summary>
public class StubHttpHandler(IReadOnlyDictionary<string, string> answers) : HttpMessageHandler
{
    private readonly List<Uri> _requests = new();

    public IReadOnlyList<Uri> Requests => _requests;

    /// <summary>
    /// If set, every request fails with this error, as if the network were down.
    /// </summary>
    public Exception? Failure { get; set; }

    /// <summary>
    /// If set, every answer has this status and body.
    /// </summary>
    public (HttpStatusCode Status, string Body)? Forced { get; set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request.RequestUri!);
        if (Failure != null)
            throw Failure;

        string path = request.RequestUri!.AbsolutePath.Trim('/');
        (HttpStatusCode status, string body) = Forced
            ?? (answers.TryGetValue(path, out string? json)
                ? (HttpStatusCode.OK, json)
                : (HttpStatusCode.NotFound, $"{{\"error\":\"Entity {path} for language en not found\"}}"));

        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
    }
}

/// <summary>
/// Fixture: loads real answers of https://genshin.jmp.blue saved in Remote/Samples once
/// and gives every test a fresh client that reads them through <see cref="StubHttpHandler"/>.
/// </summary>
/// <remarks>
/// Samples: characters diluc (5★ Pyro, Claymore) and xiangling (4★ Pyro, Polearm);
/// weapons wolf-s-gravestone (5★ Claymore), amos-bow (5★ Bow), dull-blade (1★ Sword, no passive).
/// </remarks>
public sealed class GenshinApiFixture
{
    public GenshinApiFixture()
    {
        static string Read(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Remote", "Samples", name + ".json"));

        string diluc = Read("diluc"), xiangling = Read("xiangling");
        string wolf = Read("wolf-s-gravestone"), amos = Read("amos-bow"), dull = Read("dull-blade");

        Answers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["characters/diluc"] = diluc,
            ["characters/xiangling"] = xiangling,
            ["characters/all"] = $"[{diluc},{xiangling}]",
            ["weapons/wolf-s-gravestone"] = wolf,
            ["weapons/amos-bow"] = amos,
            ["weapons/dull-blade"] = dull,
            ["weapons/all"] = $"[{wolf},{amos},{dull}]",
        };
    }

    public IReadOnlyDictionary<string, string> Answers { get; }

    /// <summary>
    /// A client over a new stub handler; the handler is returned too, to configure it or check the requests.
    /// </summary>
    public (GenshinApiClient Client, StubHttpHandler Handler) CreateClient()
    {
        var handler = new StubHttpHandler(Answers);
        return (new GenshinApiClient(new HttpClient(handler)), handler);
    }

    public RemoteCharacter Character(string id) => CreateClient().Client.GetCharacterAsync(id).GetAwaiter().GetResult();

    public RemoteWeapon Weapon(string id) => CreateClient().Client.GetWeaponAsync(id).GetAwaiter().GetResult();
}

/// <summary>
/// Mock of the game database: passes calls to another database and counts them.
/// </summary>
public class MockGameDatabase(IGameDatabase inner) : IGameDatabase
{
    public int CharacterListCalls { get; private set; }

    public int WeaponListCalls { get; private set; }

    public int CharacterCalls { get; private set; }

    public int WeaponCalls { get; private set; }

    public Task<IReadOnlyList<RemoteCharacter>> GetCharactersAsync(CancellationToken cancellation = default)
    {
        CharacterListCalls++;
        return inner.GetCharactersAsync(cancellation);
    }

    public Task<IReadOnlyList<RemoteWeapon>> GetWeaponsAsync(CancellationToken cancellation = default)
    {
        WeaponListCalls++;
        return inner.GetWeaponsAsync(cancellation);
    }

    public Task<RemoteCharacter> GetCharacterAsync(string id, CancellationToken cancellation = default)
    {
        CharacterCalls++;
        return inner.GetCharacterAsync(id, cancellation);
    }

    public Task<RemoteWeapon> GetWeaponAsync(string id, CancellationToken cancellation = default)
    {
        WeaponCalls++;
        return inner.GetWeaponAsync(id, cancellation);
    }
}

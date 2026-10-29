using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClassSystems.Remote.Models;

namespace ClassSystems.Remote;

/// <summary>
/// Reads characters and weapons of Genshin Impact from the public API https://genshin.jmp.blue
/// (project https://github.com/genshindev/api) with the standard <see cref="HttpClient"/>.
/// </summary>
/// <remarks>
/// The client is given in the constructor, so a test can pass one with a fake message handler
/// and check the requests without going to the Internet.
/// </remarks>
public class GenshinApiClient : IGameDatabase
{
    /// <summary>
    /// The address of the public service.
    /// </summary>
    public static readonly Uri DefaultAddress = new("https://genshin.jmp.blue/");

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;

    /// <param name="http">The HTTP client; if it has no base address, <see cref="DefaultAddress"/> is used.</param>
    public GenshinApiClient(HttpClient http)
    {
        _http = http;
        _http.BaseAddress ??= DefaultAddress;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<RemoteCharacter>> GetCharactersAsync(CancellationToken cancellation = default)
        => GetAsync<IReadOnlyList<RemoteCharacter>>("characters/all", cancellation);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RemoteWeapon>> GetWeaponsAsync(CancellationToken cancellation = default)
        => GetAsync<IReadOnlyList<RemoteWeapon>>("weapons/all", cancellation);

    /// <inheritdoc/>
    public Task<RemoteCharacter> GetCharacterAsync(string id, CancellationToken cancellation = default)
        => GetAsync<RemoteCharacter>($"characters/{Key(id)}", cancellation);

    /// <inheritdoc/>
    public Task<RemoteWeapon> GetWeaponAsync(string id, CancellationToken cancellation = default)
        => GetAsync<RemoteWeapon>($"weapons/{Key(id)}", cancellation);

    // Ids in addresses are lower-case; the user may type "Diluc " as well.
    private static string Key(string id) => Uri.EscapeDataString(id.Trim().ToLowerInvariant());

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellation)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(path, cancellation);
        }
        catch (HttpRequestException error)
        {
            throw new RemoteDataException($"The game database is not available: {error.Message}", error);
        }
        catch (TaskCanceledException error) when (!cancellation.IsCancellationRequested)
        {
            throw new RemoteDataException("The game database did not answer in time.", error);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new RemoteDataException($"\"{Uri.UnescapeDataString(path)}\" was not found in the game database.");

            if (!response.IsSuccessStatusCode)
                throw new RemoteDataException($"The game database answered {(int)response.StatusCode} {response.ReasonPhrase}.");

            try
            {
                return await response.Content.ReadFromJsonAsync<T>(Options, cancellation)
                       ?? throw new RemoteDataException($"The game database sent an empty answer for \"{path}\".");
            }
            catch (JsonException error)
            {
                throw new RemoteDataException($"The game database sent \"{path}\" in an unexpected format.", error);
            }
        }
    }
}

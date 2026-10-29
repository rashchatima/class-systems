using ClassSystems.Remote.Models;

namespace ClassSystems.Remote;

/// <summary>
/// Remembers what another database has returned, so the same data is downloaded only once per run.
/// </summary>
/// <remarks>
/// A decorator: it implements the same interface and wraps any <see cref="IGameDatabase"/>.
/// Once the full list has been downloaded, single objects are taken from it without new requests.
/// </remarks>
public class CachedGameDatabase : IGameDatabase
{
    private readonly IGameDatabase _inner;
    private readonly Dictionary<string, RemoteCharacter> _characters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RemoteWeapon> _weapons = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<RemoteCharacter>? _allCharacters;
    private IReadOnlyList<RemoteWeapon>? _allWeapons;

    public CachedGameDatabase(IGameDatabase inner)
    {
        _inner = inner;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RemoteCharacter>> GetCharactersAsync(CancellationToken cancellation = default)
    {
        if (_allCharacters == null)
        {
            _allCharacters = await _inner.GetCharactersAsync(cancellation);
            foreach (RemoteCharacter character in _allCharacters)
                _characters[character.Id] = character;
        }

        return _allCharacters;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RemoteWeapon>> GetWeaponsAsync(CancellationToken cancellation = default)
    {
        if (_allWeapons == null)
        {
            _allWeapons = await _inner.GetWeaponsAsync(cancellation);
            foreach (RemoteWeapon weapon in _allWeapons)
                _weapons[weapon.Id] = weapon;
        }

        return _allWeapons;
    }

    /// <inheritdoc/>
    public async Task<RemoteCharacter> GetCharacterAsync(string id, CancellationToken cancellation = default)
    {
        string key = id.Trim();
        if (!_characters.TryGetValue(key, out RemoteCharacter? character))
        {
            character = await _inner.GetCharacterAsync(key, cancellation);
            _characters[key] = character;
        }

        return character;
    }

    /// <inheritdoc/>
    public async Task<RemoteWeapon> GetWeaponAsync(string id, CancellationToken cancellation = default)
    {
        string key = id.Trim();
        if (!_weapons.TryGetValue(key, out RemoteWeapon? weapon))
        {
            weapon = await _inner.GetWeaponAsync(key, cancellation);
            _weapons[key] = weapon;
        }

        return weapon;
    }
}

using ClassSystems.Remote.Models;

namespace ClassSystems.Remote;

/// <summary>
/// A remote database of game characters and weapons.
/// </summary>
/// <remarks>
/// The rest of the program depends only on this interface, so the real web service can be replaced
/// by a cache, a local file or a test double.
/// </remarks>
public interface IGameDatabase
{
    /// <summary>
    /// All characters with full information.
    /// </summary>
    /// <exception cref="RemoteDataException">The database cannot be reached or sent broken data.</exception>
    Task<IReadOnlyList<RemoteCharacter>> GetCharactersAsync(CancellationToken cancellation = default);

    /// <summary>
    /// All weapons with full information.
    /// </summary>
    /// <exception cref="RemoteDataException">The database cannot be reached or sent broken data.</exception>
    Task<IReadOnlyList<RemoteWeapon>> GetWeaponsAsync(CancellationToken cancellation = default);

    /// <summary>
    /// One character by its id, for example <c>diluc</c>.
    /// </summary>
    /// <exception cref="RemoteDataException">There is no such character, or the database is not available.</exception>
    Task<RemoteCharacter> GetCharacterAsync(string id, CancellationToken cancellation = default);

    /// <summary>
    /// One weapon by its id, for example <c>amos-bow</c>.
    /// </summary>
    /// <exception cref="RemoteDataException">There is no such weapon, or the database is not available.</exception>
    Task<RemoteWeapon> GetWeaponAsync(string id, CancellationToken cancellation = default);
}

/// <summary>
/// The remote data could not be received: the service is not available, the object does not exist
/// or the answer has an unexpected format.
/// </summary>
/// <remarks>
/// Derives from <see cref="InvalidOperationException"/>, so the command interface reports it like any other
/// failed command instead of crashing.
/// </remarks>
public class RemoteDataException : InvalidOperationException
{
    public RemoteDataException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

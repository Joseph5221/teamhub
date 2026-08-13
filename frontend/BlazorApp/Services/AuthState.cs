using BlazorApp.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace BlazorApp.Services;

/// <summary>
/// Per-circuit session state, backed by protected browser localStorage so a page
/// reload (which starts a new circuit) doesn't force a re-login.
/// </summary>
public class AuthState
{
    private const string StorageKey = "teamhub.auth";

    private readonly ProtectedLocalStorage _storage;
    private bool _initialized;

    public string? Token { get; private set; }
    public string? Email { get; private set; }
    public string? Name { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

    public AuthState(ProtectedLocalStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Restores auth state from browser storage. Must be called after the interactive
    /// circuit has connected (JS interop is unavailable during static prerendering).
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        if (_initialized)
        {
            return IsAuthenticated;
        }

        _initialized = true;

        try
        {
            var result = await _storage.GetAsync<StoredAuth>(StorageKey);
            if (result.Success && result.Value is not null)
            {
                Token = result.Value.Token;
                Email = result.Value.Email;
                Name = result.Value.Name;
            }
        }
        catch (InvalidOperationException)
        {
            // JS interop not available yet (prerendering) — treat as unauthenticated.
        }

        return IsAuthenticated;
    }

    public async Task SetAuthAsync(AuthResponse auth)
    {
        Token = auth.Token;
        Email = auth.Email;
        Name = auth.Name;
        _initialized = true;

        await _storage.SetAsync(StorageKey, new StoredAuth(auth.Token, auth.Email, auth.Name));
    }

    public async Task ClearAsync()
    {
        Token = null;
        Email = null;
        Name = null;

        await _storage.DeleteAsync(StorageKey);
    }

    private record StoredAuth(string Token, string Email, string Name);
}

using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Plugins;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;

namespace EmbyBadges.Enhancer;

/// <summary>
/// Écoute les événements Emby qui changent le contenu ou l'état de lecture des playlists et marque l'index
/// <see cref="SyncDetector"/> comme périmé (reconstruction en tâche de fond après ~30 s).
/// Les gestionnaires ne font que positionner un drapeau : aucun appel bloquant, aucune exception remontée.
/// </summary>
public class PlaylistEventsEntryPoint : IServerEntryPoint
{
    private readonly IUserDataManager _userDataManager;
    private readonly IPlaylistManager _playlistManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;
    private bool _subscribed;

    public PlaylistEventsEntryPoint(
        ILogManager logManager,
        IUserDataManager userDataManager,
        IPlaylistManager playlistManager,
        ILibraryManager libraryManager)
    {
        _logger = logManager.GetLogger(nameof(PlaylistEventsEntryPoint));
        _userDataManager = userDataManager;
        _playlistManager = playlistManager;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Une sauvegarde de données utilisateur invalide l'index seulement pour une fin de lecture ou un marquage lu/non lu
    /// d'un média présent dans une playlist (PlaybackStart / PlaybackProgress arrivent toutes les quelques secondes : ignorés).
    /// </summary>
    internal static bool ShouldInvalidateOnUserData(UserDataSaveReason reason, long itemId, IEnumerable<long>? members)
        => (reason == UserDataSaveReason.PlaybackFinished || reason == UserDataSaveReason.TogglePlayed)
           && members != null && members.Contains(itemId);

    public void Run()
    {
        try
        {
            _userDataManager.UserDataSaved += OnUserDataSaved;
            _playlistManager.PlaylistItemsAdded += OnPlaylistChanged;
            _playlistManager.PlaylistItemsRemoved += OnPlaylistChanged;
            _libraryManager.ItemAdded += OnItemAdded;
            _libraryManager.ItemRemoved += OnItemRemoved;
            _subscribed = true;
            _logger.Debug("EmbyBadges: PlaylistEventsEntryPoint actif (abonnements posés)");
        }
        catch (Exception ex)
        {
            _logger.Warn("EmbyBadges: abonnement aux événements impossible ({0})", ex.Message);
            Unsubscribe(); // abonnement à mi-chemin : on retire ce qui a déjà été posé
        }
    }

    public void Dispose()
    {
        Unsubscribe();
    }

    /// <summary>Retire tous les gestionnaires, chacun isolément (le retrait d'un gestionnaire non posé est sans effet).</summary>
    private void Unsubscribe()
    {
        SafeRemove(() => _userDataManager.UserDataSaved -= OnUserDataSaved);
        SafeRemove(() => _playlistManager.PlaylistItemsAdded -= OnPlaylistChanged);
        SafeRemove(() => _playlistManager.PlaylistItemsRemoved -= OnPlaylistChanged);
        SafeRemove(() => _libraryManager.ItemAdded -= OnItemAdded);
        SafeRemove(() => _libraryManager.ItemRemoved -= OnItemRemoved);
        _subscribed = false;
    }

    private void SafeRemove(Action remove)
    {
        try { remove(); }
        catch (Exception ex) { _logger.Debug("EmbyBadges: désabonnement en échec ({0})", ex.Message); }
    }

    private void OnUserDataSaved(object? sender, UserDataSaveEventArgs e)
    {
        try
        {
            var detector = SyncDetector.Current;
            if (detector == null || e.Item == null) return;
            detector.OnUserDataSaved(e.SaveReason, e.Item.InternalId, e.User?.InternalId);
        }
        catch (Exception ex) { _logger.Debug("EmbyBadges: UserDataSaved ignoré ({0})", ex.Message); }
    }

    private void OnPlaylistChanged(object? sender, EventArgs e)
    {
        try { SyncDetector.Current?.MarkDirty(); }
        catch (Exception ex) { _logger.Debug("EmbyBadges: événement playlist ignoré ({0})", ex.Message); }
    }

    private void OnItemAdded(object? sender, ItemChangeEventArgs e)
    {
        try
        {
            if (e.Item is Playlist) SyncDetector.Current?.MarkDirty();
        }
        catch (Exception ex) { _logger.Debug("EmbyBadges: ItemAdded ignoré ({0})", ex.Message); }
    }

    private void OnItemRemoved(object? sender, ItemChangeEventArgs e)
    {
        try
        {
            var detector = SyncDetector.Current;
            if (detector == null || e.Item == null) return;
            if (e.Item is Playlist || detector.IsMember(e.Item.InternalId)) detector.MarkDirty();
        }
        catch (Exception ex) { _logger.Debug("EmbyBadges: ItemRemoved ignoré ({0})", ex.Message); }
    }
}

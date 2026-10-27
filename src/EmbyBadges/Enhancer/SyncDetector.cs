using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;

namespace EmbyBadges.Enhancer;

/// <summary>Types de synchro PlaySync portés par une playlist partagée.</summary>
[Flags]
public enum SyncTypes
{
    None     = 0,
    /// <summary>propager-avancement=OUI — lettre « P ».</summary>
    Position = 1,
    /// <summary>propager-lu=OUI — lettre « L ».</summary>
    Read     = 2,
    /// <summary>remove-si-lu=OUI (effectif : exige propager-lu=OUI) — lettre « S ».</summary>
    Removed  = 4
}

/// <summary>
/// Détecte les médias présents dans une playlist Emby partagée portant une étiquette PlaySync active.
/// Lecture seule via le SDK Emby (aucune dépendance à la DLL PlaySync). Un index mémoire à TTL
/// (5 min) garde le coût par appel en O(1).
/// </summary>
public class SyncDetector
{
    internal static readonly TimeSpan SyncIndexTtl = TimeSpan.FromMinutes(5);

    private static readonly Regex PositionRx = MakeRx("propager-avancement");
    private static readonly Regex ReadRx     = MakeRx("propager-lu");
    private static readonly Regex RemoveRx   = MakeRx("remove-si-lu");

    private static Regex MakeRx(string family) =>
        new(@"^\s*" + Regex.Escape(family) + @"\s*=\s*(NON|OUI)\s*\z",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IItemRepository? _itemRepository;
    private readonly ILogger _logger;

    private readonly object _buildLock = new();
    private int _rebuilding;
    private volatile Snapshot _snapshot = new(DateTime.MinValue, new Dictionary<long, SyncTypes>(), new Dictionary<long, PlaylistInfo>(), false);

    /// <summary>Instantané de l'index : <c>Map</c> = table des médias (v1.3.0, inchangée), <c>Playlists</c> = infos par playlist.</summary>
    private sealed record Snapshot(DateTime At, Dictionary<long, SyncTypes> Map, Dictionary<long, PlaylistInfo> Playlists, bool HadItems);

    /// <summary>Résultat d'une passe de construction.</summary>
    private sealed record BuildResult(Dictionary<long, SyncTypes> Map, Dictionary<long, PlaylistInfo> Playlists, bool HadItems);

    /// <summary>Infos d'une playlist (LOT-1 : partage ; la progression Played/Total reste à 0 jusqu'au LOT-2).</summary>
    internal sealed record PlaylistInfo(bool IsShared, long? OwnerId, SyncTypes OwnTypes, int Played, int Total)
    {
        internal static readonly PlaylistInfo Neutral = new(false, null, SyncTypes.None, 0, 0);
    }

    public SyncDetector(ILibraryManager libraryManager, IUserManager userManager, IItemRepository? itemRepository, ILogger logger)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _itemRepository = itemRepository;
        _logger = logger;
    }

    // ── Fonctions pures ──────────────────────────────────────────────────────

    /// <summary>
    /// Évalue les étiquettes d'une playlist. Famille active = au moins un « fam=OUI » et aucun « fam=NON ».
    /// <c>Removed</c> n'est effectif que si <c>Read</c> l'est aussi.
    /// </summary>
    internal static SyncTypes EvaluateTags(IEnumerable<string>? tags)
    {
        if (tags == null) return SyncTypes.None;
        var list = tags.Where(t => t != null).ToList();

        var result = SyncTypes.None;
        if (IsActive(list, PositionRx)) result |= SyncTypes.Position;
        if (IsActive(list, ReadRx))     result |= SyncTypes.Read;
        if ((result & SyncTypes.Read) != 0 && IsActive(list, RemoveRx)) result |= SyncTypes.Removed;
        return result;
    }

    private static bool IsActive(List<string> tags, Regex rx)
    {
        bool oui = false;
        foreach (var t in tags)
        {
            var m = rx.Match(t);
            if (!m.Success) continue;
            if (m.Groups[1].Value.Equals("NON", StringComparison.OrdinalIgnoreCase)) return false;
            oui = true;
        }
        return oui;
    }

    /// <summary>Partagée = propriétaire (ManageDelete) connu et au moins un autre membre (niveau ≥ Read).</summary>
    internal static bool IsShared(IEnumerable<UserItemShare>? rows)
    {
        if (rows == null) return false;
        long? owner = null;
        var members = new List<long>();
        foreach (var r in rows)
        {
            var level = r.ShareLevel ?? UserItemShareLevel.None;
            if (level < UserItemShareLevel.Read) continue;
            if (level == UserItemShareLevel.ManageDelete && owner == null) owner = r.UserId;
            members.Add(r.UserId);
        }
        return owner != null && members.Any(m => m != owner.Value);
    }

    /// <summary>Propriétaire = première ligne de partage de niveau ManageDelete ; <c>null</c> si introuvable.</summary>
    internal static long? OwnerOf(IEnumerable<UserItemShare>? rows)
    {
        if (rows == null) return null;
        foreach (var r in rows)
            if ((r.ShareLevel ?? UserItemShareLevel.None) == UserItemShareLevel.ManageDelete)
                return r.UserId;
        return null;
    }

    /// <summary>L'index n'est construit que si au moins un des badges qui s'en servent est actif (CA6).</summary>
    internal static bool IndexNeeded(PluginConfiguration config)
        => config.ShowSync || config.ShowPlaylistShare; // LOT-2 : ajouter ShowPlaylistProgress avec son UI

    // ── Index ────────────────────────────────────────────────────────────────

    /// <summary>Types de synchro d'un item (film, épisode, saison, série). <c>None</c> si inconnu.</summary>
    public SyncTypes GetSyncTypes(long internalId)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || !config.ShowSync) return SyncTypes.None; // CA11 : aucun coût

        var snap = EnsureSnapshot(needItems: true);
        return snap.Map.TryGetValue(internalId, out var t) ? t : SyncTypes.None;
    }

    /// <summary>Infos d'une playlist (partage, propriétaire, étiquettes propres). Neutre si inconnue ou index inutile.</summary>
    internal PlaylistInfo GetPlaylistInfo(long internalId)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || !IndexNeeded(config)) return PlaylistInfo.Neutral;

        var snap = EnsureSnapshot();
        return snap.Playlists.TryGetValue(internalId, out var p) ? p : PlaylistInfo.Neutral;
    }

    /// <summary>Renvoie l'instantané courant ; déclenche le premier build (bloquant) ou la reconstruction en tâche de fond.</summary>
    private Snapshot EnsureSnapshot(bool needItems = false)
    {
        var snap = _snapshot;
        // Index construit sans la table Items (ShowSync était faux) puis ShowSync activé : reconstruction
        // forcée, au plus toutes les 10 s pour ne pas boucler si la reconstruction échoue.
        var age = DateTime.UtcNow - snap.At;
        bool missingItems = needItems && !snap.HadItems && snap.At != DateTime.MinValue && age >= TimeSpan.FromSeconds(10);
        if (age >= SyncIndexTtl || missingItems)
        {
            if (snap.At == DateTime.MinValue)
            {
                // Tout premier build : bloquant (un seul thread ; les autres servent l'index vide).
                if (System.Threading.Monitor.TryEnter(_buildLock))
                {
                    try
                    {
                        snap = _snapshot;
                        if (snap.At == DateTime.MinValue) snap = Rebuild(snap);
                    }
                    finally { System.Threading.Monitor.Exit(_buildLock); }
                }
            }
            else if (Interlocked.CompareExchange(ref _rebuilding, 1, 0) == 0)
            {
                // TTL expiré : on sert l'ancien instantané et on reconstruit en tâche de fond.
                var previous = snap;
                Task.Run(() =>
                {
                    try { Rebuild(previous); }
                    catch (Exception ex) { _logger.Warn("SyncIndex : reconstruction en arrière-plan en échec ({0})", ex.Message); }
                    finally { Interlocked.Exchange(ref _rebuilding, 0); }
                });
            }
        }
        return snap;
    }

    private Snapshot Rebuild(Snapshot previous)
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var built = BuildMap();
            var fresh = new Snapshot(DateTime.UtcNow, built.Map, built.Playlists, built.HadItems);
            _snapshot = fresh;
            _logger.Debug("SyncIndex reconstruit : {0} items, {1} playlists, {2} ms", built.Map.Count, built.Playlists.Count, sw.ElapsedMilliseconds);
            return fresh;
        }
        catch (Exception ex)
        {
            _logger.Warn("SyncIndex : échec de la reconstruction ({0}) — index précédent conservé", ex.Message);
            // On repousse l'échéance pour ne pas retenter à chaque appel.
            var kept = new Snapshot(DateTime.UtcNow, previous.Map, previous.Playlists, previous.HadItems);
            _snapshot = kept;
            return kept;
        }
    }

    private BuildResult BuildMap()
    {
        var map = new Dictionary<long, SyncTypes>();
        var infos = new Dictionary<long, PlaylistInfo>();
        var config = Plugin.Instance?.Configuration;
        bool wantItems = config?.ShowSync ?? false; // table Items : badge Synchro des médias (v1.3.0)
        var result = new BuildResult(map, infos, wantItems);
        if (_itemRepository == null) return result;

        var playlists = _libraryManager
            .GetItemList(new InternalItemsQuery { IncludeItemTypes = new[] { "Playlist" }, Recursive = true })
            .OfType<Playlist>()
            .ToList();
        if (playlists.Count == 0) return result;

        // Une seule requête de partages pour toutes les playlists.
        var rows = _itemRepository
            .GetUserItemShares(new UserItemShareQuery { ItemIds = playlists.Select(p => p.InternalId).ToArray() }, CancellationToken.None)
            .ToLookup(r => r.ItemId);

        foreach (var playlist in playlists)
        {
            try
            {
                var shareRows = rows[playlist.InternalId].ToList();
                var own = EvaluateTags(playlist.Tags);
                var shared = IsShared(shareRows);
                infos[playlist.InternalId] = new PlaylistInfo(shared, OwnerOf(shareRows), own, 0, 0);

                // Table Items : playlist partagée ET étiquetée, seulement si ShowSync (inchangé vs v1.3.0).
                if (wantItems && own != SyncTypes.None && shared)
                    foreach (var child in ReadChildren(playlist, shareRows))
                        Add(map, child, own);
            }
            catch (Exception ex)
            {
                _logger.Warn("SyncIndex : playlist « {0} » ignorée ({1})", playlist.Name, ex.Message);
            }
        }
        return result;
    }

    private static void Add(Dictionary<long, SyncTypes> map, BaseItem child, SyncTypes types)
    {
        Or(map, child.InternalId, types);
        // Propagation aux conteneurs : saison puis série (agrégation comme résolution/langues).
        if (child is Episode)
        {
            var season = child.GetParent();
            if (season != null)
            {
                Or(map, season.InternalId, types);
                var series = season.GetParent();
                if (series != null) Or(map, series.InternalId, types);
            }
        }
    }

    private static void Or(Dictionary<long, SyncTypes> map, long id, SyncTypes t)
        => map[id] = (map.TryGetValue(id, out var cur) ? cur : SyncTypes.None) | t;

    /// <summary>Enfants d'une playlist : propriétaire, puis membres, puis sans utilisateur, puis ListIds.</summary>
    private IEnumerable<BaseItem> ReadChildren(Playlist playlist, List<UserItemShare> shareRows)
    {
        var users = shareRows
            .Where(r => (r.ShareLevel ?? UserItemShareLevel.None) >= UserItemShareLevel.Read)
            .OrderByDescending(r => r.ShareLevel ?? UserItemShareLevel.None)
            .Select(r => { try { return _userManager.GetUserById(r.UserId); } catch { return null; } })
            .Where(u => u != null)
            .ToList();

        foreach (var user in users)
        {
            var items = Try(() => playlist.GetChildren(new InternalItemsQuery(user!)));
            if (items.Length > 0) return items;
        }

        var none = Try(() => playlist.GetChildren(new InternalItemsQuery()));
        if (none.Length > 0) return none;

        return Try(() => _libraryManager.GetItemList(new InternalItemsQuery { ListIds = new[] { playlist.InternalId } }));
    }

    private BaseItem[] Try(Func<BaseItem[]> read)
    {
        try { return read() ?? Array.Empty<BaseItem>(); }
        catch (Exception ex)
        {
            _logger.Debug("SyncIndex : lecture des enfants impossible ({0})", ex.Message);
            return Array.Empty<BaseItem>();
        }
    }
}

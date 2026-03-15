using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml.Serialization;

namespace Wow_Launcher
{
    [XmlRoot("manifest")]
    public class UpdateManifest
    {
        [XmlElement("version")]
        public string Version { get; set; }

        [XmlElement("baseUrl")]
        public string BaseUrl { get; set; }

        [XmlElement("launchFile")]
        public string LaunchFile { get; set; }

        [XmlElement("breakingNewsUrl")]
        public string BreakingNewsUrl { get; set; }

        [XmlElement("news")]
        public string NewsContent { get; set; }

        [XmlArray("files")]
        [XmlArrayItem("file")]
        public List<UpdateFileEntry> Files { get; set; }
    }

    public class UpdateFileEntry
    {
        [XmlAttribute("path")]
        public string FilePath { get; set; }

        [XmlAttribute("sha256")]
        public string Sha256 { get; set; }

        [XmlAttribute("size")]
        public long Size { get; set; }
    }

    public class PendingUpdateFile
    {
        public Uri DownloadUri { get; set; }

        public UpdateFileEntry Entry { get; set; }

        public string LocalPath { get; set; }

        public string ReasonKey { get; set; }
    }

    internal sealed class SupportedLanguage
    {
        public SupportedLanguage(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }

        public string Code { get; private set; }

        public string DisplayName { get; private set; }
    }

    [DataContract]
    public class ReleaseNotesFeed
    {
        [DataMember(Name = "latest")]
        public ReleaseNotesEntry Latest { get; set; }

        [DataMember(Name = "history")]
        public List<ReleaseNotesEntry> History { get; set; }
    }

    [DataContract]
    public class RealmStatusResponse
    {
        [DataMember(Name = "player_count")]
        public int PlayerCount { get; set; }

        [DataMember(Name = "max_player_count")]
        public int MaxPlayerCount { get; set; }

        [DataMember(Name = "queued_sessions")]
        public int QueuedSessions { get; set; }

        [DataMember(Name = "uptime_formatted")]
        public string UptimeFormatted { get; set; }
    }

    [DataContract]
    public class ReleaseNotesEntry
    {
        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "created_at")]
        public string CreatedAt { get; set; }

        [DataMember(Name = "created_by")]
        public string CreatedBy { get; set; }

        [DataMember(Name = "release_notes")]
        public string ReleaseNotes { get; set; }
    }

    [DataContract]
    public class GitHubRelease
    {
        [DataMember(Name = "tag_name")]
        public string TagName { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "assets")]
        public List<GitHubReleaseAsset> Assets { get; set; }
    }

    [DataContract]
    public class GitHubReleaseAsset
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "browser_download_url")]
        public string BrowserDownloadUrl { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }
    }

    public class LauncherReleaseAsset
    {
        public Uri DownloadUri { get; set; }

        public string Version { get; set; }
    }

    internal static class AppLocalization
    {
        private const string DefaultLanguageCode = "en";

        private static readonly IReadOnlyList<SupportedLanguage> SupportedLanguages = new List<SupportedLanguage>
        {
            new SupportedLanguage("en", "English"),
            new SupportedLanguage("es", "Español"),
            new SupportedLanguage("de", "Deutsch"),
            new SupportedLanguage("fr", "Français")
        };

        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "en",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["LanguageLabel"] = "Language",
                    ["FormTitleFormat"] = "{0} Launcher",
                    ["SubtitleFormat"] = "Wrath of the Lich King 3.3.5a launcher for patches, realm news, and your next adventure. Launcher v{0}",
                    ["ConnectionGroupFormat"] = "{0} Client",
                    ["ClientHintFormat"] = "Choose your 3.3.5a client folder, sync with {0}, then enter the realm.",
                    ["ClientPath"] = "World of Warcraft path",
                    ["Browse"] = "Browse...",
                    ["CheckUpdates"] = "Check Updates",
                    ["UpdateClient"] = "Update Client",
                    ["EnterWorld"] = "Enter World",
                    ["ClearCache"] = "Clear Cache",
                    ["RealmStatusLabelFormat"] = "{0} STATUS",
                    ["OnlinePlayersLabel"] = "ONLINE PLAYERS",
                    ["NewsGroupFormat"] = "{0} News",
                    ["NewsHintFormat"] = "Breaking news and recent updates from the {0} realm feed.",
                    ["UpdatesGroupFormat"] = "{0} Operations",
                    ["UpdatesHintFormat"] = "Track downloads, pending files, and live launcher activity for {0}.",
                    ["PendingUpdates"] = "Pending Updates",
                    ["ActivityLog"] = "Activity Log",
                    ["RemoteVersionLabelFormat"] = "{0} BUILD",
                    ["StatusLabel"] = "STATUS",
                    ["DefaultNewsTextFormat"] = "Click Check Updates to load the latest {0} news.",
                    ["SelectClientFolderDescription"] = "Select your World of Warcraft 3.3.5a client folder.",
                    ["Ready"] = "Ready",
                    ["RealmStatusChecking"] = "Checking...",
                    ["RealmStatusOnline"] = "Online",
                    ["RealmStatusOffline"] = "Offline",
                    ["RealmStatusUnknown"] = "Unknown",
                    ["LaunchFailedTitle"] = "Launch failed",
                    ["LaunchFileNotFound"] = "Could not find the game executable in the selected folder.",
                    ["MissingClientFolderTitle"] = "Missing client folder",
                    ["MissingClientFolderMessage"] = "Select the local client folder first.",
                    ["CacheNotFoundTitle"] = "Cache not found",
                    ["CacheNotFoundMessage"] = "No Cache folder was found in the selected client directory.",
                    ["ClearCacheTitle"] = "Clear cache",
                    ["ClearCacheConfirm"] = "Delete the Cache folder from the selected client directory?",
                    ["CacheCleared"] = "Cache cleared.",
                    ["CacheClearFailed"] = "Cache clear failed.",
                    ["CheckingForUpdates"] = "Checking for updates...",
                    ["GatheringRealmNews"] = "Gathering realm news...",
                    ["ClientUpToDate"] = "Client is up to date.",
                    ["FilesNeedUpdateFormat"] = "{0} file(s) need to be updated.",
                    ["UpdateCheckFailed"] = "Update check failed.",
                    ["UpdateCheckFailedNews"] = "Unable to load realm news until the manifest can be reached.",
                    ["DownloadingUpdatesFormat"] = "Downloading updates with {0} threads...",
                    ["DownloadedFilesFormat"] = "Downloaded {0}/{1} file(s)...",
                    ["UpdateComplete"] = "Update complete.",
                    ["UpdateFinishedWithRemaining"] = "Update finished with remaining files.",
                    ["UpdateCompleteMessage"] = "Client update finished successfully.",
                    ["UpdateRemainingMessageFormat"] = "Downloads finished, but {0} file(s) still require attention.",
                    ["UpdateCompleteTitle"] = "Update complete",
                    ["UpdateFinishedTitle"] = "Update finished",
                    ["UpdateFailed"] = "Update failed.",
                    ["InvalidManifestUrlTitle"] = "Invalid manifest URL",
                    ["InvalidManifestUrlMessage"] = "The hardcoded manifest URL is invalid.",
                    ["InvalidClientFolderTitle"] = "Invalid client folder",
                    ["InvalidClientFolderMessage"] = "The selected client folder does not exist.",
                    ["LauncherUpdateCheckFailedTitle"] = "Launcher update check failed",
                    ["LauncherUpdateAvailableTitle"] = "Launcher update available",
                    ["LauncherUpdatePromptFormat"] = "A launcher update is available.\r\n\r\nCurrent version: {0}\r\nAvailable version: {1}\r\n\r\nInstall it now? The launcher will restart after the update finishes.",
                    ["UpdatingLauncher"] = "Updating launcher...",
                    ["LauncherUpdateFailed"] = "Launcher update failed.",
                    ["LauncherUpdateStartFailed"] = "The launcher update could not be started.",
                    ["NoRealmNews"] = "No realm news has been published yet.",
                    ["RealmNewsLoadFailed"] = "Realm news could not be loaded right now.",
                    ["BreakingNewsHeading"] = "Breaking News",
                    ["RecentUpdatesHeading"] = "Recent Updates",
                    ["NewsHeadingFormat"] = "{0} News",
                    ["BuildPrefix"] = "Build",
                    ["PublishedPrefix"] = "Published",
                    ["ByPrefix"] = "By",
                    ["PlayersPrefix"] = "Players",
                    ["QueuePrefix"] = "Queue",
                    ["UptimePrefix"] = "Uptime",
                    ["PendingReasonMissing"] = "Missing locally",
                    ["PendingReasonSizeMismatch"] = "Size mismatch",
                    ["PendingReasonHashMismatch"] = "Hash mismatch",
                    ["SelfUpdateInvalidArgumentsMessage"] = "The launcher update arguments are invalid.",
                    ["SelfUpdateFailedTitle"] = "Launcher update failed",
                    ["SelfUpdateFailedMessageFormat"] = "The launcher could not update itself.\r\n\r\n{0}"
                }
            },
            {
                "es",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["LanguageLabel"] = "Idioma",
                    ["FormTitleFormat"] = "Lanzador de {0}",
                    ["SubtitleFormat"] = "Lanzador de Wrath of the Lich King 3.3.5a para parches, noticias del reino y tu próxima aventura. Lanzador v{0}",
                    ["ConnectionGroupFormat"] = "Cliente de {0}",
                    ["ClientHintFormat"] = "Elige tu carpeta del cliente 3.3.5a, sincronízala con {0} y entra al reino.",
                    ["ClientPath"] = "Ruta de World of Warcraft",
                    ["Browse"] = "Examinar...",
                    ["CheckUpdates"] = "Buscar actualizaciones",
                    ["UpdateClient"] = "Actualizar cliente",
                    ["EnterWorld"] = "Entrar al mundo",
                    ["ClearCache"] = "Limpiar caché",
                    ["RealmStatusLabelFormat"] = "ESTADO DE {0}",
                    ["OnlinePlayersLabel"] = "JUGADORES EN LÍNEA",
                    ["NewsGroupFormat"] = "Noticias de {0}",
                    ["NewsHintFormat"] = "Noticias urgentes y actualizaciones recientes del feed del reino de {0}.",
                    ["UpdatesGroupFormat"] = "Operaciones de {0}",
                    ["UpdatesHintFormat"] = "Sigue las descargas, archivos pendientes y la actividad del lanzador de {0}.",
                    ["PendingUpdates"] = "Actualizaciones pendientes",
                    ["ActivityLog"] = "Registro de actividad",
                    ["RemoteVersionLabelFormat"] = "VERSIÓN DE {0}",
                    ["StatusLabel"] = "ESTADO",
                    ["DefaultNewsTextFormat"] = "Haz clic en Buscar actualizaciones para cargar las últimas noticias de {0}.",
                    ["SelectClientFolderDescription"] = "Selecciona la carpeta de tu cliente de World of Warcraft 3.3.5a.",
                    ["Ready"] = "Listo",
                    ["RealmStatusChecking"] = "Comprobando...",
                    ["RealmStatusOnline"] = "En línea",
                    ["RealmStatusOffline"] = "Desconectado",
                    ["RealmStatusUnknown"] = "Desconocido",
                    ["LaunchFailedTitle"] = "No se pudo iniciar",
                    ["LaunchFileNotFound"] = "No se encontró el ejecutable del juego en la carpeta seleccionada.",
                    ["MissingClientFolderTitle"] = "Falta la carpeta del cliente",
                    ["MissingClientFolderMessage"] = "Selecciona primero la carpeta local del cliente.",
                    ["CacheNotFoundTitle"] = "Caché no encontrada",
                    ["CacheNotFoundMessage"] = "No se encontró una carpeta Cache en el directorio del cliente seleccionado.",
                    ["ClearCacheTitle"] = "Limpiar caché",
                    ["ClearCacheConfirm"] = "¿Eliminar la carpeta Cache del directorio del cliente seleccionado?",
                    ["CacheCleared"] = "Caché limpiada.",
                    ["CacheClearFailed"] = "No se pudo limpiar la caché.",
                    ["CheckingForUpdates"] = "Buscando actualizaciones...",
                    ["GatheringRealmNews"] = "Cargando noticias del reino...",
                    ["ClientUpToDate"] = "El cliente está actualizado.",
                    ["FilesNeedUpdateFormat"] = "{0} archivo(s) deben actualizarse.",
                    ["UpdateCheckFailed"] = "Falló la comprobación de actualizaciones.",
                    ["UpdateCheckFailedNews"] = "No se pudieron cargar las noticias del reino hasta que se pueda acceder al manifiesto.",
                    ["DownloadingUpdatesFormat"] = "Descargando actualizaciones con {0} hilos...",
                    ["DownloadedFilesFormat"] = "Descargados {0}/{1} archivo(s)...",
                    ["UpdateComplete"] = "Actualización completada.",
                    ["UpdateFinishedWithRemaining"] = "La actualización terminó con archivos pendientes.",
                    ["UpdateCompleteMessage"] = "La actualización del cliente terminó correctamente.",
                    ["UpdateRemainingMessageFormat"] = "Las descargas terminaron, pero {0} archivo(s) todavía requieren atención.",
                    ["UpdateCompleteTitle"] = "Actualización completada",
                    ["UpdateFinishedTitle"] = "Actualización finalizada",
                    ["UpdateFailed"] = "Falló la actualización.",
                    ["InvalidManifestUrlTitle"] = "URL de manifiesto no válida",
                    ["InvalidManifestUrlMessage"] = "La URL fija del manifiesto no es válida.",
                    ["InvalidClientFolderTitle"] = "Carpeta del cliente no válida",
                    ["InvalidClientFolderMessage"] = "La carpeta del cliente seleccionada no existe.",
                    ["LauncherUpdateCheckFailedTitle"] = "Falló la comprobación del lanzador",
                    ["LauncherUpdateAvailableTitle"] = "Actualización del lanzador disponible",
                    ["LauncherUpdatePromptFormat"] = "Hay una actualización del lanzador disponible.\r\n\r\nVersión actual: {0}\r\nVersión disponible: {1}\r\n\r\n¿Instalarla ahora? El lanzador se reiniciará cuando termine la actualización.",
                    ["UpdatingLauncher"] = "Actualizando lanzador...",
                    ["LauncherUpdateFailed"] = "Falló la actualización del lanzador.",
                    ["LauncherUpdateStartFailed"] = "No se pudo iniciar la actualización del lanzador.",
                    ["NoRealmNews"] = "Todavía no se han publicado noticias del reino.",
                    ["RealmNewsLoadFailed"] = "No se pudieron cargar las noticias del reino en este momento.",
                    ["BreakingNewsHeading"] = "Noticias urgentes",
                    ["RecentUpdatesHeading"] = "Actualizaciones recientes",
                    ["NewsHeadingFormat"] = "Noticias de {0}",
                    ["BuildPrefix"] = "Versión",
                    ["PublishedPrefix"] = "Publicado",
                    ["ByPrefix"] = "Por",
                    ["PlayersPrefix"] = "Jugadores",
                    ["QueuePrefix"] = "Cola",
                    ["UptimePrefix"] = "Tiempo activo",
                    ["PendingReasonMissing"] = "No existe localmente",
                    ["PendingReasonSizeMismatch"] = "Tamaño distinto",
                    ["PendingReasonHashMismatch"] = "Hash distinto",
                    ["SelfUpdateInvalidArgumentsMessage"] = "Los argumentos de actualización del lanzador no son válidos.",
                    ["SelfUpdateFailedTitle"] = "Falló la actualización del lanzador",
                    ["SelfUpdateFailedMessageFormat"] = "El lanzador no pudo actualizarse a sí mismo.\r\n\r\n{0}"
                }
            },
            {
                "de",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["LanguageLabel"] = "Sprache",
                    ["FormTitleFormat"] = "{0} Launcher",
                    ["SubtitleFormat"] = "Wrath of the Lich King 3.3.5a-Launcher für Patches, Realm-Neuigkeiten und dein nächstes Abenteuer. Launcher v{0}",
                    ["ConnectionGroupFormat"] = "{0}-Client",
                    ["ClientHintFormat"] = "Wähle deinen 3.3.5a-Clientordner, synchronisiere ihn mit {0} und betrete dann das Realm.",
                    ["ClientPath"] = "World of Warcraft-Pfad",
                    ["Browse"] = "Durchsuchen...",
                    ["CheckUpdates"] = "Nach Updates suchen",
                    ["UpdateClient"] = "Client aktualisieren",
                    ["EnterWorld"] = "Welt betreten",
                    ["ClearCache"] = "Cache leeren",
                    ["RealmStatusLabelFormat"] = "{0}-STATUS",
                    ["OnlinePlayersLabel"] = "SPIELER ONLINE",
                    ["NewsGroupFormat"] = "{0}-Neuigkeiten",
                    ["NewsHintFormat"] = "Aktuelle Meldungen und neue Updates aus dem Realm-Feed von {0}.",
                    ["UpdatesGroupFormat"] = "{0}-Operationen",
                    ["UpdatesHintFormat"] = "Verfolge Downloads, ausstehende Dateien und die aktuelle Launcher-Aktivität für {0}.",
                    ["PendingUpdates"] = "Ausstehende Updates",
                    ["ActivityLog"] = "Aktivitätsprotokoll",
                    ["RemoteVersionLabelFormat"] = "{0}-BUILD",
                    ["StatusLabel"] = "STATUS",
                    ["DefaultNewsTextFormat"] = "Klicke auf " + "Nach Updates suchen" + ", um die neuesten Neuigkeiten von {0} zu laden.",
                    ["SelectClientFolderDescription"] = "Wähle deinen World of Warcraft 3.3.5a-Clientordner aus.",
                    ["Ready"] = "Bereit",
                    ["RealmStatusChecking"] = "Prüfung läuft...",
                    ["RealmStatusOnline"] = "Online",
                    ["RealmStatusOffline"] = "Offline",
                    ["RealmStatusUnknown"] = "Unbekannt",
                    ["LaunchFailedTitle"] = "Start fehlgeschlagen",
                    ["LaunchFileNotFound"] = "Die Spiel-Executable wurde im ausgewählten Ordner nicht gefunden.",
                    ["MissingClientFolderTitle"] = "Clientordner fehlt",
                    ["MissingClientFolderMessage"] = "Wähle zuerst den lokalen Clientordner aus.",
                    ["CacheNotFoundTitle"] = "Cache nicht gefunden",
                    ["CacheNotFoundMessage"] = "Im ausgewählten Clientverzeichnis wurde kein Cache-Ordner gefunden.",
                    ["ClearCacheTitle"] = "Cache leeren",
                    ["ClearCacheConfirm"] = "Den Cache-Ordner aus dem ausgewählten Clientverzeichnis löschen?",
                    ["CacheCleared"] = "Cache geleert.",
                    ["CacheClearFailed"] = "Cache konnte nicht geleert werden.",
                    ["CheckingForUpdates"] = "Suche nach Updates...",
                    ["GatheringRealmNews"] = "Realm-Neuigkeiten werden geladen...",
                    ["ClientUpToDate"] = "Der Client ist aktuell.",
                    ["FilesNeedUpdateFormat"] = "{0} Datei(en) müssen aktualisiert werden.",
                    ["UpdateCheckFailed"] = "Updateprüfung fehlgeschlagen.",
                    ["UpdateCheckFailedNews"] = "Realm-Neuigkeiten können erst geladen werden, wenn das Manifest erreichbar ist.",
                    ["DownloadingUpdatesFormat"] = "Updates werden mit {0} Threads heruntergeladen...",
                    ["DownloadedFilesFormat"] = "{0}/{1} Datei(en) heruntergeladen...",
                    ["UpdateComplete"] = "Update abgeschlossen.",
                    ["UpdateFinishedWithRemaining"] = "Update mit verbleibenden Dateien beendet.",
                    ["UpdateCompleteMessage"] = "Das Client-Update wurde erfolgreich abgeschlossen.",
                    ["UpdateRemainingMessageFormat"] = "Die Downloads wurden beendet, aber {0} Datei(en) benötigen noch Aufmerksamkeit.",
                    ["UpdateCompleteTitle"] = "Update abgeschlossen",
                    ["UpdateFinishedTitle"] = "Update beendet",
                    ["UpdateFailed"] = "Update fehlgeschlagen.",
                    ["InvalidManifestUrlTitle"] = "Ungültige Manifest-URL",
                    ["InvalidManifestUrlMessage"] = "Die fest codierte Manifest-URL ist ungültig.",
                    ["InvalidClientFolderTitle"] = "Ungültiger Clientordner",
                    ["InvalidClientFolderMessage"] = "Der ausgewählte Clientordner existiert nicht.",
                    ["LauncherUpdateCheckFailedTitle"] = "Launcher-Updateprüfung fehlgeschlagen",
                    ["LauncherUpdateAvailableTitle"] = "Launcher-Update verfügbar",
                    ["LauncherUpdatePromptFormat"] = "Ein Launcher-Update ist verfügbar.\r\n\r\nAktuelle Version: {0}\r\nVerfügbare Version: {1}\r\n\r\nJetzt installieren? Der Launcher wird nach Abschluss des Updates neu gestartet.",
                    ["UpdatingLauncher"] = "Launcher wird aktualisiert...",
                    ["LauncherUpdateFailed"] = "Launcher-Update fehlgeschlagen.",
                    ["LauncherUpdateStartFailed"] = "Das Launcher-Update konnte nicht gestartet werden.",
                    ["NoRealmNews"] = "Es wurden noch keine Realm-Neuigkeiten veröffentlicht.",
                    ["RealmNewsLoadFailed"] = "Die Realm-Neuigkeiten konnten derzeit nicht geladen werden.",
                    ["BreakingNewsHeading"] = "Wichtige Neuigkeiten",
                    ["RecentUpdatesHeading"] = "Letzte Updates",
                    ["NewsHeadingFormat"] = "{0}-Neuigkeiten",
                    ["BuildPrefix"] = "Build",
                    ["PublishedPrefix"] = "Veröffentlicht",
                    ["ByPrefix"] = "Von",
                    ["PlayersPrefix"] = "Spieler",
                    ["QueuePrefix"] = "Warteschlange",
                    ["UptimePrefix"] = "Laufzeit",
                    ["PendingReasonMissing"] = "Lokal nicht vorhanden",
                    ["PendingReasonSizeMismatch"] = "Größe stimmt nicht überein",
                    ["PendingReasonHashMismatch"] = "Hash stimmt nicht überein",
                    ["SelfUpdateInvalidArgumentsMessage"] = "Die Argumente für das Launcher-Update sind ungültig.",
                    ["SelfUpdateFailedTitle"] = "Launcher-Update fehlgeschlagen",
                    ["SelfUpdateFailedMessageFormat"] = "Der Launcher konnte sich nicht selbst aktualisieren.\r\n\r\n{0}"
                }
            },
            {
                "fr",
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["LanguageLabel"] = "Langue",
                    ["FormTitleFormat"] = "Lanceur {0}",
                    ["SubtitleFormat"] = "Lanceur Wrath of the Lich King 3.3.5a pour les correctifs, les actualités du royaume et votre prochaine aventure. Lanceur v{0}",
                    ["ConnectionGroupFormat"] = "Client {0}",
                    ["ClientHintFormat"] = "Choisissez votre dossier client 3.3.5a, synchronisez-le avec {0}, puis entrez dans le royaume.",
                    ["ClientPath"] = "Chemin de World of Warcraft",
                    ["Browse"] = "Parcourir...",
                    ["CheckUpdates"] = "Vérifier les mises à jour",
                    ["UpdateClient"] = "Mettre à jour le client",
                    ["EnterWorld"] = "Entrer dans le monde",
                    ["ClearCache"] = "Vider le cache",
                    ["RealmStatusLabelFormat"] = "STATUT DE {0}",
                    ["OnlinePlayersLabel"] = "JOUEURS EN LIGNE",
                    ["NewsGroupFormat"] = "Actualités de {0}",
                    ["NewsHintFormat"] = "Dernières nouvelles et mises à jour récentes provenant du flux du royaume {0}.",
                    ["UpdatesGroupFormat"] = "Opérations de {0}",
                    ["UpdatesHintFormat"] = "Suivez les téléchargements, les fichiers en attente et l'activité du lanceur pour {0}.",
                    ["PendingUpdates"] = "Mises à jour en attente",
                    ["ActivityLog"] = "Journal d'activité",
                    ["RemoteVersionLabelFormat"] = "BUILD {0}",
                    ["StatusLabel"] = "STATUT",
                    ["DefaultNewsTextFormat"] = "Cliquez sur Vérifier les mises à jour pour charger les dernières actualités de {0}.",
                    ["SelectClientFolderDescription"] = "Sélectionnez votre dossier client World of Warcraft 3.3.5a.",
                    ["Ready"] = "Prêt",
                    ["RealmStatusChecking"] = "Vérification...",
                    ["RealmStatusOnline"] = "En ligne",
                    ["RealmStatusOffline"] = "Hors ligne",
                    ["RealmStatusUnknown"] = "Inconnu",
                    ["LaunchFailedTitle"] = "Échec du lancement",
                    ["LaunchFileNotFound"] = "Impossible de trouver l'exécutable du jeu dans le dossier sélectionné.",
                    ["MissingClientFolderTitle"] = "Dossier client manquant",
                    ["MissingClientFolderMessage"] = "Sélectionnez d'abord le dossier client local.",
                    ["CacheNotFoundTitle"] = "Cache introuvable",
                    ["CacheNotFoundMessage"] = "Aucun dossier Cache n'a été trouvé dans le répertoire client sélectionné.",
                    ["ClearCacheTitle"] = "Vider le cache",
                    ["ClearCacheConfirm"] = "Supprimer le dossier Cache du répertoire client sélectionné ?",
                    ["CacheCleared"] = "Cache vidé.",
                    ["CacheClearFailed"] = "Échec du vidage du cache.",
                    ["CheckingForUpdates"] = "Recherche des mises à jour...",
                    ["GatheringRealmNews"] = "Chargement des actualités du royaume...",
                    ["ClientUpToDate"] = "Le client est à jour.",
                    ["FilesNeedUpdateFormat"] = "{0} fichier(s) doivent être mis à jour.",
                    ["UpdateCheckFailed"] = "Échec de la vérification des mises à jour.",
                    ["UpdateCheckFailedNews"] = "Impossible de charger les actualités du royaume tant que le manifeste n'est pas accessible.",
                    ["DownloadingUpdatesFormat"] = "Téléchargement des mises à jour avec {0} threads...",
                    ["DownloadedFilesFormat"] = "{0}/{1} fichier(s) téléchargé(s)...",
                    ["UpdateComplete"] = "Mise à jour terminée.",
                    ["UpdateFinishedWithRemaining"] = "Mise à jour terminée avec des fichiers restants.",
                    ["UpdateCompleteMessage"] = "La mise à jour du client s'est terminée avec succès.",
                    ["UpdateRemainingMessageFormat"] = "Les téléchargements sont terminés, mais {0} fichier(s) nécessitent encore une attention.",
                    ["UpdateCompleteTitle"] = "Mise à jour terminée",
                    ["UpdateFinishedTitle"] = "Mise à jour terminée",
                    ["UpdateFailed"] = "Échec de la mise à jour.",
                    ["InvalidManifestUrlTitle"] = "URL du manifeste invalide",
                    ["InvalidManifestUrlMessage"] = "L'URL codée en dur du manifeste est invalide.",
                    ["InvalidClientFolderTitle"] = "Dossier client invalide",
                    ["InvalidClientFolderMessage"] = "Le dossier client sélectionné n'existe pas.",
                    ["LauncherUpdateCheckFailedTitle"] = "Échec de la vérification du lanceur",
                    ["LauncherUpdateAvailableTitle"] = "Mise à jour du lanceur disponible",
                    ["LauncherUpdatePromptFormat"] = "Une mise à jour du lanceur est disponible.\r\n\r\nVersion actuelle : {0}\r\nVersion disponible : {1}\r\n\r\nL'installer maintenant ? Le lanceur redémarrera une fois la mise à jour terminée.",
                    ["UpdatingLauncher"] = "Mise à jour du lanceur...",
                    ["LauncherUpdateFailed"] = "Échec de la mise à jour du lanceur.",
                    ["LauncherUpdateStartFailed"] = "Impossible de démarrer la mise à jour du lanceur.",
                    ["NoRealmNews"] = "Aucune actualité du royaume n'a encore été publiée.",
                    ["RealmNewsLoadFailed"] = "Les actualités du royaume n'ont pas pu être chargées pour le moment.",
                    ["BreakingNewsHeading"] = "Dernières nouvelles",
                    ["RecentUpdatesHeading"] = "Mises à jour récentes",
                    ["NewsHeadingFormat"] = "Actualités de {0}",
                    ["BuildPrefix"] = "Build",
                    ["PublishedPrefix"] = "Publié",
                    ["ByPrefix"] = "Par",
                    ["PlayersPrefix"] = "Joueurs",
                    ["QueuePrefix"] = "File d'attente",
                    ["UptimePrefix"] = "Temps de fonctionnement",
                    ["PendingReasonMissing"] = "Absent localement",
                    ["PendingReasonSizeMismatch"] = "Taille différente",
                    ["PendingReasonHashMismatch"] = "Hash différent",
                    ["SelfUpdateInvalidArgumentsMessage"] = "Les arguments de mise à jour du lanceur sont invalides.",
                    ["SelfUpdateFailedTitle"] = "Échec de la mise à jour du lanceur",
                    ["SelfUpdateFailedMessageFormat"] = "Le lanceur n'a pas pu se mettre à jour lui-même.\r\n\r\n{0}"
                }
            }
        };

        public static IReadOnlyList<SupportedLanguage> GetSupportedLanguages()
        {
            return SupportedLanguages;
        }

        public static string ResolveLanguageCode(string preferredLanguageCode)
        {
            string normalizedPreferredLanguageCode = NormalizeLanguageCode(preferredLanguageCode);
            if (IsSupported(normalizedPreferredLanguageCode))
            {
                return normalizedPreferredLanguageCode;
            }

            string currentUiLanguageCode = NormalizeLanguageCode(CultureInfo.CurrentUICulture.Name);
            return IsSupported(currentUiLanguageCode) ? currentUiLanguageCode : DefaultLanguageCode;
        }

        public static void ApplyCulture(string languageCode)
        {
            var culture = GetCulture(languageCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        public static string Get(string key, string languageCode)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            Dictionary<string, string> languageTranslations;
            if (Translations.TryGetValue(ResolveLanguageCode(languageCode), out languageTranslations))
            {
                string value;
                if (languageTranslations.TryGetValue(key, out value))
                {
                    return value;
                }
            }

            string fallbackValue;
            return Translations[DefaultLanguageCode].TryGetValue(key, out fallbackValue) ? fallbackValue : key;
        }

        public static string Format(string key, string languageCode, params object[] args)
        {
            return string.Format(GetCulture(languageCode), Get(key, languageCode), args ?? Array.Empty<object>());
        }

        public static CultureInfo GetCulture(string languageCode)
        {
            string resolvedLanguageCode = ResolveLanguageCode(languageCode);
            if (string.Equals(resolvedLanguageCode, "es", StringComparison.OrdinalIgnoreCase))
            {
                return CultureInfo.GetCultureInfo("es-ES");
            }

            if (string.Equals(resolvedLanguageCode, "de", StringComparison.OrdinalIgnoreCase))
            {
                return CultureInfo.GetCultureInfo("de-DE");
            }

            if (string.Equals(resolvedLanguageCode, "fr", StringComparison.OrdinalIgnoreCase))
            {
                return CultureInfo.GetCultureInfo("fr-FR");
            }

            return CultureInfo.GetCultureInfo("en-US");
        }

        private static bool IsSupported(string languageCode)
        {
            return SupportedLanguages.Any(language => string.Equals(language.Code, languageCode, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeLanguageCode(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return string.Empty;
            }

            string normalizedLanguageCode = languageCode.Trim();
            int separatorIndex = normalizedLanguageCode.IndexOf('-');
            if (separatorIndex < 0)
            {
                separatorIndex = normalizedLanguageCode.IndexOf('_');
            }

            return (separatorIndex > 0 ? normalizedLanguageCode.Substring(0, separatorIndex) : normalizedLanguageCode).ToLowerInvariant();
        }
    }
}

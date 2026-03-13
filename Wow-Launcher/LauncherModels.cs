using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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

        public string Reason { get; set; }
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
}

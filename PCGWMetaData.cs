using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using Bluscream;
using PCGWMetaData.Classes;
using System.Windows.Controls;
using Playnite.SDK.Models;
//using System.IO;

namespace PCGWMetaData
{
    public class PCGWMetaDataPlugin : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        public override string Name { get; } = "PCGamingWiki";
        public override Guid Id { get; } = Guid.Parse("111001DB-DBD1-46C6-B5D0-B1BA559D10E4");
        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField> { MetadataField.Tags };
        public IPlayniteAPI api;
        internal Cache cache;
        internal WebClient webClient = new WebClient();
        internal const string url_base = "https://www.pcgamingwiki.com/w/api.php?action=browsebysubject&format=json&subject={0}";

        public PCGWMetaDataPlugin(IPlayniteAPI playniteAPI) : base(playniteAPI)
        {
            this.api = playniteAPI;
            this.cache = new Cache(this);
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new PCGWMetadataProvider(options, this);
        }
    }

    public class PCGWMetadataProvider : OnDemandMetadataProvider
    {
        private readonly MetadataRequestOptions options;
        private List<MetadataField> availableFields;
        private PCGWMetaDataPlugin plugin;

        public PCGWMetadataProvider(MetadataRequestOptions options, PCGWMetaDataPlugin plugin)
        {
            this.options = options;
            this.plugin = plugin;
        }

        public override List<MetadataField> AvailableFields
        {
            get
            {
                if (availableFields == null)
                {
                    availableFields = GetAvailableFields();
                }

                return availableFields;
            }
        }

        private List<MetadataField> GetAvailableFields()
        {
            return new List<MetadataField> { MetadataField.Tags };
        }

        public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
        {
            plugin.api.Dialogs.ShowMessage("Requested metadata for game " + options.GameData.Name);
            var tags = new List<MetadataProperty>();
            var l_ = plugin.api.Database.Games.FirstOrDefault(g => g.Id == options.GameData.Id);
            if (l_ != null)
            {
                var l__ = l_.Tags;
                if (l__ != null)
                {
                    /* tags = l__.Select(t => t.Name).ToList(); */
                    foreach (Tag t in l__)
                    {
                        tags.Add(new MetadataNameProperty(t.Name.ToLowerInvariant().Trim()));
                    }
                }
            }
            var _result = plugin.cache.getGame(options.GameData.Name);
            if (_result != null)
            {
                var result = _result.Data();
                plugin.api.Dialogs.ShowMessage(JsonConvert.SerializeObject(result));
                //File.WriteAllText("c:\\temp\\log.txt", JsonConvert.SerializeObject(result));
                if (result is null || result.Query is null || result.Query.Data is null)
                    return null;
                var local_play = result.Query.Data.Where(i => i.Property == "Local_play").FirstOrDefault()?.Dataitem.FirstOrDefault().Item;
                if (local_play == "true")
                {
                    tags.Add(new MetadataNameProperty("Local Multiplayer".ToLowerInvariant().Trim()));
                    var local_play_mode = result.Query.Data.Where(i => i.Property == "Local_play_modes").FirstOrDefault()?.Dataitem.FirstOrDefault().Item;
                    if (local_play_mode != null)
                        tags.Add(new MetadataNameProperty(("Local MP Mode: " + local_play_mode).ToLowerInvariant().Trim()));
                    var local_play_players = result.Query.Data.Where(i => i.Property == "Local_play_players").FirstOrDefault()?.Dataitem.FirstOrDefault().Item;
                    if (local_play_players != null && local_play_players != "0")
                        tags.Add(new MetadataNameProperty(("Local MP Playercount: " + local_play_players).ToLowerInvariant().Trim()));
                }
                plugin.api.Dialogs.ShowMessage(JsonConvert.SerializeObject(tags));
            }
            return tags;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Helpers.Utils;
using PhoenixAdult.Helpers;
using System.Globalization;
using MediaBrowser.Controller.Entities.Movies;

namespace PhoenixAdult.Sites
{
    public class SiteFamilyTherapyXXX : IProviderBase
    {
        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            searchTitle = searchTitle.Replace(" ", "+", StringComparison.OrdinalIgnoreCase);
            var url = Helper.GetSearchSearchURL(siteNum) + searchTitle;
            var data = await HTML.ElementFromURL(url, cancellationToken).ConfigureAwait(false);

            var searchResults = data.SelectNodesSafe("//article[contains(@class, 'post')]");
            foreach (var searchResult in searchResults)
            {
                var sceneLink = searchResult.SelectSingleNode(".//h2[@class='entry-title']//a");
                var sceneURL = sceneLink.Attributes["href"].Value;
                var sceneName = sceneLink.InnerText.Trim();
                var curID = Helper.Encode(sceneURL);
                var image = searchResult.SelectSingleNode(".//a[@class='entry-featured-image-url']/img");
                var scenePoster = image.Attributes["src"].Value;
                var date = searchResult.SelectSingleNode(".//p[@class='post-meta']//span[@class='published']");
                var sceneDate = date.InnerText.Trim();

                var res = new RemoteSearchResult
                {
                    ProviderIds = { { Plugin.Instance.Name, curID } },
                    Name = sceneName,
                    ImageUrl = scenePoster,
                };

                if (DateTime.TryParseExact(sceneDate, "MMM d, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
                {
                    res.PremiereDate = sceneDateObj;
                }

                result.Add(res);
            }

            return result;
        }

        public async Task<MetadataResult<BaseItem>> Update(int[] siteNum, string[] sceneID, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<BaseItem>()
            {
                Item = new Movie(),
                People = new List<PersonInfo>(),
            };

            if (sceneID == null)
            {
                return result;
            }

            return result;
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(int[] siteNum, string[] sceneID, BaseItem item, CancellationToken cancellationToken)
        {
            var result = new List<RemoteImageInfo>();

            if (sceneID == null)
            {
                return result;
            }

            return result;
        }
    }
}

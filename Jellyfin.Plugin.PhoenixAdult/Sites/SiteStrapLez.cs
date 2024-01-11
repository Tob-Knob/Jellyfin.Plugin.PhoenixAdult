using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using PhoenixAdult.Helpers;
using PhoenixAdult.Helpers.Utils;

namespace PhoenixAdult.Sites
{
    public class SiteStrapLez : IProviderBase
    {
        private static Regex posterUrlRegex = new Regex(@"\((.*?)\)");

        public async Task<List<RemoteSearchResult>> Search(int[] siteNum, string searchTitle, DateTime? searchDate, CancellationToken cancellationToken)
        {
            var result = new List<RemoteSearchResult>();
            if (siteNum == null || string.IsNullOrEmpty(searchTitle))
            {
                return result;
            }

            var searchResultsURLs = new List<string>();

            var url = Helper.GetSearchSearchURL(siteNum) + searchTitle;
            Logger.Info($"Searching for scene: {url}");
            var data = await HTML.ElementFromURL(url, cancellationToken, additionalSuccessStatusCodes: HttpStatusCode.Redirect).ConfigureAwait(false);
            var siteResults = data.SelectNodesSafe("//div[contains(@class, 'card-media')]/a");

            foreach (var searchResult in siteResults)
            {
                var sceneURL = Helper.GetSearchBaseURL(siteNum) + searchResult.Attributes["href"].Value;
                Logger.Info($"Possible result {sceneURL}");
                searchResultsURLs.Add(sceneURL);
            }

            foreach (var searchResult in searchResultsURLs)
            {
                var sceneID = new List<string> { Helper.Encode(searchResult) };

                if (searchDate.HasValue)
                {
                    sceneID.Add(searchDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }

                var searchResultsFromUpdate = await Helper.GetSearchResultsFromUpdate(this, siteNum, sceneID.ToArray(), searchDate, cancellationToken).ConfigureAwait(false);
                if (searchResultsFromUpdate.Any())
                {
                    result.AddRange(searchResultsFromUpdate);
                }
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

            var siteUrl = Helper.GetSearchBaseURL(siteNum);
            var sceneURL = Helper.Decode(sceneID[0]);
            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = siteUrl + sceneURL;
            }

            result.Item.ExternalId = sceneURL;
            result.Item.AddStudio("Strap Lez");

            Logger.Info($"Loading scene {sceneURL}");
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken, additionalSuccessStatusCodes: HttpStatusCode.Redirect).ConfigureAwait(false);

            var title = sceneData.SelectSingleText("//div[@class='panel-content']//div[contains(@class, 'info-container')]/h3[contains(@class, 'headline')]/a");
            result.Item.Name = title;

            var dateString = sceneData.SelectNodesSafe("//li[./span[contains(text(), 'Released')]]/span")[1].InnerText;
            if (DateTime.TryParseExact(dateString, "MMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sceneDateObj))
            {
                result.Item.PremiereDate = sceneDateObj;
            }

            // performers
            var performers = sceneData.SelectNodesSafe("//li[./span[contains(text(), 'Cast')]]/span/a");

            foreach (var performer in performers)
            {
                var performerURL = siteUrl + performer.Attributes["href"].Value;
                Logger.Info($"Loading performer page: {performerURL}");
                var performerData = await HTML.ElementFromURL(performerURL, cancellationToken, additionalSuccessStatusCodes: HttpStatusCode.Redirect).ConfigureAwait(false);
                var performerImage = performerData.SelectSingleNode("//div[@class='panel-content']/img");
                var performerName = performerData.SelectSingleText("//h3[contains(@class, 'headline')]/a");
                result.AddPerson(new PersonInfo
                {
                    Name = performerName,
                    ImageUrl = performerImage.Attributes["src"].Value,
                });
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

            var sceneURL = Helper.Decode(sceneID[0]);
            if (!sceneURL.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                sceneURL = Helper.GetSearchBaseURL(siteNum) + sceneURL;
            }

            Logger.Info($"Loading scene for images {sceneURL}");
            var sceneData = await HTML.ElementFromURL(sceneURL, cancellationToken, additionalSuccessStatusCodes: HttpStatusCode.Redirect).ConfigureAwait(false);

            var poster = sceneData.SelectSingleNode("//div[@class='panel-content']/img");

            result.Add(new RemoteImageInfo
            {
                Url = poster.Attributes["src"].Value,
                Type = ImageType.Primary,
            });

            var largePoster = sceneData.SelectSingleNode("//div[@class='jw-preview jw-reset']");
            var largePosterStyle = largePoster.Attributes["style"].Value;
            var largePosterUrl = posterUrlRegex.Match(largePosterStyle).Groups[1].Value;

            result.Add(new RemoteImageInfo
            {
                Url = largePosterUrl,
                Type = ImageType.Primary,
            });
            result.Add(new RemoteImageInfo
            {
                Url = largePosterUrl,
                Type = ImageType.Backdrop,
            });

            return result;
        }
    }
}

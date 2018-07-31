using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using NuGet.Versioning;
using System;

namespace NuGetVersion
{
    public class NuGetClient
    {
        private HttpClient httpClient;
        public NuGetClient(HttpClient client)
        {
            httpClient = client;
        }

        private async Task<JObject> GetResource(string name)
        {
            var url = $"https://api.nuget.org/v3/registration3/{name}";
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return JObject.Parse(await response.Content.ReadAsStringAsync());
            }
            else
            {
                // Console.WriteLine(await response.Content.ReadAsStringAsync());
                return null;
            }
        }

        public async Task<PackageInfo> GetPackageInfo(string packageName)
        {
            var json = await this.GetResource(packageName.ToLower() + "/index.json");

            if (json == null)
            {
                return null;
            }
            var versions = new List<SemanticVersion>();

            var items = json["items"].AsJEnumerable();
            if (items[0]["items"] != null)
            {
                foreach (var item in items)
                {
                    versions.AddRange(this.ExtractVersions(item["items"]));
                }
            }
            else
            {
                var requests = items.Select(i =>
                {
                    var id = i["@id"].ToString();
                    var resourceName = id.Substring(id.IndexOf(packageName.ToLower()));
                    return this.GetResource(resourceName);
                });

                var pages = await Task.WhenAll(requests);
                foreach (var page in pages)
                    versions.AddRange(this.ExtractVersions(page["items"]));
            }

            versions.Reverse();
            return new PackageInfo(packageName, versions);
        }

        private IEnumerable<SemanticVersion> ExtractVersions(JToken items)
        {
            foreach (var item in items)
            {
                bool listed = Convert.ToBoolean(item["catalogEntry"]["listed"].ToString());
                if (!listed)
                    continue;

                SemanticVersion version;
                if (SemanticVersion.TryParse(item["catalogEntry"]["version"].ToString(), out version))
                    yield return version;
            }
        }
    }
}
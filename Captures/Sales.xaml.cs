using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace Captures
{
    public partial class Sales : ContentPage
    {
        private readonly HttpClient _httpClient;

        // Credentials
        private readonly string USERNAME = "krishnavamsiott1045@gmail.com";
        private readonly string PASSWORD = "Gamana@123yFbgOvs9C5apjorR59WyJJDO";
        private readonly string CONSUMER_KEY = "3MVG9VMBZCsTL9hlHgf0XlaebsnPGz9PGXNm.gOGUpbi84nsJ0rsa2kNe.lCUjr1oGBScAFs03nxHJqegWb74";
        private readonly string CONSUMER_SECRET = "58EC207B519179198BBBAF145A0CBB255377D2AE2BE3CC3D2062E92F7C16F344";
        private readonly string DOMAIN_NAME = "https://gamana31-dev-ed.develop.my.salesforce.com";

        // Constructor
        public Sales()
        {
            InitializeComponent();
            _httpClient = new HttpClient();

            // Call the method to get access token and retrieve all object data
            GetAccessTokenAndRetrieveAllObjectsAsync();
        }

        public async Task GetAccessTokenAndRetrieveAllObjectsAsync()
        {
            OutputLabel.Text = "Attempting to obtain access token...";

            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", CONSUMER_KEY },
                { "client_secret", CONSUMER_SECRET },
                { "username", USERNAME },
                { "password", PASSWORD }
            };

            var content = new FormUrlEncodedContent(parameters);

            try
            {
                var response = await _httpClient.PostAsync($"{DOMAIN_NAME}/services/oauth2/token", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var tokenData = JsonDocument.Parse(jsonResponse);
                    var accessToken = tokenData.RootElement.GetProperty("access_token").GetString();
                    var instanceUrl = tokenData.RootElement.GetProperty("instance_url").GetString();

                    // Retrieve all objects and their data
                    await RetrieveAllObjectsDataAsync(instanceUrl, accessToken);
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    OutputLabel.Text = $"Failed to obtain access token. Response: {errorResponse}";
                }
            }
            catch (Exception ex)
            {
                OutputLabel.Text = $"Exception occurred while trying to obtain access token: {ex.Message}";
            }
        }

        public async Task RetrieveAllObjectsDataAsync(string instanceUrl, string accessToken)
        {
            OutputLabel.Text = "Retrieving all objects...";

            try
            {
                // Retrieve metadata of all objects
                var requestUrl = $"{instanceUrl}/services/data/v59.0/sobjects";
                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var allObjectsData = JsonDocument.Parse(jsonResponse);

                    // Filter and display all objects
                    StringBuilder displayText = new StringBuilder("All Objects and Their Records:\n");

                    foreach (var sobject in allObjectsData.RootElement.GetProperty("sobjects").EnumerateArray())
                    {
                        var name = sobject.GetProperty("name").GetString();
                        var label = sobject.GetProperty("label").GetString();
                        var custom = sobject.GetProperty("custom").GetBoolean();

                        displayText.AppendLine($"Object Name: {label} ({name})");

                        // Retrieve records for each object
                        var records = await FetchRecordsForObject(instanceUrl, accessToken, name);
                        foreach (var record in records)
                        {
                            displayText.AppendLine($" - Record Name: {record.Name}");
                        }

                        displayText.AppendLine();
                    }

                    OutputLabel.Text = displayText.ToString();
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    OutputLabel.Text = $"Failed to retrieve objects. ErrorCode: {response.StatusCode}, Message: {errorResponse}";
                }
            }
            catch (Exception ex)
            {
                OutputLabel.Text = $"Exception occurred while trying to retrieve objects: {ex.Message}";
            }
        }

        private async Task<List<Record>> FetchRecordsForObject(string instanceUrl, string accessToken, string objectName)
        {
            try
            {
                var query = $"SELECT Name FROM {objectName} LIMIT 10"; // Adjust query as needed
                var requestUrl = $"{instanceUrl}/services/data/v59.0/query/?q={Uri.EscapeDataString(query)}";

                var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var recordsData = JsonDocument.Parse(jsonResponse);
                    var records = new List<Record>();

                    foreach (var record in recordsData.RootElement.GetProperty("records").EnumerateArray())
                    {
                        var recordName = record.GetProperty("Name").GetString();
                        records.Add(new Record { Name = recordName });
                    }

                    return records;
                }
                else
                {
                    return new List<Record>(); // Return empty list on failure
                }
            }
            catch (Exception)
            {
                return new List<Record>(); // Return empty list on exception
            }
        }

        public class Record
        {
            public string Name { get; set; }
        }
    }
}

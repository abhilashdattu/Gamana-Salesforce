using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text;
using Microsoft.UI;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Linq;

namespace Captures
{
    public partial class Login : ContentPage
    {
        private readonly HttpClient _httpClient;

        public ObservableCollection<CustomObject> CustomObjects { get; } = new ObservableCollection<CustomObject>();

        public Login()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            MetadataCollectionView.ItemsSource = CustomObjects;
        }

        private async void OnSalesforceLoginButtonClicked(object sender, EventArgs e)
        {
            var username = UsernameEntry.Text;
            var password = PasswordEntry.Text;
            var securityToken = SecurityTokenEntry.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(securityToken))
            {
                await DisplayAlert("Error", "Please fill in all fields.", "OK");
                return;
            }

            var requestData = new
            {
                username = username,
                password = password,
                securityToken = securityToken
            };

            var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("http://localhost:3000/fetch-metadata", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var customObjects = JsonSerializer.Deserialize<List<CustomObject>>(jsonResponse);

                    // Clear previous data
                    CustomObjects.Clear();

                    // Add the new data to the CollectionView
                    foreach (var obj in customObjects)
                    {
                        // Fetch records for each custom object
                        var records = await FetchRecordsForObject(obj.name);

                        // Add records to the object and then to the collection
                        obj.records = records;
                        CustomObjects.Add(obj);
                    }

                    // Show the CollectionView
                    MetadataCollectionView.IsVisible = true;
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Error: {errorResponse}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Exception", $"Exception: {ex.Message}", "OK");
            }
        }

        private async Task<List<Record>> FetchRecordsForObject(string objectName)
        {
            try
            {
                var query = $"SELECT Name FROM {objectName} LIMIT 10"; // Adjust query as needed
                var response = await _httpClient.GetAsync($"http://localhost:3000/query-object?objectName={objectName}&query={Uri.EscapeDataString(query)}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Record>>(jsonResponse);
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

        public class CustomObject
        {
            public string name { get; set; }
            public string label { get; set; }
            public bool custom { get; set; }
            public string keyPrefix { get; set; }
            public List<Record> records { get; set; } // Records associated with the object
        }

        public class Record
        {
            public string Name { get; set; }
        }
    }
}

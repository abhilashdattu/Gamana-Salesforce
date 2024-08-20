using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;
using Google.Apis.Drive.v3.Data;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

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

            LoadingIndicator.IsRunning = true; // Show the loading indicator

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
            finally
            {
                LoadingIndicator.IsRunning = false; // Hide the loading indicator
            }
        }

        private async void OnCustomObjectSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedObject = (CustomObject)e.CurrentSelection.FirstOrDefault();

            if (selectedObject != null && selectedObject.records == null)
            {
                LoadingIndicator.IsRunning = true; // Show the loading indicator

                try
                {
                    selectedObject.records = await FetchRecordsForObject(selectedObject.name);
                }
                finally
                {
                    LoadingIndicator.IsRunning = false; // Hide the loading indicator
                }
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

        private static readonly string[] Scopes = { DriveService.Scope.DriveFile };
        private static readonly string ApplicationName = "Your App Name";

        private async void OnSaveToGoogleDriveButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var driveService = AuthenticateGoogleDrive();

                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = "CustomObjects.json",
                    Parents = new List<string> { "appDataFolder" } // Uploads to app data folder
                };

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(CustomObjects))))
                {
                    var request = driveService.Files.Create(fileMetadata, stream, "application/json");
                    request.Fields = "id";
                    var file = await request.UploadAsync();

                    if (file.Status == Google.Apis.Upload.UploadStatus.Completed)
                    {
                        await DisplayAlert("Success", "Custom objects saved to Google Drive.", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", "Failed to save file to Google Drive.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private DriveService AuthenticateGoogleDrive()
        {
            UserCredential credential;

            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            return new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
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

namespace Captures;

public partial class WebLoginPage : ContentPage
{
    public WebLoginPage(string authorizationUrl)
    {
        InitializeComponent();

        // Set the WebView's source to the Salesforce authorization URL
        //SalesforceWebView.Source = authorizationUrl;
    }
}

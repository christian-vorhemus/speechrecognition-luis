using Template10.Mvvm;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Template10.Services.NavigationService;
using Windows.UI.Xaml.Navigation;
using Windows.Media.SpeechRecognition;
using Windows.UI.Xaml;
using System.Net.Http;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using Windows.UI.Xaml.Controls;

namespace SpeechrecognitionLUIS.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public MainPageViewModel()
        {
            Value = "";
        }

        private string _Value = "";

        // Replace keys with your own keys ----------------------------------------
        private string luisAppId = "<<YOUR LUIS APP ID>>";
        private string subscriptionKey = "<<YOUR LUIS KEY>>";
        private string openweathermapKey = "<<YOUR OPENWEATHERMAP KEY>>";
        // ------------------------------------------------------------------------

        public string Value { get { return _Value; } set { Set(ref _Value, value); } }

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> suspensionState)
        {

        }

        public override async Task OnNavigatedFromAsync(IDictionary<string, object> suspensionState, bool suspending)
        {

        }

        public override async Task OnNavigatingFromAsync(NavigatingEventArgs args)
        {
            args.Cancel = false;
            await Task.CompletedTask;
        }


        public async void StartRecognizing_Click(object sender, RoutedEventArgs e)
        {

            // Create an instance of SpeechRecognizer.
            var language = new Windows.Globalization.Language("en-US");
            var speechRecognizer = new SpeechRecognizer(language);

            // Compile the dictation grammar by default.
            await speechRecognizer.CompileConstraintsAsync();

            // Start recognition.
            SpeechRecognitionResult speechRecognitionResult = await speechRecognizer.RecognizeWithUIAsync();

            // Pass recognized text to LUIS
            CallLUISService(speechRecognitionResult.Text);

        }


        private async void CallLUISService(string text)
        {
            var client = new HttpClient();

            string encodedText = Uri.EscapeDataString(text);

            string uri = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/" + luisAppId + "?subscription-key=" + subscriptionKey + "&staging=true&verbose=true&timezoneOffset=0&q=" + encodedText;

            // Call LUIS service
            var response = await client.GetAsync(uri);
            var strResponseContent = await response.Content.ReadAsStringAsync();

            // Parse the result LUIS returns. Take a look at the docs to see the structure or just enter the Uri in a browser
            JObject luisObject = JObject.Parse(strResponseContent.ToString());
            var resultEntities = luisObject["entities"].Children();

            string intent = luisObject["topScoringIntent"]["intent"].ToString();
            string city = "";
            string date = "";

            foreach (JObject entity in resultEntities)
            {
                if(entity["type"].ToString() == "City")
                {
                    city = entity["entity"].ToString();
                }
                if (entity["type"].ToString() == "builtin.datetimeV2.date")
                {
                    foreach (JObject dateObj in entity["resolution"]["values"].Children())
                    {
                        date = dateObj["value"].ToString();
                    }
                }
            }

            // We just use one service, so the switch doesn't really make sense here but can be extended for a real app
            switch (intent)
            {
                case "GetWeather":
                    CallWeatherService(city, date);
                    break;
                case "GetTemperature":
                    CallWeatherService(city, date);
                    break;
                default:
                    CallWeatherService(city, date);
                    break;
            }

        }



        private async void CallWeatherService(string city, string date)
        {
            var client = new HttpClient();

            // Simple fallback: If no city is present in the command, take "Redmond"
            if(city == "")
            {
                city = "redmond";
            }

            // If no date is present in the command, take today
            if(date == "")
            {
                date = DateTime.Now.ToString("yyyy-MM-dd");
            }

            string uri = "http://api.openweathermap.org/data/2.5/forecast/daily?q=" + city + "&cnt=7&appid=" + openweathermapKey;
            var response = await client.GetAsync(uri);
            var strResponseContent = await response.Content.ReadAsStringAsync();

            // Parse the JSON object returned by openweathermap
            JObject weatherObject = JObject.Parse(strResponseContent.ToString());
            var forecasts = weatherObject["list"].Children();

            foreach (JObject forecast in forecasts)
            {
                double unixts = (double)forecast["dt"];
                var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dt = dt.AddSeconds(unixts).ToLocalTime();
                string dateObj = dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                if(dateObj == date)
                {
                    double temp = Math.Round((double)forecast["temp"]["day"] - 273.15, 2);
                    string weather = forecast["weather"][0]["main"].ToString();

                    // Show forecast in Textbox
                    Value = "Weather forecast for " + UppercaseFirst(city) + " for " + date + ": " + weather + ", " + temp + "°C";
                }

            }

        }

        static string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            return char.ToUpper(s[0]) + s.Substring(1);
        }

    }
}

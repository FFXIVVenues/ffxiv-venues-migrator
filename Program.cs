using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using V2021 = FFXIVVenues.VenueModels.V2021;
using V2022 = FFXIVVenues.VenueModels.V2022;

if (args.Length != 2)
{
    Console.Error.WriteLine("2 arguments required; base URL of API and API authorization key\n\n    FFXIVVenues.Migrator.exe <baseUrl> <authKey>");
    return;
}

var baseUrl = args[0];
var authKey = args[1];

var githubClient = new HttpClient();

var response = await githubClient.GetAsync("https://raw.githubusercontent.com/FFXIVVenues/ffxiv-venues-web/master/src/venues.json");
var venues = await JsonSerializer.DeserializeAsync<V2021.Venue[]>(await response.Content.ReadAsStreamAsync());

var apiClient = new HttpClient();
apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authKey);
apiClient.BaseAddress = new Uri(baseUrl);


foreach (var venue in venues)
{
    try
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"Now processing {venue.id} '{venue.name}'");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(" ¬ Converting venue to 2022 model");
        var newVenue = new V2022.Venue(venue);

        Console.WriteLine(" ¬ Sending venue");
        var venueSendResponse = await apiClient.PutAsJsonAsync($"/venue/{venue.id}", newVenue);
        venueSendResponse.EnsureSuccessStatusCode();

        if (venue.banner != null)
        {
            Console.WriteLine(" ¬ Sending banner media");
            var bannerResponse = await githubClient.GetAsync($"https://raw.githubusercontent.com/FFXIVVenues/ffxiv-venues-web/master/public/{venue.banner}");
            bannerResponse.EnsureSuccessStatusCode();
            var stream = await bannerResponse.Content.ReadAsStreamAsync();
            var sendStream = new StreamContent(stream);
            sendStream.Headers.ContentType = bannerResponse.Content.Headers.ContentType;
            var mediaSendResponse = await apiClient.PutAsync($"/venue/{venue.id}/media", sendStream);
            mediaSendResponse.EnsureSuccessStatusCode();
        }

        Console.WriteLine(" ¬ Approving venue");
        var approveResponse = await apiClient.PutAsJsonAsync($"/venue/{venue.id}/approved", true);
        approveResponse.EnsureSuccessStatusCode();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($" ¬ Complete {venue.id} '{venue.name}'");
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"Could not migrate venue {venue.id} '{venue.name}");
        Console.Error.WriteLine(ex);
    }
}

Console.ResetColor();

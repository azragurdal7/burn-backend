using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;

public class FlaskAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _flaskBaseUrl;

    public FlaskAiService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _flaskBaseUrl = configuration["FlaskSettings:BaseUrl"];
    }

    public async Task<string> PredictBurnAsync(Stream imageStream, string height, string weight)
    {
        var content = new MultipartFormDataContent();

        content.Add(new StreamContent(imageStream), "image", "image.jpg");
        content.Add(new StringContent(height), "height_cm");
        content.Add(new StringContent(weight), "weight_kg");

        var response = await _httpClient.PostAsync($"{_flaskBaseUrl}/predict", content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}

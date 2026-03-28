using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.CognitiveServices.Speech;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json.Serialization;

public class AzureAiService
{
    private readonly IConfiguration _config;

    public AzureAiService(IConfiguration config)
    {
        _config = config;
    }

    // Extract Text
    public async Task<string> ExtractTextAsync(Stream stream)
    {
        var endpoint = _config["Azure:DocIntelEndpoint"];
        var key = _config["Azure:DocIntelKey"];

        var client = new DocumentAnalysisClient(
            new Uri(endpoint),
            new AzureKeyCredential(key));

        var operation = await client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-read",
            stream);

        var result = operation.Value;

        return string.Join("\n",
            result.Pages.SelectMany(p => p.Lines.Select(l => l.Content)));
    }

    // Translate Text
    public async Task<string> TranslateTextAsync(string text, string targetLang)
    {
        var key = _config["Azure:TranslatorKey"];
        var endpoint = _config["Azure:TranslatorEndpoint"];
        var region = _config["Azure:TranslatorRegion"];

        var route = $"/translate?api-version=3.0&to={targetLang}";
        var body = new object[] { new { Text = text } };

        using var client = new HttpClient();

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(endpoint + route),
            Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
        };

        request.Headers.Add("Ocp-Apim-Subscription-Key", key);
        request.Headers.Add("Ocp-Apim-Subscription-Region", region);

        var response = await client.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        dynamic json = JsonConvert.DeserializeObject(result);
        return json[0].translations[0].text;
    }

    // Text → Speech (with chunking + language voice)
    public async Task<byte[]> ConvertTextToSpeechAsync(string text, string lang)
    {
        var speechKey = _config["Azure:SpeechKey"];
        var region = _config["Azure:SpeechRegion"];

        var config = SpeechConfig.FromSubscription(speechKey, region);

        config.SpeechSynthesisVoiceName = lang switch
        {
            "hi" => "hi-IN-SwaraNeural",
            "mr" => "mr-IN-AarohiNeural",
            "de" => "de-DE-KatjaNeural",
            _ => "en-US-AriaNeural"
        };

        using var synthesizer = new SpeechSynthesizer(config);

        var chunks = SplitText(text, 3000);
        var allAudio = new List<byte>();

        foreach (var chunk in chunks)
        {
            var result = await synthesizer.SpeakTextAsync(chunk);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                allAudio.AddRange(result.AudioData);
            }
            else
            {
                throw new Exception("Speech synthesis failed");
            }
        }

        return allAudio.ToArray();
    }

    // 🔧 Helper
    private List<string> SplitText(string text, int size)
    {
        return Enumerable.Range(0, (text.Length + size - 1) / size)
            .Select(i => text.Substring(i * size,
                Math.Min(size, text.Length - i * size)))
            .ToList();
    }
}
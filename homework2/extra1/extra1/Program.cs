/*
 
 
Реализуйте веб-приложения на C# для получения данных о погоде для заданного местоположения. Данные о погоде необходимо запрашивать через АРІ сайта - https://www.weatherapi.com/docs/. (Зарегистрируйте себе бесплатный аккаунт) На главной странице сайта разместите ссылки с названиями популярных городов, по которым можно получить информацию о погоде. Также, в верхней части страницы, разместите форму для поиска, пользователь вводит названия города и получает по нему погоду, если такого города нет, выводим соответствующее сообщение.
Не забудьте получить на сайт АРІ-Кеу.
Пример того как получить погоду о конкретном городе, смотрите по этой ссылке - https://www.weatherapi.com/api-explorer.aspx
(после регистрации и получения ключа).
*/

// списки городов и стран:
// https://github.com/dr5hn/countries-states-cities-database




using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient(); // нельзя каждый раз создавать httpclient 
builder.Services.Configure<WeatherApiOptions>(builder.Configuration.GetSection("WeatherApi"));

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.Run(async (context)=>
{
    HttpRequest request = context.Request;
    HttpResponse response = context.Response;
    var json = await File.ReadAllTextAsync("wwwroot/countries+cities.json");
    var countriesList = JsonSerializer.Deserialize<List<Country>>(json);
    /* if(request.Path == "/")
     {


     }*/
    if (request.Path.Equals("/countries") && request.Method == "GET")
    {
        /*var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var countriesList = JsonSerializer.Deserialize<List<Country>>(json, options);
        */

        if (countriesList != null && countriesList.Count > 0)
            await response.WriteAsJsonAsync(countriesList.Select(c => c.Name));
        //Results.Json()
    }
    else if (request.Path.Equals("/cities") && request?.Method == "GET")
    {
        // List<string>? cityList = countriesList.FirstOrDefault(c => c.Name == request.Query["country"]).Cities;
        var country = countriesList.FirstOrDefault(c => c.Name.Equals(request.Query["country"]));
        if (country != null)
            await response.WriteAsJsonAsync(country.Cities);
        else
        {
            response.StatusCode = 404;
            await response.WriteAsync("страна не найдена");
        }
    }
    else if (request.Path.Equals("/submit") && request?.Method == "GET")
    {
        var factory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
        IConfiguration? configurationService = app.Services.GetService<IConfiguration>();

        if(configurationService != null)
        {
            string? Key = configurationService["WeatherApi:Key"];
            string? baseUri = configurationService["WeatherApi:BaseUrl"];

            if (!string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(baseUri))
            {
                HttpClient httpClient = factory.CreateClient();

                // Uri.EscapeDataString - для экранирования строки, то есть для замены недопустимых символов на их URL-совместимые представления (пробел заменяется на %20)
                string? city = request.Query["city"].ToString();
                string? country = request.Query["country"].ToString();
                //string q = $"&q={city},{country}";

                /*
                 end pointы на http://api.weatherapi.com
                /current.json
                /forecast.json
                /future.json
                /history.json
                /marine.json
                /search.json
                /ip.json
                /timezone.json
                /astronomy.json
                 */
                string q = Uri.EscapeDataString($"{city},{country}");
                string url = $"{baseUri}current.json?key={Key}&q={q}";

                //string url = $"{baseUri}current.json?key={Key}&{Uri.EscapeDataString($"q={city},{country}")}";
                var httpResponse = await httpClient.GetAsync(url); // ответ от погодного сайта
                string? content = await httpResponse.Content.ReadAsStringAsync(); // получаю ответ, как строку. но это уже полноценный json. просто в виде строки с экранированными кавычками
                // отправляю клиенту, как json
                // или
                response.ContentType = "application/json";
                await response.WriteAsync(content);
                // или
                // await response.WriteAsJsonAsync(JsonDocument.Parse(content));

            }
        }        
    }

});

/*
app.MapGet("/", async(IHttpClientFactory httpClientFactory) =>
{
    HttpClient httpClient = httpClientFactory.CreateClient();
    var apiKey = "5eb177b5aa5d4b1493d160602250409"; 
    var city = "Одесса";
    var url = $"http://api.weatherapi.com/v1/current.json?key={apiKey}&q={city}";
    var response = await httpClient.GetAsync(url);
    var content = await response.Content.ReadAsStringAsync();

    return Results.Content(content, "application/json");
});
*/
app.Run();

public record Country {
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("cities")]
    public List<string> Cities { get; set; } = new List<string>();

    public Country() { }
    public Country(string name, List<string> cities)
    {
        Name = name;
        Cities = new List<string>(cities);
    }

}

public class WeatherApiOptions
{
    public string Key { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}
// смена кодировки страницы

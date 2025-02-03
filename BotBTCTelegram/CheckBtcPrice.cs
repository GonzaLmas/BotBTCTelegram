using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Telegram.Bot;

public class CheckBtcPrice
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly string TelegramToken = "8093848208:AAFBtwula2g4IoxGgBrihJ0-rrkGiSGEpDM";
    private static readonly string ChatIdG = "5847624767";
    private static readonly string ChatIdR = "7275568176";
    private readonly ILogger _logger;

    public CheckBtcPrice(ILogger<CheckBtcPrice> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function("CheckBtcPrice")]
    public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"Checking BTC price at {DateTime.Now}");

        try
        {
            decimal currentPrice = await GetBtcPrice();
            decimal previousPrice = await GetPreviousPriceFromBinance();

            //if (currentPrice < previousPrice * 0.92m)
            //    await SendTelegramMessage($"🚨 Alerta BTC: Precio bajó más del 8% 🚨\n💰 Precio actual: {currentPrice} USD\n📉 Precio anterior: {previousPrice} USD");

            if (currentPrice != previousPrice)
                await SendTelegramMessage($"🚨 Alerta BTC: Precio bajó más de 8% 🚨\n💰 Precio actual: {currentPrice} USD\n📉 Precio anterior: {previousPrice} USD");
            else
                await SendTelegramMessage($"🚨 Alerta BTC: Precio NO bajó más de 8$ 🚨\n💰 Precio actual: {currentPrice} USD\n📉 Precio anterior: {previousPrice} USD");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking the BTC price");
        }
    }

    static async Task<decimal> GetBtcPrice()
    {
        string url = "https://api.binance.com/api/v3/ticker/price?symbol=BTCUSDT";
        var response = await client.GetStringAsync(url);
        dynamic data = JsonConvert.DeserializeObject(response);

        return data.price;
    }

    static async Task<decimal> GetPreviousPriceFromBinance()
    {
        string url = "https://api.binance.com/api/v3/klines?symbol=BTCUSDT&interval=1d&limit=2";
        var response = await client.GetStringAsync(url);
        dynamic data = JsonConvert.DeserializeObject(response);

        return data[0][4]; // Índice 4 = precio de cierre
    }

    static async Task SendTelegramMessage(string message)
    {
        var botClient = new TelegramBotClient(TelegramToken);
        await botClient.SendMessage(ChatIdG, message);
        await botClient.SendMessage(ChatIdR, message);
    }
}

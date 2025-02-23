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
    private static readonly string TelegramToken = "80954324848208:AAFBtwula2g4IoxGgBrihJ0-rrkGiSGEpDM";
    private static readonly string ChatIdG = "58476247432427";
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
            _logger.LogInformation("Starting to fetch BTC current price...");
            decimal currentPrice = await GetBtcPrice(_logger); 
            _logger.LogInformation($"Current BTC price fetched: {currentPrice} USD");

            _logger.LogInformation("Starting to fetch the previous BTC price...");
            decimal previousPrice = await GetPreviousPriceFromBinance(_logger); 
            _logger.LogInformation($"Previous BTC price fetched: {previousPrice} USD");

            if (currentPrice != previousPrice)
            {
                _logger.LogInformation("BTC price changed, sending Telegram message...");
                await SendTelegramMessage(_logger, $"🚨 Alerta BTC: Precio bajó más de 8% 🚨\n💰 Precio actual: {currentPrice} USD\n📉 Precio anterior: {previousPrice} USD");
            }
            else
            {
                _logger.LogInformation("BTC price did not change, sending Telegram message...");
                await SendTelegramMessage(_logger, $"🚨 Alerta BTC: Precio NO bajó más de 8% 🚨\n💰 Precio actual: {currentPrice} USD\n📉 Precio anterior: {previousPrice} USD");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking the BTC price");
        }
    }

    static async Task<decimal> GetBtcPrice(ILogger logger) 
    {
        try
        {
            logger.LogInformation("Making request to Binance API for BTC price...");
            string url = "https://api.binance.com/api/v3/ticker/price?symbol=BTCUSDT";
            var response = await client.GetStringAsync(url);
            dynamic data = JsonConvert.DeserializeObject(response);

            logger.LogInformation($"Binance response received for BTC price: {response}");
            return data.price;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch BTC price from Binance");
            throw;
        }
    }

    static async Task<decimal> GetPreviousPriceFromBinance(ILogger logger) 
    {
        try
        {
            logger.LogInformation("Making request to Binance API for previous BTC price...");
            string url = "https://api.binance.com/api/v3/klines?symbol=BTCUSDT&interval=1d&limit=2";
            var response = await client.GetStringAsync(url);
            dynamic data = JsonConvert.DeserializeObject(response);

            logger.LogInformation($"Binance response received for previous BTC price: {response}");
            return data[0][4]; // Índice 4 = precio de cierre
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch previous BTC price from Binance");
            throw;
        }
    }

    static async Task SendTelegramMessage(ILogger logger, string message)
    {
        try
        {
            logger.LogInformation("Preparing to send Telegram message...");
            var botClient = new TelegramBotClient(TelegramToken);

            if (string.IsNullOrEmpty(TelegramToken))
            {
                logger.LogError("Telegram Bot Token is null or empty. Cannot send message.");
                return;
            }

            if (string.IsNullOrEmpty(ChatIdG))
            {
                logger.LogError("Chat ID is null or empty. Cannot send message.");
                return;
            }

            logger.LogInformation("Sending Telegram message...");
            var sendResponse = await botClient.SendMessage(ChatIdG, message);

            if (sendResponse != null && sendResponse.MessageId != 0)
            {
                logger.LogInformation("Telegram message sent successfully");
            }
            else
            {
                logger.LogError("Telegram message was not sent successfully. Response: " + sendResponse?.MessageId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send Telegram message");
            throw;
        }
    }
}

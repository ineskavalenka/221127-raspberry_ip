using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Diagnostics;

namespace TelegramBotExperiments
{

    /*
     * Deploying a self-contained app https://learn.microsoft.com/en-us/dotnet/iot/deployment
     * dotnet publish --runtime linux-arm --self-contained
     * 
     * D:\helloworld\221127-telegram-ip>
     * dotnet publish --runtime linux-arm64 --self-contained -p:PublishSingleFile=true
     * 
     * 
     */
    class Program
    {
        static ITelegramBotClient bot = new TelegramBotClient("token");

        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var rmu = new ReplyKeyboardMarkup(new KeyboardButton("/ping"));
            await botClient.SendTextMessageAsync(update.Message.Chat.Id, "hi", ParseMode.Html, null, false, false, false, null, true, rmu);

            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                if (message is null || message.Text is null)
                    return;

                if (message.Text.ToLower() == "/ping")
                {
                    try
                    {
                        // await botClient.SendTextMessageAsync(message.Chat, $"Platform: {Environment.OSVersion.Platform}");

                        //if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        //{
                        //    await botClient.SendTextMessageAsync(message.Chat, $"Detected Windows platform");
                        //    await botClient.SendTextMessageAsync(message.Chat, $"Platform not supported");
                        //}
                        //else
                        if (Environment.OSVersion.Platform == PlatformID.Unix)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, $"Detected Unix platform");

                            //var public_ip = Execute("host", "myip.opendns.com resolver1.opendns.com | grep \"myip.opendns.com has\" | awk '{print $4}'");
                            var public_ip = Execute("/bin/sh", "./showext");
                            await botClient.SendTextMessageAsync(message.Chat, $"public_ip: {public_ip}");

                            //var internal_ip = Execute("ip", "addr show wlan0 | grep \"inet\\b\" | awk '{print $2}' | cut -d/ -f1");

                            var internal_ip = Execute("/bin/sh", "./showlan");
                            await botClient.SendTextMessageAsync(message.Chat, $"internal_ip: {internal_ip}");

                            //ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = "/dev/init.d/mnw stop", };
                            //Process proc = new Process() { StartInfo = startInfo, };
                            //proc.Start();
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat, $"Platform not supported");
                        }
                    }
                    catch (Exception ex)
                    {
                        await botClient.SendTextMessageAsync(message.Chat, $"Error: {ex.Message}");
                    }

                    return;
                }
            }
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }

        static void Main(string[] args)
        {
            // todo chmod

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.ReadLine();
        }

        public static string Execute(string cmd, string args)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = cmd,
                Arguments = args //Arguments = $"-c \"{args}\""
            });
            process?.WaitForExit();

            var stream = process.ExitCode == 0 ? process.StandardOutput : process.StandardError;
            var result = stream.ReadToEnd();

            if (process.ExitCode != 0 && process.ExitCode != 1)
                throw new ArgumentException(nameof(cmd), result);

            return result;
        }
    }
}
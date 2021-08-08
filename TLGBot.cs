using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AForge.WindowsForms;
using NeuralNetwork1;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using File = System.IO.File;

namespace Bots
{
    public class TLGBot
    {
        private Telegram.Bot.TelegramBotClient bot = null;
        private AIMLBot aiml = new AIMLBot();

        private MainForm form;
        public TLGBot(MainForm form)
        {
            
            bot = new TelegramBotClient(File.ReadAllText("TextFile.txt"));
            bot.OnMessage += Bot_OnMessageAsync;
            this.form = form;
        }

        private void Bot_OnMessageAsync(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var message = e.Message;
            if (message == null)
                return;
            Console.WriteLine(message.Text);
            var perсeptron = form.neuralNetwork;
            //  Получение файла (картинки)
            if (message.Type == Telegram.Bot.Types.Enums.MessageType.Photo)
            {
                var photoId = message.Photo.Last().FileId;
                Telegram.Bot.Types.File fl = bot.GetFileAsync(photoId).Result;

                var img = System.Drawing.Image.FromStream(bot.DownloadFileAsync(fl.FilePath).Result);

                System.Drawing.Bitmap bm = new System.Drawing.Bitmap(img);

                //  Масштабируем aforge
                Sample sample = form.GetSample(SignType.Undefined, bm, true);
                string result = perсeptron.Predict(sample).ToString().ToLower();

                using (var fileStream = new FileStream($"test_images\\{ result }.png",
                    FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var inputOnlineFile = new InputOnlineFile(fileStream);
                    inputOnlineFile.FileName = "StupidImage.jpg";
                    var x = bot.SendPhotoAsync(
                        chatId: message.Chat.Id,
                        photo: inputOnlineFile,
                        caption: aiml.Talk(message.Chat, result)
                    ).Result;
                }
            }

            if (message.Type == MessageType.Video)
            {
                bot.SendTextMessageAsync(message.Chat.Id, aiml.Talk(message.Chat, "Вот видео"));
            }
            if (message.Type == MessageType.Audio)
            {
                bot.SendTextMessageAsync(message.Chat.Id, aiml.Talk(message.Chat, "Вот аудио"));
            }

            if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text) return;
            if (message.Text == "Authors")
            {
                string authors = "Чумаков, Кафтанов";
                bot.SendTextMessageAsync(message.Chat.Id, "Авторы проекта : " + authors);
            }
            else
            {
                bot.SendTextMessageAsync(message.Chat.Id, aiml.Talk(message.Chat, message.Text));
            }
        }

        public bool Act()
        {
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Ssl3 |
                SecurityProtocolType.Tls |
                SecurityProtocolType.Tls11 |
                SecurityProtocolType.Tls12;

            try
            {
                bot.StartReceiving();
                var me = bot.GetMeAsync().Result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            Console.WriteLine("TELEGRAM");
            return true;
        }

        public void Stop()
        {
            bot.StopReceiving();
        }

    }
}

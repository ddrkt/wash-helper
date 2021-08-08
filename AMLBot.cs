using System;
using System.Collections.Generic;
using System.IO;
using AIMLbot;
using Telegram.Bot.Types;
using User = AIMLbot.User;

namespace NeuralNetwork1
{
    class AIMLBot
    {
        Bot myBot;
        Dictionary<long, User> users = new Dictionary<long, User>();

        public AIMLBot()
        {
            myBot = new Bot();
            myBot.loadSettings();
            myBot.isAcceptingUserInput = false;
            myBot.loadAIMLFromFiles();
            myBot.isAcceptingUserInput = true;
        }

        public string Talk(Chat chat, string phrase)
        {
            var result = "";
            User user = null;
            if (!users.ContainsKey(chat.Id))
            {
                user = new User(chat.Id.ToString(), myBot);
                users.Add(chat.Id, user);
                Request r = new Request($"Меня зовут {chat.FirstName}", user, myBot);
                result += myBot.Chat(r).Output + System.Environment.NewLine;
            }
            else
            {
                user = users[chat.Id];
            }
            result += myBot.Chat(new Request(phrase, user, myBot)).Output;
            return result;
        }
    }
}
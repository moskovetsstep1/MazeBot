﻿using System;
using System.Text.RegularExpressions;
using MazeGenerator.Database;
using MazeGenerator.Models;
using MazeGenerator.Models.Enums;
using MazeGenerator.TelegramBot.Models;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MazeGenerator.TelegramBot
{
    public class MazeBot
    {
        public readonly TelegramBotClient BotClient;
        CharacterRepository a = new CharacterRepository();


        public MazeBot(string _tMaze)
        {
            BotClient = new TelegramBotClient(_tMaze); //{"Timeout":"00:01:40","IsReceiving":true,"MessageOffset":0}
            BotClient.OnMessage += OnNewMessage;
            BotClient.StartReceiving();
            Console.ReadLine();
        }

        public void OnNewMessage(object sender, MessageEventArgs e)
        {
            int playerId = e.Message.From.Id;

            if (e.Message.Type != MessageType.Text)
                return;
            Regex login_regex = new Regex("^[a-zA-Zа-яА-Я][a-zA-Zа-яА-Я0-9]{2,9}$");
            string source = "ivanov98";

            if (login_regex.Match(source).Success)
            {
                //TODO:
            }
            else
            {

            }
            BotClient.SendChatActionAsync(playerId, ChatAction.Typing);
            if (e.Message.Text == "/start")
            {
                if (a.Read(e.Message.From.Id) == null)
                {
                    //TODO: тут будет турториал
                    a.Create(e.Message.From.Id);
                    //TODO: проверка
                    BotClient.SendTextMessageAsync(playerId, "Напишите имя персонажа");
                    //TODO: создание персонаже
                    return;
                }
                else
                {
                    BotClient.SendTextMessageAsync(playerId, "Вы хотите удалить персонажа? Для удаления напишите *Удаляю* и нажмите /start", ParseMode.Markdown);
                    return;
                }
            }

            if (a.Read(e.Message.From.Id).CharacterName != null)
            {
                if (e.Message.Text == "/game")
                {
                    if (LobbyControl.CheckLobby(playerId))
                    {
                        BotClient.SendTextMessageAsync(playerId, "Вы уже находитесь в лобби");
                    }
                    else
                    {
                        LobbyControl.AddUser(playerId);
                        if (LobbyControl.EmptyPlaceCount(playerId) == 0)
                        {
                            BotService.StartGame(playerId);
                            BotClient.SendTextMessageAsync(playerId, "Игра начата", ParseMode.Default, false, false, 0,
                                KeybordConfiguration.NewKeyBoard());
                        }
                        else
                        {
                            string m =
                                $"Вы добавлены в лобби, осталось игроков для начала игры{LobbyControl.EmptyPlaceCount(playerId)}";
                            BotClient.SendTextMessageAsync(playerId, m, ParseMode.Default, false, false, 0,
                                KeybordConfiguration.NewKeyBoard());
                        }
                    }
                }
            }
            else
            {
                var r = a.Read(e.Message.From.Id);
                r.CharacterName = e.Message.Text;
                a.Update(r);
                BotClient.SendTextMessageAsync(playerId, "Имя задано", ParseMode.Markdown);
            }

            if (!LobbyControl.CheckLobby(playerId))
            {
                BotClient.SendTextMessageAsync(playerId, "Вы не находитесь ни в одной из игр, воспользуйтесь командой /game для поиска игры");
                return;
            }
            if (e.Message.Text == "/afk")
            {
                AfkComm(e);
            }
            if (e.Message.Text == "Вверх")
            {
                MComm(e, Direction.North);
            }
            if (e.Message.Text == "Вниз")
            {
                MComm(e, Direction.South);
            }
            if (e.Message.Text == "Влево")
            {
                MComm(e, Direction.West);
            }
            if (e.Message.Text == "Вправо")
            {
                MComm(e, Direction.East);
            }
            if (e.Message.Text == "Удар кинжалом")
            {
                StabComm(e);
            }
            if (e.Message.Text == "Пропуск хода")
            {
                SkipComm(e);
            }
            if (e.Message.Text == "Выстрел")
            {
                var inlineKeyboard = KeybordConfiguration.ChooseDirectionKeyboard();
                BotClient.SendTextMessageAsync(playerId, "Выбирай направление", replyMarkup: inlineKeyboard);
                BotClient.OnCallbackQuery += BotClient_OnCallbackQueryShoot;


            }
            if (e.Message.Text == "Взрыв стены")
            {
                var inlineKeyboard = KeybordConfiguration.ChooseDirectionKeyboard();
                BotClient.SendTextMessageAsync(playerId, "Выбирай направление", replyMarkup: inlineKeyboard);
                BotClient.OnCallbackQuery += BotClient_OnCallbackQueryBomb;
            }

        }

        private void BotClient_OnCallbackQueryShoot(object sender, CallbackQueryEventArgs e)
        {
            if (e.CallbackQuery.Data != "0")
            {
                BotClient.OnCallbackQuery -= BotClient_OnCallbackQueryShoot;
                switch (e.CallbackQuery.Data)
                {
                    case "1":
                        SComm(e, Direction.North);
                        break;
                    case "2":
                        SComm(e, Direction.West);
                        break;
                    case "3":
                        SComm(e, Direction.East);
                        break;
                    case "4":
                        SComm(e, Direction.South);
                        break;
                }
            }
            else
            {
                BotClient.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id,
                    "Выбирайте направление а не пустые кнопки");
            }
        }
        private void BotClient_OnCallbackQueryBomb(object sender, CallbackQueryEventArgs e)
        {
            if (e.CallbackQuery.Data != "0")
            {
                BotClient.OnCallbackQuery -= BotClient_OnCallbackQueryBomb;
                switch (e.CallbackQuery.Data)
                {
                    case "1":
                        BComm(e, Direction.North);
                        break;
                    case "2":
                        BComm(e, Direction.West);
                        break;
                    case "3":
                        BComm(e, Direction.East);
                        break;
                    case "4":
                        BComm(e, Direction.South);
                        break;
                }
            }
            else
            {
                BotClient.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id,
                    "Выбирайте направление а не пустые кнопки");
            }
        }
        //TODO: следующим методам добавить, что б отправляли всем игрокам

        public void AfkComm(MessageEventArgs e)
        {
            var s = BotService.AfkCommand(e);
            BotClient.SendTextMessageAsync(e.Message.From.Id, s.Answer);
        }



        public void StabComm(MessageEventArgs e)
        {
            var s = BotService.StabCommand(e.Message.From.Id);
            BotClient.SendTextMessageAsync(e.Message.From.Id, s.Answer);
        }
        public void SkipComm(MessageEventArgs e)
        {
            var s = BotService.SkipTurn(e.Message.From.Id);
            BotClient.SendTextMessageAsync(e.Message.From.Id, s.Answer);
        }
        public void MComm(MessageEventArgs e, Direction direction)
        {
            var s = BotService.MoveCommand(e.Message.From.Id, direction, e.Message.From.Username);
            if (s.KeyBoardId != KeyBoardEnum.Move)
            {
                BotClient.SendTextMessageAsync(e.Message.From.Id, s.Answer, ParseMode.Default, false, false, 0,
                    KeybordConfiguration.NewKeyBoard());
            }
            else
            {
                BotClient.SendTextMessageAsync(e.Message.From.Id, s.Answer, ParseMode.Markdown);
            }
        }
        public void SComm(CallbackQueryEventArgs e, Direction direction)
        {
            var s = BotService.ShootCommand(e.CallbackQuery.From.Id, direction, e.CallbackQuery.From.Username);
            BotClient.SendTextMessageAsync(e.CallbackQuery.From.Id, s.Answer, ParseMode.Default, false, false, 0,
            KeybordConfiguration.NewKeyBoard());
        }
        public void BComm(CallbackQueryEventArgs e, Direction direction)
        {
            var s = BotService.BombCommand(e.CallbackQuery.From.Id, direction, e.CallbackQuery.From.Username);
            BotClient.SendTextMessageAsync(e.CallbackQuery.From.Id, s.Answer, ParseMode.Default, false, false, 0,
                KeybordConfiguration.NewKeyBoard());
        }
    }
}
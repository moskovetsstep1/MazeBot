﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MazeGenerator.Core;
using MazeGenerator.Core.Services;
using MazeGenerator.Core.Tools;
using MazeGenerator.Database;
using MazeGenerator.Models;
using MazeGenerator.Models.Enums;
using MazeGenerator.TelegramBot.Models;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MazeGenerator.TelegramBot
{
    public class MazeBot
    {
        public readonly TelegramBotClient BotClient;
        private readonly CharacterRepository _characterRepository = new CharacterRepository();


        public MazeBot(string token)
        {
            BotClient = new TelegramBotClient(token); //{"Timeout":"00:01:40","IsReceiving":true,"MessageOffset":0}
            BotClient.OnMessage += OnNewMessage;
            BotClient.StartReceiving();
        }

        public void OnNewMessage(object sender, MessageEventArgs e)
        {
            
            int playerId = e.Message.From.Id;
            var character = _characterRepository.Read(playerId);


            if (e.Message.Type != MessageType.Text)
                return;
            BotClient.SendChatActionAsync(playerId, ChatAction.Typing);

            //=====
            List<MessageConfig> msg = null;
            try
            {
                
                if (character == null)
                {
                    msg = StateMachine(CharacterState.NewCharacter, e.Message.Text, playerId);
                }
                else
                {
                    msg = StateMachine(_characterRepository.Read(playerId).State, e.Message.Text, playerId);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return;
            }

            //    if (character == null)
            //    {
            //        msg = StateMachine(CharacterState.NewCharacter, e.Message.Text, playerId);
            //    }
            //    else
            //    {
            //        msg = StateMachine(_characterRepository.Read(playerId).State, e.Message.Text, playerId);
            //    }
            if (msg != null)
            {
                foreach (var item in msg)
                {
                    if (item.KeyBoardId != null)
                    {
                        BotClient.SendTextMessageAsync(item.PlayerId, item.Answer, ParseMode.Markdown, false,false,0, item.KeyBoardId);
                    }
                    else
                    {
                        BotClient.SendTextMessageAsync(item.PlayerId, item.Answer, ParseMode.Markdown);
                    }

                }
                    return;
            }
        }

        private void BotClient_OnCallbackQueryShoot(object sender, CallbackQueryEventArgs e)
        {
            var bot = (TelegramBotClient) sender;
            bot.OnCallbackQuery -= BotClient_OnCallbackQueryShoot;
            switch (e.CallbackQuery.Data)
            {
                case "1":
                    SComm(e.CallbackQuery.From.Id, Direction.North);
                    break;
                case "2":
                    SComm(e.CallbackQuery.From.Id, Direction.West);
                    break;
                case "3":
                    SComm(e.CallbackQuery.From.Id, Direction.East);
                    break;
                case "4":
                    SComm(e.CallbackQuery.From.Id, Direction.South);
                    break;
                default:
                    bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id,
                        "Выбирайте направление а не пустые кнопки");
                    break;
            }
        }

        private void BotClient_OnCallbackQueryBomb(object sender, CallbackQueryEventArgs e)
        {
            var bot = (TelegramBotClient) sender;
            bot.OnCallbackQuery -= BotClient_OnCallbackQueryBomb;
            switch (e.CallbackQuery.Data)
            {
                case "1":
                    BComm(e.CallbackQuery.From.Id, Direction.North);
                    break;
                case "2":
                    BComm(e.CallbackQuery.From.Id, Direction.West);
                    break;
                case "3":
                    BComm(e.CallbackQuery.From.Id, Direction.East);
                    break;
                case "4":
                    BComm(e.CallbackQuery.From.Id, Direction.South);
                    break;
                default:
                    bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id,
                        "Выбирайте направление а не пустые кнопки");
                    break;
            }
        }
        public ReplyKeyboardMarkup GetKeyboardMarkup(KeyboardType keyBoardId)
        {
            switch (keyBoardId)
            {
                case KeyboardType.Bomb:
                    return KeybordConfiguration.WithoutShootKeyBoard();
                case KeyboardType.Move:
                    return KeybordConfiguration.WithoutBombAndShootKeyboard();
                case KeyboardType.Shoot:
                    return KeybordConfiguration.WithoutBombKeyBoard();
                case KeyboardType.ShootwithBomb:
                    return KeybordConfiguration.NewKeyBoard();
                default:
                    throw new ArgumentException(keyBoardId.ToString());
            }
        }

        public void SComm(int playerid, Direction direction)
        {
            var s = BotService.ShootCommand(playerid, direction);
            foreach (var item in s)
            {
                if (item.KeyBoardId != null)
                {
                    BotClient.SendTextMessageAsync(item.PlayerId, item.Answer, ParseMode.Markdown, false, false, 0, item.KeyBoardId);
                }
                else
                {
                    BotClient.SendTextMessageAsync(item.PlayerId, item.Answer, ParseMode.Markdown);
                }

            }
        }
        public void BComm(int playerId, Direction direction)
        {
            var s = BotService.BombCommand(playerId, direction);
            foreach (var item in s)
            {
                if (item.KeyBoardId != null)
                {
                    BotClient.SendTextMessageAsync(item.PlayerId, item.Answer, ParseMode.Markdown, false, false, 0, item.KeyBoardId);
                }
                else
                {
                    BotClient.SendTextMessageAsync(item.PlayerId, item.Answer, ParseMode.Markdown);
                }

            }
        }

        public List<MessageConfig> StateMachine(CharacterState state, string command, int playerId)
        {
            switch (state)
            {
                case CharacterState.ChangeName:
                    return BotService.TryChangeName(command, playerId);

                case CharacterState.ChangeGameMode:
                    if (command == "/game")
                    {
                        return BotService.FindGameCommand(playerId);
                    }
                    else if (command == "/tutorial")
                    {
                        var character = _characterRepository.Read(playerId);
                        LobbyRepository lobbyRepository = new LobbyRepository();
                        var lobby = lobbyRepository.Read(0);
                        lobby.Players.Add(new Player{ Health = 3, HeroName = character.CharacterName, Rotate = Direction.North, TelegramUserId = playerId, UserCoordinate = new Coordinate(3, 3)});
                        lobbyRepository.Update(lobby);
                        character.State = CharacterState.Tutorial;
                        _characterRepository.Update(character);
                    }
                    else
                    {
                        return new List<MessageConfig>
                        {
                            new MessageConfig()
                            {
                                Answer = "неверная команда",
                                PlayerId =  playerId
                            }
                        };
                    }
                    break;

                case CharacterState.Tutorial:
                    if (command == "Вверх")
                    {
                        return TutorialService.MoveCommand(playerId, Direction.North);
                    }
                    else if (command == "Вниз")
                    {
                        return TutorialService.MoveCommand(playerId, Direction.South);
                    }
                    else if (command == "Вправо")
                    {
                        return TutorialService.MoveCommand(playerId, Direction.East);
                    }
                    else if (command == "Влево")
                    {
                        return TutorialService.MoveCommand(playerId, Direction.West);
                    }
                    else if (command == "Взрыв стены")
                    {
                        var inlineKeyboard = KeybordConfiguration.ChooseDirectionKeyboard();
                        BotClient.SendTextMessageAsync(playerId, "Выбирай направление", replyMarkup: inlineKeyboard);
                        BotClient.OnCallbackQuery += BotClient_OnCallbackQueryBomb;
                        return null;
                    }
                    else if (command == "/skiptutorial")
                    {
                        throw new NotImplementedException();
                        //TODO: выйти из туториала
                    }
                    else
                    {
                        throw new NotImplementedException();
                        //TODO: сообщение, что неверная комманда
                    }

                case CharacterState.FindGame:
                    if (command == "/help")
                    {
                        throw new NotImplementedException();
                    }
                    else if (command == "/stop")
                    {
                        _characterRepository.Read(playerId);
                        MemberRepository repo =new MemberRepository();
                        var character = _characterRepository.Read(playerId);
                        character.State = CharacterState.ChangeGameMode;
                        _characterRepository.Update(character);
                        repo.DeleteOne(playerId);
                        return new List<MessageConfig>
                        {
                            new MessageConfig()
                            {
                                Answer = "Вы удалены из очереди",
                                PlayerId =  playerId
                            }
                        };
                    }
                    else
                    {
                        throw new NotImplementedException();
                        //TODO: сообщение, что неверная комманда
                    }

                case CharacterState.InGame:
                    switch (command)
                    {
                        case "Вверх":
                            return BotService.MoveCommand(playerId, Direction.North);
                        case "Вниз":
                            return BotService.MoveCommand(playerId, Direction.South);
                        case "Вправо":
                            return BotService.MoveCommand(playerId, Direction.East);
                        case "Влево":
                            return BotService.MoveCommand(playerId, Direction.West);
                        case "Удар кинжалом":
                            return BotService.StabCommand(playerId);
                        case "Пропуск хода":
                            return BotService.SkipTurn(playerId);
                        case "Выстрел":
                        {
                            var inlineKeyboard = KeybordConfiguration.ChooseDirectionKeyboard();
                            BotClient.SendTextMessageAsync(playerId, "Выбирай направление", replyMarkup: inlineKeyboard);
                            BotClient.OnCallbackQuery += BotClient_OnCallbackQueryShoot;
                            return null;
                        }
                        case "Взрыв стены":
                        {
                            var inlineKeyboard = KeybordConfiguration.ChooseDirectionKeyboard();
                            BotClient.SendTextMessageAsync(playerId, "Выбирай направление", replyMarkup: inlineKeyboard);
                            BotClient.OnCallbackQuery += BotClient_OnCallbackQueryBomb;
                            return null;
                        }
                        case "/afk":
                            return BotService.AfkCommand(playerId);
                    }

                    return new List<MessageConfig>
                    {
                        new MessageConfig()
                        {
                            Answer = Answers.UndefinedCommand.RandomAnswer(),
                            PlayerId =  playerId
                        }
                    };

                case CharacterState.NewCharacter:
                    if (command == "/start")
                    {
                        if (_characterRepository.Read(playerId) == null)
                        {
                            _characterRepository.Create(playerId);
                            return new List<MessageConfig>
                            {
                                new MessageConfig()
                                {
                                    Answer = "Напишите имя персонажа",
                                    PlayerId = playerId
                                }
                            };
                        }

                        return new List<MessageConfig>
                        {
                            new MessageConfig()
                            {
                                Answer = "Вы хотите удалить персонажа? Для удаления напишите *Удаляю* и нажмите /start",
                                PlayerId = playerId
                            }
                        };
                    }
                    return new List<MessageConfig>
                    {
                        new MessageConfig()
                        {
                            Answer = "Неверная команда",
                            PlayerId = playerId
                        }
                    };

                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
               
            }
            throw new Exception();
        }
    }
}
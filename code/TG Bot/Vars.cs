using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bots.Types.Passport;
using Npgsql;

namespace TG_Bot
{
    public static class connection_strings
    {
        public static string bot_token = "";
        public static string db_connect = "Host=;Username=;Password=;Database=;Port=";
    }
    public static class Keyboard_Markups
    {
        public static ReplyKeyboardMarkup Main_Menu_Reply_Keyboard = new ReplyKeyboardMarkup(
    new List<KeyboardButton[]>()
    {
                                        new KeyboardButton []{
                                        new KeyboardButton("Данные о себе"),
                                        new KeyboardButton("Еда"),
                                        new KeyboardButton("Напоминания"),
                                        new KeyboardButton("Расчеты")
                                        }
    }
       )
        {
            ResizeKeyboard = true,
        };
        public static ReplyKeyboardMarkup user_Reply_Keyboard = new ReplyKeyboardMarkup(
                new List<KeyboardButton[]>()
                {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Изменить данные о себе"),
                                            new KeyboardButton("Данные о себе сейчас")
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Вернуться")
                                        }
                })
        {
            ResizeKeyboard = true,
        };
        public static ReplyKeyboardMarkup Dishes_Reply_Keyboard = new ReplyKeyboardMarkup(
            new List<KeyboardButton[]>()
            {
                                    new KeyboardButton[]
                                        {
                                            new KeyboardButton("Добавить блюдо"),
                                            new KeyboardButton("Удалить блюдо")
                                        },
                                            new KeyboardButton[]
                                        {
                                            new KeyboardButton("Создать меню на сегодня")
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Вернуться")
                                        }
                })
        {
            ResizeKeyboard = true,
        };
        public static ReplyKeyboardMarkup Reminders_Reply_Keyboard = new ReplyKeyboardMarkup(
            new List<KeyboardButton[]>()
            {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Настроить график питания"),
                                            new KeyboardButton("Настроить график водного баланса")
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Вернуться")
                                        }
            })
        {
            ResizeKeyboard = true,
        };
        public static ReplyKeyboardMarkup Counts_Reply_Keyboard = new ReplyKeyboardMarkup(
            new List<KeyboardButton[]>()
            {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Посчитать водный баланс"),
                                            new KeyboardButton("Посчитать рекомендуемое количество калорий в день")
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Вернуться")
                                        }
        })
        {
            ResizeKeyboard = true,
        };
        public static ReplyKeyboardMarkup user_Change_Reply_Keyboard = new ReplyKeyboardMarkup(
                new List<KeyboardButton[]>()
                {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Рост"),
                                            new KeyboardButton("Вес")
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Количество воды за день"),
                                            new KeyboardButton("Количество калорий за день")
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Возраст"),
                                            new KeyboardButton("Пол")
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Вернуться")
                                        }
})
        {
            ResizeKeyboard = true,
        };
        public static ReplyKeyboardMarkup Gender_Change_Reply_Keyboard = new ReplyKeyboardMarkup(
                new List<KeyboardButton[]>()
                {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("М"),
                                            new KeyboardButton("Ж")
                                        }
})
        {
            ResizeKeyboard = true,
        };
    }
    public class User_data
    {        
    //public string user_state;
    public double calories_sum;
    public List<string> List_Of_Dishes = new List<string>();
    //public long chat_id;
    public long chat_id{ get; set; }
    public string user_state { get; set; }
    }
}



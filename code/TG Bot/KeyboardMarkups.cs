using Telegram.Bot.Types.ReplyMarkups;

namespace TG_Bot;

public static class KeyboardMarkups
{
    public static readonly ReplyKeyboardMarkup MainMenuReplyKeyboard = new(
        new List<KeyboardButton[]>
        {
            new[]
            {
                new("Данные о себе"),
                new KeyboardButton("Еда"),
                new KeyboardButton("Напоминания"),
                new KeyboardButton("Расчеты")
            }
        }
    )
    {
        ResizeKeyboard = true
    };

    public static readonly ReplyKeyboardMarkup UserReplyKeyboard = new(
        new List<KeyboardButton[]>
        {
            new[]
            {
                new("Изменить данные о себе"),
                new KeyboardButton("Данные о себе сейчас")
            },
            new[]
            {
                new KeyboardButton("Вернуться")
            }
        })
    {
        ResizeKeyboard = true
    };

    public static readonly ReplyKeyboardMarkup DishesReplyKeyboard = new(
        new List<KeyboardButton[]>
        {
            new[]
            {
                new("Добавить блюдо"),
                new KeyboardButton("Удалить блюдо")
            },
            new[]
            {
                new KeyboardButton("Создать меню на завтра")
            },
            new[]
            {
                new KeyboardButton("Я поел"),
                new KeyboardButton("Я попил")
            },
            new[]
            {
                new KeyboardButton("Вернуться")
            }
        })
    {
        ResizeKeyboard = true
    };

    public static readonly ReplyKeyboardMarkup RemindersReplyKeyboard = new(
        new List<KeyboardButton[]>
        {
            new[]
            {
                new("Настроить график питания"),
                new KeyboardButton("Настроить график питья")
            },
            new[]
            {
                new KeyboardButton("Вернуться")
            }
        })
    {
        ResizeKeyboard = true
    };

    public static readonly ReplyKeyboardMarkup CountsReplyKeyboard = new(
        new List<KeyboardButton[]>
        {
            new[]
            {
                new("Посчитать водный баланс"),
                new KeyboardButton("Посчитать рекомендуемое количество калорий в день")
            },
            new[]
            {
                new KeyboardButton("Вернуться")
            }
        })
    {
        ResizeKeyboard = true
    };

    public static readonly ReplyKeyboardMarkup UserChangeReplyKeyboard = new(
        new List<KeyboardButton[]>
        {
            new[]
            {
                new("Рост"),
                new KeyboardButton("Пол")
            },
            new[]
            {
                new KeyboardButton("Количество воды за день"),
                new KeyboardButton("Количество калорий за день")
            },
            new[]
            {
                new KeyboardButton("Возраст"),
                new KeyboardButton("Вес")
            },
            new[]
            {
                new KeyboardButton("Вернуться")
            }
        })
    {
        ResizeKeyboard = true
    };

    public static readonly ReplyKeyboardMarkup GenderChangeReplyKeyboard = new(
        new List<KeyboardButton[]>
        {
            new[]
            {
                new("М"),
                new KeyboardButton("Ж")
            }
        })
    {
        ResizeKeyboard = true
    };
}
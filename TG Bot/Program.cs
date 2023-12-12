using Npgsql;
using System.Data;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TG_Bot;

using CancellationTokenSource cts = new();
string user_state = "";
string add_dish_string = "";
var botClient = new TelegramBotClient(connection_strings.bot_token);
var connectionString = connection_strings.db_connect;

await using var dataSource = NpgsqlDataSource.Create(connectionString);
await using var connection = await dataSource.OpenConnectionAsync();
// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Обязательно ставим блок try-catch, чтобы наш бот не "падал" в случае каких-либо ошибок
    try
    {
        // Сразу же ставим конструкцию switch, чтобы обрабатывать приходящие Update
        switch (update.Type)
        {
            case UpdateType.Message:
                {
                    // эта переменная будет содержать в себе все связанное с сообщениями
                    var message = update.Message;
                    // Добавляем проверку на тип Message
                    switch (message.Type)
                    {
                        // Тут понятно, текстовый тип
                        case MessageType.Text:
                            {
                                var replyKeyboard_1 = new ReplyKeyboardMarkup(
                                        new List<KeyboardButton[]>()
                                        {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Изменить данные о себе"),
                                            new KeyboardButton("Данные о себе сейчас")
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Добавить блюдо"),
                                            new KeyboardButton("Удалить блюдо")
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Настроить график питания"),
                                            new KeyboardButton("Настроить график водного баланса")
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Создать рацион"),
                                            new KeyboardButton("Посчитать калорийность рациона")
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Посчитать водный баланс"),
                                            new KeyboardButton("Посчитать рекомендуемое количество калорий в день")
                                        }
                                })
                                {
                                    ResizeKeyboard = true,
                                };
                                var replyKeyboard_2 = new ReplyKeyboardMarkup(
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
                                var replyKeyboard_3 = new ReplyKeyboardMarkup(
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
                                bool chat_id_exists = false;
                                if (user_state == "water_per_day_change")
                                {
                                    using (var sql_command2 = dataSource.CreateCommand("UPDATE users set water_per_day = " + message.Text + " where chat_id = " + message.Chat.Id))
                                    {
                                        await sql_command2.ExecuteNonQueryAsync();
                                    };
                                    await botClient.SendTextMessageAsync(message.Chat, "Рост изменен, поменяем что-нибудь ещё?", replyMarkup: replyKeyboard_2);
                                    user_state = "general_data_change";
                                    return;
                                }
                                if (user_state == "calories_per_day_change")
                                {
                                    using (var sql_command2 = dataSource.CreateCommand("UPDATE users set calories_per_day = " + message.Text + " where chat_id = " + message.Chat.Id))
                                    {
                                        await sql_command2.ExecuteNonQueryAsync();
                                    };
                                    await botClient.SendTextMessageAsync(message.Chat, "Рост изменен, поменяем что-нибудь ещё?", replyMarkup: replyKeyboard_2);
                                    user_state = "general_data_change";
                                    return;
                                }
                                if (user_state == "height_change")
                                {
                                    using (var sql_command2 = dataSource.CreateCommand("UPDATE users set height = " + message.Text + " where chat_id = " + message.Chat.Id))
                                    {
                                        await sql_command2.ExecuteNonQueryAsync();
                                    };
                                    await botClient.SendTextMessageAsync(message.Chat, "Рост изменен, поменяем что-нибудь ещё?", replyMarkup: replyKeyboard_2);
                                    user_state = "general_data_change";
                                    return;
                                }
                                if (user_state == "weight_change")
                                {
                                    using (var sql_command2 = dataSource.CreateCommand("UPDATE users set weight = " + message.Text + " where chat_id = " + message.Chat.Id))
                                    {
                                        await sql_command2.ExecuteNonQueryAsync();
                                    };
                                    await botClient.SendTextMessageAsync(message.Chat, "Вес изменен, поменяем что-нибудь ещё?", replyMarkup: replyKeyboard_2);
                                    user_state = "general_data_change";
                                    return;
                                }
                                if (user_state == "age_change")
                                {
                                    using (var sql_command2 = dataSource.CreateCommand("UPDATE users set age = " + message.Text + " where chat_id = " + message.Chat.Id))
                                    {
                                        await sql_command2.ExecuteNonQueryAsync();
                                    };
                                    await botClient.SendTextMessageAsync(message.Chat, "Возраст изменен, поменяем что-нибудь ещё?", replyMarkup: replyKeyboard_2);
                                    user_state = "general_data_change";
                                    return;
                                }
                                if (user_state == "gender_change")
                                {
                                    using (var sql_command2 = dataSource.CreateCommand("UPDATE users set gender = '" + message.Text + "' where chat_id = " + message.Chat.Id))
                                    {
                                        await sql_command2.ExecuteNonQueryAsync();
                                    };
                                    await botClient.SendTextMessageAsync(message.Chat, "Пол изменен, поменяем что-нибудь ещё?", replyMarkup: replyKeyboard_2);
                                    user_state = "general_data_change";
                                    return;
                                }
                                if (user_state == "dish_add_weight")
                                {
                                    add_dish_string += $"{message.Text})";
                                    Console.WriteLine(add_dish_string);
                                    using var sql_command2 = dataSource.CreateCommand(add_dish_string);
                                    await sql_command2.ExecuteNonQueryAsync();
                                    await botClient.SendTextMessageAsync(message.Chat, "Блюдо добавлено", replyMarkup: replyKeyboard_1);
                                    user_state = "";
                                    add_dish_string = "";
                                }
                                if (user_state == "dish_add_calories")
                                {
                                    await botClient.SendTextMessageAsync(message.Chat, "Введите средний вес порции");
                                    add_dish_string += $"{message.Text},";
                                    user_state = "dish_add_weight";
                                }
                                if (user_state == "dish_add_type")
                                {
                                    await botClient.SendTextMessageAsync(message.Chat, "Введите калорийность блюда на 100 грамм");
                                    user_state = "dish_add_calories";
                                }
                                if (user_state == "dish_add_name")
                                {

                                    await botClient.SendTextMessageAsync(message.Chat, "Введите тип блюда, например");
                                    add_dish_string = $"INSERT INTO fooditem (name, calories, weight) VALUES ('{message.Text}',";
                                    user_state = "dish_add_type";
                                }
                                if (message.Text == "/start")
                                {
                                    await using (var sql_command1 = dataSource.CreateCommand("SELECT EXISTS (SELECT 1 FROM users WHERE chat_id = "+ message.Chat.Id + ")"))
                                    await using (var reader = await sql_command1.ExecuteReaderAsync())
                                    {
                                        while (await reader.ReadAsync())
                                        {
                                           chat_id_exists = reader.GetBoolean(0);
                                        }
                                    };

                                    if (!chat_id_exists)
                                    {
                                        await botClient.SendTextMessageAsync(message.Chat, "Привет, " + message.From.Username + ". Я тебя ещё не знаю, но очень хочу познакомиться\n" +
                                                                                            "Пожалуйста, добавь свои данные\n", replyMarkup: replyKeyboard_2);
                                         using (var sql_command2 = dataSource.CreateCommand("INSERT INTO users (chat_id) VALUES ($1)"))
                                        {
                                            sql_command2.Parameters.AddWithValue(message.Chat.Id);
                                             await sql_command2.ExecuteNonQueryAsync();
                                        };
                                    }
                                    else
                                    {
                                        await botClient.SendTextMessageAsync(message.Chat, "Привет!", replyMarkup: replyKeyboard_1);
                                    }
                                    return;
                                }
                                if (message.Text == "Добавить блюдо")
                                {
                                    user_state = "dish_add_name";
                                    await botClient.SendTextMessageAsync(message.Chat, "Введите название блюда");

                                    return;
                                }
                                if (message.Text == "Данные о себе сейчас")
                                {
                                    double user_weight = 0;
                                    int user_height = 0;
                                    int user_age = 0;
                                    double calories_per_day = 0;
                                    int calories_eaten = 0;
                                    int water_per_day = 0;
                                    int water_drunk = 0;
                                    await using (var sql_command1 = dataSource.CreateCommand("SELECT weight, height, age, calories_per_day, " +
                                        "calories_eaten, water_per_day, water_drunk FROM users WHERE chat_id = " + message.Chat.Id))
                                    await using (var reader = await sql_command1.ExecuteReaderAsync())
                                    {
                                        while (await reader.ReadAsync())
                                        {
                                            user_weight = reader.GetDouble(0);
                                            user_height = reader.GetInt32(1);
                                            user_age = reader.GetInt32(2);
                                            calories_per_day = reader.GetDouble(3);
                                            calories_eaten = reader.GetInt32(4);
                                            water_per_day = reader.GetInt32(5);
                                            water_drunk = reader.GetInt32(6);
                                        }
                                    };
                                    await botClient.SendTextMessageAsync(message.Chat, "Данные о пользователе\n" +
                                        $"Ваш вес {user_weight}\n" +
                                        $"Ваш рост {user_height}\n" +
                                        $"Ваш возраст {user_age}\n" +
                                        $"Рекомендуемое дневное количество калорий {calories_per_day}\n" +
                                        $"Потребленное количество калорий за сегодня {calories_eaten}\n" +
                                        $"Рекомендуемое количество выпитой жидкости {water_drunk}\n" +
                                        $"Выпитое за сегодня количество жидкости {water_per_day}\n");
                                }
                                if (message.Text == "Изменить данные о себе" && user_state == "")
                                {
                                    await botClient.SendTextMessageAsync(message.Chat, "Что будем менять?", replyMarkup: replyKeyboard_2);
                                    user_state = "general_data_change";
                                    return;
                                }
                                if (message.Text == "Посчитать рекомендуемое количество калорий в день")
                                {
                                    double user_weight = 0;
                                    int user_height = 0;
                                    int user_age = 0;
                                    string user_gender = null;
                                    await using (var sql_command1 = dataSource.CreateCommand("SELECT weight, height, age, gender FROM users WHERE chat_id = " + message.Chat.Id))
                                    await using (var reader = await sql_command1.ExecuteReaderAsync())
                                    {
                                        while (await reader.ReadAsync())
                                        {
                                            user_weight = reader.GetDouble(0);
                                            user_height = reader.GetInt32(1);
                                            user_age = reader.GetInt32(2);
                                            user_gender = reader.GetString(3);
                                        }
                                    };
                                    double user_recommended_calories = (user_weight * 10 + user_height * 6.25 - user_age * 5 - (user_gender == "М" ? +5 : -161)) * 1.2;
                                    using (var sql_command2 = dataSource.CreateCommand("UPDATE users set calories_per_day = " + user_recommended_calories + " where chat_id = " + message.Chat.Id))
                                    {
                                        await sql_command2.ExecuteNonQueryAsync();
                                    };
                                    await botClient.SendTextMessageAsync(message.Chat, "В среднем, рекомендуемое количество калорий в день для вас равно " + user_recommended_calories + 
                                        " ККал/день", replyMarkup: replyKeyboard_1);
                                }
                                if (message.Text == "Посчитать водный баланс")
                                {
                                    double user_weight = 0;
                                    int user_height = 0;
                                    int user_age = 0;
                                    string user_gender = null;
                                    await using (var sql_command1 = dataSource.CreateCommand("SELECT weight, height, age, gender FROM users WHERE chat_id = " + message.Chat.Id))
                                    await using (var reader = await sql_command1.ExecuteReaderAsync())
                                    {
                                        while (await reader.ReadAsync())
                                        {
                                            user_weight = reader.GetDouble(0);
                                            user_height = reader.GetInt32(1);
                                            user_age = reader.GetInt32(2);
                                            user_gender = reader.GetString(3);
                                        }
                                    };
                                    double user_recommended_calories = (user_weight * 10 + user_height * 6.25 - user_age * 5 - (user_gender == "М" ? +5 : -161)) * 1.2;
                                    
                                }
                                if (message.Text == "Количество воды за день" && user_state == "general_data_change")
                                {
                                    user_state = "water_per_day_change";
                                    return;
                                }
                                if (message.Text == "Количество калорий за день" && user_state == "general_data_change")
                                {
                                    user_state = "calories_per_day_change";
                                    return;
                                }
                                if (message.Text == "Рост" && user_state == "general_data_change")
                                {
                                    await botClient.SendTextMessageAsync(message.Chat, "Введите свой рост в сантиметрах");
                                    user_state = "height_change";
                                    return;
                                }
                                if (message.Text == "Вес" && user_state == "general_data_change")
                                {
                                    await botClient.SendTextMessageAsync(message.Chat, "Введите свой вес в килограммах");
                                    user_state = "weight_change";
                                    return;
                                }
                                if (message.Text == "Возраст" && user_state == "general_data_change")
                                {
                                    await botClient.SendTextMessageAsync(message.Chat, "Введите свой возраст");
                                    user_state = "age_change";
                                    return;
                                }
                                if (message.Text == "Пол" && user_state == "general_data_change")
                                {
                                    await botClient.SendTextMessageAsync(message.Chat, "Выберите пол", replyMarkup: replyKeyboard_3);
                                    user_state = "gender_change";
                                    return;
                                }
                                if (message.Text == "Вернуться")
                                {
                                    user_state = "";
                                    double user_weight = 0;
                                    int user_height = 0;
                                    int user_age = 0;
                                    double calories_per_day = 0;
                                    int calories_eaten = 0;
                                    int water_per_day = 0;
                                    int water_drunk = 0;
                                    await using (var sql_command1 = dataSource.CreateCommand("SELECT weight, height, age, calories_per_day, " +
                                        "calories_eaten, water_per_day, water_drunk FROM users WHERE chat_id = " + message.Chat.Id))
                                    await using (var reader = await sql_command1.ExecuteReaderAsync())
                                    {
                                        while (await reader.ReadAsync())
                                        {
                                            user_weight = reader.GetDouble(0);
                                            user_height = reader.GetInt32(1);
                                            user_age = reader.GetInt32(2);
                                            calories_per_day = reader.GetDouble(3);
                                            calories_eaten = reader.GetInt32(4);
                                            water_per_day = reader.GetInt32(5);
                                            water_drunk = reader.GetInt32(6);
                                        }
                                    };
                                    await botClient.SendTextMessageAsync(message.Chat, "Данные о пользователе\n" +
                                        $"Ваш вес {user_weight}\n" +
                                        $"Ваш рост {user_height}\n" +
                                        $"Ваш возраст {user_age}\n" +
                                        $"Рекомендуемое дневное количество калорий {calories_per_day}\n" +
                                        $"Потребленное количество калорий за сегодня {calories_eaten}\n" +
                                        $"Рекомендуемое количество выпитой жидкости {water_drunk}\n" +
                                        $"Выпитое за сегодня количество жидкости {water_per_day}\n", replyMarkup:replyKeyboard_1);
                                    return;
                                }
                                return;
                            }
                        default:
                            {
                                return;
                            }
                    }
                }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}
Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

connection_strings strings = new connection_strings();
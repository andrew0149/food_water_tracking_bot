using Npgsql;
using System.Security.Cryptography.X509Certificates;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bots.Types.Inline;
using TG_Bot;

using CancellationTokenSource cts = new();
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
async Task <string> get_user_state (long chat_id)
        {
            string state = "";
            await using (var sql_command = dataSource.CreateCommand($"select user_state from users where chat_id = {chat_id}"))
            await using (var reader = await sql_command.ExecuteReaderAsync()) {
                while (await reader.ReadAsync())
                {
                    state = reader.GetString(0);
                }
            }
            return state;
        }
async Task set_user_state (long chat_id, string state)
        {
            using (var sql_command = dataSource.CreateCommand($"UPDATE users set user_state = '{state}' where chat_id = " + chat_id))
            {
                await sql_command.ExecuteNonQueryAsync();
            }
            
        }
async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Обязательно ставим блок try-catch, чтобы наш бот не "падал" в случае каких-либо ошибок
    try
    {
        User_data user = new User_data
        {
            chat_id = update.Message.Chat.Id,
            user_state = await get_user_state(update.Message.Chat.Id)
        };
        async Task <InlineKeyboardMarkup> MarkupMaker(Int64 chat_id)
        {
            user.List_Of_Dishes.Clear();
            string command = "select " + '"' + "name" + '"' + $" from fooditem where chat_id = {chat_id}";
            await using (var sql_command1 = dataSource.CreateCommand(command))
            await using (var reader = await sql_command1.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        user.List_Of_Dishes.Add(reader.GetString(i));
                    }

                }
            };
            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
            foreach (var dish in user.List_Of_Dishes)
            {
                buttons.Add(InlineKeyboardButton.WithCallbackData(dish, dish));
            }
            var Menu_Inline_Keyboard = new InlineKeyboardMarkup(buttons);
            return Menu_Inline_Keyboard;
        }
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
                                //user.user_state = await get_user_state(user.chat_id);
                                user.chat_id = message.Chat.Id;
                                bool chat_id_exists = false;
                                if (message.Text != "Вернуться")
                                {
                                    switch (user.user_state)
                                    {
                                        case "water_per_day_change":
                                            {
                                                /* using (var sql_command2 = dataSource.CreateCommand("UPDATE users set water_per_day = " + message.Text + " where chat_id = " + user.chat_id))
                                                 {
                                                     await sql_command2.ExecuteNonQueryAsync();
                                                 };
                                                 await botClient.SendTextMessageAsync(message.Chat, "Рост изменен, поменяем что-нибудь ещё?", replyMarkup: Keyboard_Markups.user_Change_Reply_Keyboard);
                                                 */
                                                user.user_state = "general_data_change";
                                                await set_user_state(user.chat_id, user.user_state);


                                                return;
                                            }
                                        case "calories_per_day_change":
                                            {
                                                using (var sql_command2 = dataSource.CreateCommand("UPDATE users set calories_per_day = " + message.Text + " where chat_id = " + user.chat_id))
                                                {
                                                    await sql_command2.ExecuteNonQueryAsync();
                                                };
                                                await botClient.SendTextMessageAsync(message.Chat, "Рост изменен, поменяем что-нибудь ещё?", replyMarkup: Keyboard_Markups.user_Change_Reply_Keyboard);
                                                user.user_state = "general_data_change";
                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                        case "height_change":
                                            {
                                                using (var sql_command2 = dataSource.CreateCommand("UPDATE users set height = " + message.Text + " where chat_id = " + user.chat_id))
                                                {
                                                    await sql_command2.ExecuteNonQueryAsync();
                                                };
                                                await botClient.SendTextMessageAsync(message.Chat, "Рост изменен, поменяем что-нибудь ещё?", replyMarkup: Keyboard_Markups.user_Change_Reply_Keyboard);
                                                user.user_state = "general_data_change";
                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                        case "weight_change":

                                            {
                                                using (var sql_command2 = dataSource.CreateCommand("UPDATE users set weight = " + message.Text + " where chat_id = " + user.chat_id))
                                                {
                                                    await sql_command2.ExecuteNonQueryAsync();
                                                };
                                                await botClient.SendTextMessageAsync(message.Chat, "Вес изменен, поменяем что-нибудь ещё?", replyMarkup: Keyboard_Markups.user_Change_Reply_Keyboard);
                                                user.user_state = "general_data_change";
                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                        case "age_change":
                                            {
                                                using (var sql_command2 = dataSource.CreateCommand("UPDATE users set age = " + message.Text + " where chat_id = " + user.chat_id))
                                                {
                                                    await sql_command2.ExecuteNonQueryAsync();
                                                };
                                                await botClient.SendTextMessageAsync(message.Chat, "Возраст изменен, поменяем что-нибудь ещё?", replyMarkup: Keyboard_Markups.user_Change_Reply_Keyboard);
                                                user.user_state = "general_data_change";
                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                        case "gender_change":

                                            {
                                                using (var sql_command2 = dataSource.CreateCommand("UPDATE users set gender = '" + message.Text + "' where chat_id = " + user.chat_id))
                                                {
                                                    await sql_command2.ExecuteNonQueryAsync();
                                                };
                                                await botClient.SendTextMessageAsync(message.Chat, "Пол изменен, поменяем что-нибудь ещё?", replyMarkup: Keyboard_Markups.user_Change_Reply_Keyboard);
                                                user.user_state = "general_data_change";
                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                        case "dish_add_weight":

                                            {
                                                user.add_dish_string += $"{message.Text}, {user.chat_id})";
                                                using var sql_command2 = dataSource.CreateCommand(user.add_dish_string);
                                                await sql_command2.ExecuteNonQueryAsync();
                                                await botClient.SendTextMessageAsync(message.Chat, "Блюдо добавлено", replyMarkup: Keyboard_Markups.user_Reply_Keyboard);
                                                user.user_state = "";
                                                await set_user_state(user.chat_id, user.user_state);

                                                user.add_dish_string = "";
                                                return;
                                            }
                                        case "dish_add_calories":

                                            {
                                                await botClient.SendTextMessageAsync(message.Chat, "Введите средний вес порции в граммах");
                                                user.add_dish_string += $"{message.Text},";
                                                await set_user_state(user.chat_id, user.user_state);

                                                user.user_state = "dish_add_weight";
                                                return;
                                            }
                                        case "dish_add_type":

                                            {
                                                await botClient.SendTextMessageAsync(message.Chat, "Введите калорийность блюда на 100 грамм");
                                                user.user_state = "dish_add_calories";
                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                        case "dish_add_name":

                                            {

                                                await botClient.SendTextMessageAsync(message.Chat, "Введите тип блюда, например");
                                                user.add_dish_string = $"INSERT INTO fooditem (name, calories, weight, chat_id) VALUES ('{message.Text}',";
                                                user.user_state = "dish_add_type";
                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                        case "water_reminder_change_3":
                                            {
                                                using (var sql_command = dataSource.CreateCommand($"update reminders set water_remind = current_date + time '{message.Text}:00';")) ;
                                                await botClient.SendTextMessageAsync(message.Chat, "Напоминание установлено", replyMarkup: Keyboard_Markups.Main_Menu_Reply_Keyboard);
                                                user.user_state = "";
                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                        case "water_reminder_change_2":
                                            {
                                                int water_reminders_interval = Int32.Parse(message.Text);
                                                await botClient.SendTextMessageAsync(message.Chat, "Во сколько вы хотите получить первое напоминание? (Введите время в формате hh:mm");
                                                user.user_state = "water_reminder_change_3";
                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                        case "water_reminder_change_1":
                                            {
                                                int water_reminders_count = Int32.Parse(message.Text);
                                                await botClient.SendTextMessageAsync(message.Chat, "С каким интервалом вы хотите получать напоминания? (Введите количество часов)");
                                                using (var sql_command = dataSource.CreateCommand("update reminders set water_remind_interval = interval '$1 hour'"))
                                                {
                                                    sql_command.Parameters.AddWithValue(water_reminders_count);
                                                    await sql_command.ExecuteNonQueryAsync();
                                                };
                                                user.user_state = "water_reminder_change_2";
                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                    }
                                    switch (message.Text)
                                    {
                                        case "/start":
                                            {
                                                await using (var sql_command1 = dataSource.CreateCommand("SELECT EXISTS (SELECT 1 FROM users WHERE chat_id = " + user.chat_id + ")"))
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
                                                                                                        "Пожалуйста, добавь свои данные используя встроенную клавиатуру\n", replyMarkup: Keyboard_Markups.user_Change_Reply_Keyboard);
                                                    using (var sql_command2 = dataSource.CreateCommand("INSERT INTO users (chat_id) VALUES ($1)"))
                                                    {
                                                        sql_command2.Parameters.AddWithValue(user.chat_id);
                                                        await sql_command2.ExecuteNonQueryAsync();
                                                    };
                                                    using (var sql_command3 = dataSource.CreateCommand("INSERT INTO reminders (chat_id) VALUES ($1)"))
                                                    {
                                                        sql_command3.Parameters.AddWithValue(user.chat_id);
                                                        await sql_command3.ExecuteNonQueryAsync();
                                                    };
                                                    using (var sql_command4 = dataSource.CreateCommand("INSERT INTO fooditem (chat_id) VALUES ($1)"))
                                                    {
                                                        sql_command4.Parameters.AddWithValue(user.chat_id);
                                                        await sql_command4.ExecuteNonQueryAsync();
                                                    }
                                                    using (var sql_command5 = dataSource.CreateCommand("INSERT INTO fooditems_per_day (chat_id) VALUES ($1)"))
                                                    {
                                                        sql_command5.Parameters.AddWithValue(user.chat_id);
                                                        await sql_command5.ExecuteNonQueryAsync();
                                                    }
                                                    user.user_state = "general_data_change";
                                                    await set_user_state(user.chat_id, user.user_state);

                                                }
                                                else
                                                {
                                                    await botClient.SendTextMessageAsync(message.Chat, "Привет!", replyMarkup: Keyboard_Markups.Main_Menu_Reply_Keyboard);
                                                }
                                                return;
                                            }
                                        case "Данные о себе":
                                            {
                                                await botClient.SendTextMessageAsync(message.Chat, "Меню данных о себе", replyMarkup: Keyboard_Markups.user_Reply_Keyboard);
                                                return;
                                            }
                                        case "Еда":
                                            {
                                                string fooditems = "";
                                                await using (var sql_command = dataSource.CreateCommand($"select f.\"name\" from fooditem f inner join fooditems_per_day fpd on f.chat_id = fpd.chat_id where f.chat_id = {user.chat_id}"))
                                                await using (var reader = await sql_command.ExecuteReaderAsync())
                                                {
                                                    while (await reader.ReadAsync())
                                                    {
                                                        for (int i = 0; i < reader.FieldCount; i++)
                                                            fooditems = fooditems + reader.GetString(i) + '\n';
                                                    }
                                                }
                                                await botClient.SendTextMessageAsync(message.Chat, "Меню на сегодня\n" + fooditems, replyMarkup: Keyboard_Markups.Dishes_Reply_Keyboard);
                                                return;
                                            }
                                        case "Напоминания":
                                            {
                                                await botClient.SendTextMessageAsync(message.Chat, "Меню напоминаний", replyMarkup: Keyboard_Markups.Reminders_Reply_Keyboard);
                                                return;
                                            }
                                        case "Расчеты":
                                            {
                                                await botClient.SendTextMessageAsync(message.Chat, "Меню с расчетами", replyMarkup: Keyboard_Markups.Counts_Reply_Keyboard);
                                                return;
                                            }
                                        case "Добавить блюдо":
                                            {
                                                user.user_state = "dish_add_name";
                                                await set_user_state(user.chat_id, user.user_state);

                                                await botClient.SendTextMessageAsync(message.Chat, "Введите название блюда");
                                                return;
                                            }
                                        case "Данные о себе сейчас":
                                            {
                                                double user_weight = 0;
                                                int user_height = 0;
                                                int user_age = 0;
                                                double calories_per_day = 0;
                                                int calories_eaten = 0;
                                                int water_per_day = 0;
                                                int water_drunk = 0;
                                                await using (var sql_command1 = dataSource.CreateCommand("SELECT weight, height, age, calories_per_day, " +
                                                    "calories_eaten, water_per_day, water_drunk FROM users WHERE chat_id = " + user.chat_id))
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
                                                    $"Рекомендуемое количество выпитой жидкости {water_per_day}\n" +
                                                    $"Выпитое за сегодня количество жидкости {water_drunk}\n");
                                                return;
                                            }
                                        case "Изменить данные о себе":
                                            if (user.user_state == "")
                                            {
                                                await botClient.SendTextMessageAsync(message.Chat, "Что будем менять?", replyMarkup: Keyboard_Markups.user_Change_Reply_Keyboard);
                                                user.user_state = "general_data_change";

                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                            else return;
                                        case "Посчитать рекомендуемое количество калорий в день":
                                            {
                                                double user_weight = 0;
                                                int user_height = 0;
                                                int user_age = 0;
                                                string user_gender = null;
                                                await using (var sql_command1 = dataSource.CreateCommand("SELECT weight, height, age, gender FROM users WHERE chat_id = " + user.chat_id))
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
                                                if (user_height == 0 || user_age == 0 || user_weight == 0 || user_gender == null)
                                                {
                                                    await botClient.SendTextMessageAsync(user.chat_id, "Каких-то данных не хватает. Закончите заполнение данных о себе и возвращайтесь!");
                                                    return;
                                                }
                                                else
                                                {
                                                    double user_recommended_calories = (user_weight * 10 + user_height * 6.25 - user_age * 5 - (user_gender == "М" ? +5 : -161)) * 1.2;
                                                    using (var sql_command2 = dataSource.CreateCommand("UPDATE users set calories_per_day = " + user_recommended_calories + " where chat_id = " + user.chat_id))
                                                    {
                                                        await sql_command2.ExecuteNonQueryAsync();
                                                    };
                                                    await botClient.SendTextMessageAsync(message.Chat, "В среднем, рекомендуемое количество калорий в день для вас равно " + user_recommended_calories +
                                                        " ККал/день", replyMarkup: Keyboard_Markups.Counts_Reply_Keyboard);
                                                    return;
                                                }

                                            }
                                        case "Посчитать водный баланс":
                                            {
                                                double user_weight = 0;
                                                int user_height = 0;
                                                string user_gender = null;
                                                await using (var sql_command1 = dataSource.CreateCommand("SELECT weight, height, gender FROM users WHERE chat_id = " + user.chat_id))
                                                await using (var reader = await sql_command1.ExecuteReaderAsync())
                                                {
                                                    while (await reader.ReadAsync())
                                                    {
                                                        user_weight = reader.GetDouble(0);
                                                        user_height = reader.GetInt32(1);
                                                        user_gender = reader.GetString(2);
                                                    }
                                                };
                                                if (user_height == 0 || user_weight == 0 || user_gender == null)
                                                {
                                                    await botClient.SendTextMessageAsync(user.chat_id, "Каких-то данных не хватает. Закончите заполнение данных о себе и возвращайтесь!");
                                                    return;
                                                }
                                                else
                                                {
                                                    double user_recommended_water = (user_height - 100) * ((user_height - user_weight <= user_height - 100 ? 30 : 35) - (user_height - 100) * (user_gender == "М" ? 0.04 : 0.06));
                                                    using (var sql_command2 = dataSource.CreateCommand("UPDATE users set water_per_day = " + user_recommended_water + " where chat_id = " + user.chat_id))
                                                    {
                                                        await sql_command2.ExecuteNonQueryAsync();
                                                    };
                                                    await botClient.SendTextMessageAsync(message.Chat, "Мы рекомендуем вам пить примерно " + user_recommended_water + " миллилитров в день");
                                                    return;
                                                }
                                            }
                                        case "Количество воды за день":
                                            if (user.user_state == "general_data_change")
                                            {
                                                user.user_state = "water_per_day_change";
                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                            else return;
                                        case "Количество калорий за день":
                                            if (user.user_state == "general_data_change")
                                            {
                                                user.user_state = "calories_per_day_change";
                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                            else return;
                                        case "Рост":
                                            if (user.user_state == "general_data_change")
                                            {
                                                await botClient.SendTextMessageAsync(message.Chat, "Введите свой рост в сантиметрах");
                                                user.user_state = "height_change";
                                                await set_user_state(user.chat_id, user.user_state);


                                                return;
                                            }
                                            else return;
                                        case "Вес":
                                            if (user.user_state == "general_data_change")
                                            {
                                                await botClient.SendTextMessageAsync(message.Chat, "Введите свой вес в килограммах");
                                                user.user_state = "weight_change";
                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                            else return;
                                        case "Возраст":
                                            if (user.user_state == "general_data_change")
                                            {
                                                await botClient.SendTextMessageAsync(message.Chat, "Введите свой возраст");
                                                user.user_state = "age_change";
                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                            else return;
                                        case "Пол":
                                            if (user.user_state == "general_data_change")
                                            {
                                                await botClient.SendTextMessageAsync(message.Chat, "Выберите пол", replyMarkup: Keyboard_Markups.Gender_Change_Reply_Keyboard);
                                                user.user_state = "gender_change";
                                                await set_user_state(user.chat_id, user.user_state);

                                                return;
                                            }
                                            else return;
                                        case "Настроить график водного баланса":
                                            {
                                                user.user_state = "water_reminder_change_1";
                                                await set_user_state(user.chat_id, user.user_state);

                                                await botClient.SendTextMessageAsync(message.Chat, "Сколько раз в день вы хотите получать напоминание?");
                                                return;
                                            }
                                        case "Создать меню на сегодня":
                                            {
                                                user.user_state = "menu_create_1";
                                                await set_user_state(user.chat_id, user.user_state);

                                                bool dishes_exists = false;
                                                await using (var sql_command1 = dataSource.CreateCommand($"SELECT EXISTS (SELECT 1 FROM fooditem WHERE chat_id = {user.chat_id} and \"name\" is not null)"))
                                                await using (var reader = await sql_command1.ExecuteReaderAsync())
                                                {
                                                    while (await reader.ReadAsync())
                                                    {
                                                        dishes_exists = reader.GetBoolean(0);
                                                    }
                                                };
                                                if (dishes_exists)
                                                {
                                                    InlineKeyboardMarkup menu_inline_keyboard = await MarkupMaker(user.chat_id);
                                                    await botClient.SendTextMessageAsync(message.Chat, "Какое блюдо будем есть на завтрак?", replyMarkup: menu_inline_keyboard);
                                                }
                                                else
                                                {
                                                    await botClient.SendTextMessageAsync(message.Chat, "Чтобы сделать меню, нужно добавить хотя-бы одно блюдо");
                                                    user.user_state = "";
                                                }
                                                return;
                                            }
                                        default:
                                            {
                                                await botClient.SendTextMessageAsync(message.Chat, "Вы что-то нажали и оно сломалось, попробуйте ещё раз");
                                                return;
                                            }
                                    }
                                }
                                else
                                {
                                    user.user_state = "";
                                    await set_user_state(user.chat_id, user.user_state);

                                    double user_weight = 0;
                                    int user_height = 0;
                                    int user_age = 0;
                                    double calories_per_day = 0;
                                    int calories_eaten = 0;
                                    int water_per_day = 0;
                                    int water_drunk = 0;
                                    await using (var sql_command1 = dataSource.CreateCommand("SELECT weight, height, age, calories_per_day, " +
                                        "calories_eaten, water_per_day, water_drunk FROM users WHERE chat_id = " + user.chat_id))
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
                                        $"Рекомендуемое количество выпитой жидкости {water_per_day}\n" +
                                        $"Выпитое за сегодня количество жидкости {water_drunk}\n", replyMarkup: Keyboard_Markups.Main_Menu_Reply_Keyboard);
                                    return;
                                }
                            }
                        default:
                            {
                                return;
                            }
                    }

                }
            case UpdateType.CallbackQuery:
            {
                    var message = update.CallbackQuery;
                    user.chat_id = update.CallbackQuery.Message.Chat.Id;
                    async Task <double> calories_get (string dish, long chat_id)
                    {
                        var calories = 0.0;
                        var weight = 0.0;
                        await using (var sql_command = dataSource.CreateCommand("Select calories, weight from fooditem where" + '"' + "name" + '"' + $" like '{message.Data}' and chat_id = " + user.chat_id))
                        await using (var reader = await sql_command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                calories = reader.GetDouble(0);
                                weight = reader.GetDouble(1);
                            }
                        }
                        return calories * weight;
                    }
                    if (user.user_state == "menu_create_1")
                    {
                        InlineKeyboardMarkup menu_inline_keyboard = await MarkupMaker(user.chat_id);
                        await botClient.SendTextMessageAsync(user.chat_id, "Вы добавили " + message.Data + " калорийностью " + await calories_get(message.Data, user.chat_id) + " на завтрак.\n" +
                            "Что добавим на обед?", replyMarkup: menu_inline_keyboard);
                        using var sql_command2 = dataSource.CreateCommand($"update fooditems_per_day set breakfast = (select fooditem_id from fooditem where \"name\" like '{message.Data}') where user.chat_id = ({user.chat_id}) ");
                        await sql_command2.ExecuteNonQueryAsync();
                        user.user_state = "menu_create_2";
                        user.calories_sum += await calories_get(message.Data, user.chat_id);
                    }
                    else if (user.user_state == "menu_create_2") {
                        InlineKeyboardMarkup menu_inline_keyboard = await MarkupMaker(user.chat_id);
                        await botClient.SendTextMessageAsync(user.chat_id, "Вы добавили " + message.Data + " калорийностью " + await calories_get(message.Data, user.chat_id) + " на обед.\n" +
                            "Что добавим на ужин?", replyMarkup: menu_inline_keyboard);
                        using var sql_command2 = dataSource.CreateCommand($"update fooditems_per_day set lunch = (select fooditem_id from fooditem where \"name\" like '{message.Data}') where user.chat_id = ({user.chat_id}) ");
                        await sql_command2.ExecuteNonQueryAsync();
                        user.user_state = "menu_create_3";
                        user.calories_sum += await calories_get(message.Data, user.chat_id);
                        user.List_Of_Dishes.Clear();
                    }
                    else if (user.user_state == "menu_create_3")
                    {
                        user.calories_sum += await calories_get(message.Data, user.chat_id);
                        await botClient.SendTextMessageAsync(user.chat_id, "Вы добавили " + message.Data + " калорийностью " + await calories_get(message.Data, user.chat_id) + " на обед.\n" +
                            "Составление меню закончено, общая калорийность = " + user.calories_sum);
                        using var sql_command2 = dataSource.CreateCommand($"update fooditems_per_day set dinner = (select fooditem_id from fooditem where \"name\" like '{message.Data}') where user.chat_id = ({user.chat_id}) ");
                        await sql_command2.ExecuteNonQueryAsync();
                        user.calories_sum = 0.0;
                        user.user_state = "";
                    }
                    return;
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

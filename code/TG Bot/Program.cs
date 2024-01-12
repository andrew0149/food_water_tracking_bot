using System.Globalization;
using Npgsql;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TG_Bot;

var addDishString = "";
var eatenDishName = "";

CancellationTokenSource cts = new();

var botClient = new TelegramBotClient(Config.TgApiToken);
var pgCredentials = Config.PgCredentials;

await using var dataSource = NpgsqlDataSource.Create(
    $"Host={pgCredentials["host"]};" 
    + $"Username={pgCredentials["username"]};" 
    + $"Password={pgCredentials["password"]};" 
    + $"Database={pgCredentials["database"]};" 
    + $"Port={pgCredentials["port"]}");

await using var connection = await dataSource.OpenConnectionAsync();

ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
    HandleUpdateAsync,
    HandlePollingErrorAsync,
    receiverOptions,
    cts.Token
);

var me = await botClient.GetMeAsync();
await Schedule();

async Task<string> GetUserState(long chatId)
{
    var state = "";
    await using var sqlCommand = dataSource.CreateCommand($"select user_state from users where chat_id = {chatId}");
    await using var reader = await sqlCommand.ExecuteReaderAsync();
    while (await reader.ReadAsync()) state = reader.GetString(0);

    return state;
}

async Task SetUserState(long chatId, string state)
{
    await using var sqlCommand =
        dataSource.CreateCommand($"UPDATE users set user_state = '{state}' where chat_id = " + chatId);
    await sqlCommand.ExecuteNonQueryAsync();
}

async Task HandleUpdateAsync(ITelegramBotClient telegramBotClient, Update update, CancellationToken cancellationToken)
{
    try
    {
        if (update.Message != null && update.CallbackQuery is { Message: { } })
        {
            var user = new UserData
            {
                ChatId = update.Type == UpdateType.Message
                    ? update.Message.Chat.Id
                    : update.CallbackQuery.Message.Chat.Id
            };

            user.UserState = await GetUserState(user.ChatId);

            async Task<InlineKeyboardMarkup> DishesMarkupMaker(long chatId)
            {
                var dishesList = new List<string>();
                var command = $"select \"name\" from fooditem where chat_id = {chatId}";
                await using (var sqlCommand1 = dataSource.CreateCommand(command))
                await using (var reader = await sqlCommand1.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        for (var i = 0; i < reader.FieldCount; i++)
                            dishesList.Add(reader.GetString(i));
                }

                var buttons = dishesList.Select(
                        dish
                            => new[] { InlineKeyboardButton.WithCallbackData(dish, dish) })
                    .ToList();

                return new InlineKeyboardMarkup(buttons);
            }

            async Task<InlineKeyboardMarkup> MenuMarkupMaker(long chatId)
            {
                var menu = new List<string>();
                var commands = new[]
                {
                    "select \"name\" from fooditem f inner join fooditems_per_day fpd on "
                    + $"f.chat_id = fpd.chat_id and f.fooditem_id = fpd.breakfast where f.chat_id = {chatId} limit 1;",
                    "select \"name\" from fooditem f inner join fooditems_per_day fpd on "
                    + $"f.chat_id = fpd.chat_id and f.fooditem_id = fpd.lunch where f.chat_id = {chatId} limit 1;",
                    "select \"name\" from fooditem f inner join fooditems_per_day fpd on "
                    + $"f.chat_id = fpd.chat_id and f.fooditem_id = fpd.dinner where f.chat_id = {chatId} limit 1;"
                };

                foreach (var command in commands)
                {
                    await using var sqlCommand1 = dataSource.CreateCommand(command);
                    await using var reader = await sqlCommand1.ExecuteReaderAsync();
                    while (await reader.ReadAsync()) menu.Add(reader.GetString(0));
                }

                var buttons = menu.Select(
                        dish
                            => new[] { InlineKeyboardButton.WithCallbackData(dish, dish) })
                    .ToList();

                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("Всё меню", "whole_list") });

                return new InlineKeyboardMarkup(buttons);
            }

            switch (update.Type)
            {
                case UpdateType.Message:
                {
                    var message = update.Message;
                    switch (message.Type)
                    {
                        case MessageType.Text:
                        {
                            user.ChatId = message.Chat.Id;
                            var chatIdExists = false;
                            if (message.Text != "Вернуться")
                            {
                                switch (user.UserState)
                                {
                                    case "water_per_day_change":
                                    {
                                        await using (var sqlCommand2 = dataSource.CreateCommand(
                                                         "UPDATE users set water_per_day = " + message.Text +
                                                         " where chat_id = " + user.ChatId))
                                        {
                                            await sqlCommand2.ExecuteNonQueryAsync();
                                        }

                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Планируемый объем воды изменен, поменяем что-нибудь ещё?",
                                            replyMarkup: KeyboardMarkups.UserChangeReplyKeyboard);

                                        user.UserState = "general_data_change";
                                        await SetUserState(user.ChatId, user.UserState);
                                        return;
                                    }
                                    case "calories_per_day_change":
                                    {
                                        await using (var sqlCommand2 = dataSource.CreateCommand(
                                                         "UPDATE users set calories_per_day = " + message.Text +
                                                         " where chat_id = " + user.ChatId))
                                        {
                                            await sqlCommand2.ExecuteNonQueryAsync();
                                        }

                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Планируемый объем калорий изменен, поменяем что-нибудь ещё?",
                                            replyMarkup: KeyboardMarkups.UserChangeReplyKeyboard);

                                        user.UserState = "general_data_change";
                                        await SetUserState(user.ChatId, user.UserState);
                                        return;
                                    }
                                    case "height_change":
                                    {
                                        await using (var sqlCommand2 = dataSource.CreateCommand(
                                                         "UPDATE users set height = " +
                                                         message.Text + " where chat_id = " + user.ChatId))
                                        {
                                            await sqlCommand2.ExecuteNonQueryAsync();
                                        }

                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Рост изменен, поменяем что-нибудь ещё?",
                                            replyMarkup: KeyboardMarkups.UserChangeReplyKeyboard);

                                        user.UserState = "general_data_change";
                                        await SetUserState(user.ChatId, user.UserState);

                                        return;
                                    }

                                    case "weight_change":
                                    {
                                        await using (var sqlCommand2 = dataSource.CreateCommand(
                                                         "UPDATE users set weight = " +
                                                         message.Text + " where chat_id = " + user.ChatId))
                                        {
                                            await sqlCommand2.ExecuteNonQueryAsync();
                                        }

                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Вес изменен, поменяем что-нибудь ещё?",
                                            replyMarkup: KeyboardMarkups.UserChangeReplyKeyboard);

                                        user.UserState = "general_data_change";
                                        await SetUserState(user.ChatId, user.UserState);

                                        return;
                                    }
                                    case "age_change":
                                    {
                                        await using (var sqlCommand2 = dataSource.CreateCommand(
                                                         "UPDATE users set age = " +
                                                         message.Text + " where chat_id = " + user.ChatId))
                                        {
                                            await sqlCommand2.ExecuteNonQueryAsync();
                                        }

                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Возраст изменен, поменяем что-нибудь ещё?",
                                            replyMarkup: KeyboardMarkups.UserChangeReplyKeyboard);

                                        user.UserState = "general_data_change";
                                        await SetUserState(user.ChatId, user.UserState);

                                        return;
                                    }
                                    case "gender_change":

                                    {
                                        await using (var sqlCommand2 = dataSource.CreateCommand(
                                                         "UPDATE users set gender = '" +
                                                         message.Text + "' where chat_id = " + user.ChatId))
                                        {
                                            await sqlCommand2.ExecuteNonQueryAsync();
                                        }

                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Пол изменен, поменяем что-нибудь ещё?",
                                            replyMarkup: KeyboardMarkups.UserChangeReplyKeyboard);

                                        user.UserState = "general_data_change";
                                        await SetUserState(user.ChatId, user.UserState);

                                        return;
                                    }
                                    case "dish_add_weight":

                                    {
                                        addDishString += $"{message.Text}, {user.ChatId})";
                                        await using var sqlCommand2 = dataSource.CreateCommand(addDishString);
                                        await sqlCommand2.ExecuteNonQueryAsync();

                                        await telegramBotClient.SendTextMessageAsync(message.Chat, "Блюдо добавлено",
                                            replyMarkup: KeyboardMarkups.DishesReplyKeyboard);

                                        user.UserState = "";
                                        await SetUserState(user.ChatId, user.UserState);

                                        addDishString = "";
                                        return;
                                    }
                                    case "dish_add_calories":
                                    {
                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Введите средний вес порции в граммах");
                                        addDishString += $"{message.Text},";
                                        user.UserState = "dish_add_weight";
                                        await SetUserState(user.ChatId, user.UserState);
                                        return;
                                    }

                                    case "dish_add_type":
                                    {
                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Введите калорийность блюда на 100 грамм сухого продукта");
                                        user.UserState = "dish_add_calories";
                                        await SetUserState(user.ChatId, user.UserState);

                                        return;
                                    }

                                    case "dish_add_name":
                                    {
                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Введите тип блюда, например суп");
                                        addDishString =
                                            $"INSERT INTO fooditem (name, calories, weight, chat_id) VALUES ('{message.Text}',";

                                        user.UserState = "dish_add_type";
                                        await SetUserState(user.ChatId, user.UserState);

                                        return;
                                    }
                                    case "water_reminder_change_1":
                                    {
                                        await using (var sqlCommand = dataSource.CreateCommand(
                                                         "update reminders set water_remind = current_date + interval '$1 day' + " +
                                                         $"time '{message.Text}:00' where chat_id = {user.ChatId};"))
                                        {
                                            await sqlCommand.ExecuteNonQueryAsync();
                                        }

                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "С каким интервалом вы хотите получать напоминания? (Введите время в формате HH:MM)");

                                        user.UserState = "water_reminder_change_2";
                                        await SetUserState(user.ChatId, user.UserState);

                                        return;
                                    }
                                    case "water_reminder_change_2":
                                    {
                                        await using (var sqlCommand = dataSource.CreateCommand(
                                                         $"update reminders set water_remind_interval = '{message.Text}:00' where chat_id = {user.ChatId}"))
                                        {
                                            await sqlCommand.ExecuteNonQueryAsync();
                                        }

                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Напоминание установлено на завтра.",
                                            replyMarkup: KeyboardMarkups.MainMenuReplyKeyboard);

                                        user.UserState = "";
                                        await SetUserState(user.ChatId, user.UserState);
                                        return;
                                    }

                                    case "food_reminder_change_1":
                                    {
                                        await using (var sqlCommand = dataSource.CreateCommand(
                                                         "update reminders set fooditem_remind = current_date + interval '$1 day' + " +
                                                         $"time '{message.Text}:00' where chat_id = {user.ChatId};"))
                                        {
                                            await sqlCommand.ExecuteNonQueryAsync();
                                        }

                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "С каким интервалом вы хотите получать напоминания? (Введите время в формате HH:MM)");

                                        user.UserState = "food_reminder_change_2";
                                        await SetUserState(user.ChatId, user.UserState);

                                        return;
                                    }

                                    case "food_reminder_change_2":
                                    {
                                        await using (var sqlCommand = dataSource.CreateCommand(
                                                         $"update reminders set fooditem_remind_interval = '{message.Text}' where chat_id = {user.ChatId}"))
                                        {
                                            await sqlCommand.ExecuteNonQueryAsync();
                                        }

                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Напоминание установлено на завтра.",
                                            replyMarkup: KeyboardMarkups.MainMenuReplyKeyboard);

                                        user.UserState = "";
                                        await SetUserState(user.ChatId, user.UserState);

                                        return;
                                    }

                                    case "i_have_drunk":
                                    {
                                        if (message.Text != null)
                                        {
                                            var waterDrunk = int.Parse(message.Text);
                                            var prevWaterDrunk = 0;
                                            await using (var sqlCommand1 =
                                                         dataSource.CreateCommand(
                                                             $"select water_drunk from users where chat_id = {user.ChatId}"))

                                            await using (var reader = await sqlCommand1.ExecuteReaderAsync())
                                            {
                                                while (await reader.ReadAsync()) prevWaterDrunk = reader.GetInt32(0);
                                            }

                                            await using (var sqlCommand2 = dataSource.CreateCommand(
                                                             "update users set water_drunk = " +
                                                             $"{prevWaterDrunk + waterDrunk} where chat_id = {user.ChatId}"))
                                            {
                                                await sqlCommand2.ExecuteNonQueryAsync();
                                            }

                                            await telegramBotClient.SendTextMessageAsync(message.Chat,
                                                $"Отлично! За день вы уже выпили {prevWaterDrunk + waterDrunk} миллилитров",
                                                replyMarkup: KeyboardMarkups.MainMenuReplyKeyboard);
                                        }

                                        user.UserState = "";
                                        await SetUserState(user.ChatId, user.UserState);

                                        return;
                                    }

                                    case "i_have_eaten":
                                    {
                                        Console.WriteLine(eatenDishName);
                                        if (message.Text != null)
                                        {
                                            var eatenDishWeight = double.Parse(message.Text);
                                            double calories = 0;
                                            double caloriesEaten = 0;
                                            double dishStandardWeight = 0;

                                            await using (var sqlCommand1 = dataSource.CreateCommand(
                                                             $"select calories, weight from fooditem where \"name\" = '{eatenDishName}' and chat_id = {user.ChatId}"))

                                            await using (var reader = await sqlCommand1.ExecuteReaderAsync())
                                            {
                                                while (await reader.ReadAsync())
                                                {
                                                    calories = reader.GetDouble(0);
                                                    dishStandardWeight = reader.GetDouble(1);
                                                }
                                            }

                                            await using (var sqlCommand1 =
                                                         dataSource.CreateCommand(
                                                             $"select calories_eaten from users where chat_id = {user.ChatId}"))

                                            await using (var reader = await sqlCommand1.ExecuteReaderAsync())
                                            {
                                                while (await reader.ReadAsync()) caloriesEaten = reader.GetDouble(0);
                                            }

                                            var sumCalories = caloriesEaten +
                                                              calories * eatenDishWeight / dishStandardWeight;

                                            await telegramBotClient.SendTextMessageAsync(user.ChatId,
                                                $"Отлично! За сегодня вы съели {sumCalories} ККал",
                                                replyMarkup: KeyboardMarkups.MainMenuReplyKeyboard);

                                            await using (var sqlCommand2 = dataSource.CreateCommand(
                                                             "update users set calories_eaten = " +
                                                             $"{sumCalories} where chat_id = {user.ChatId}"))
                                            {
                                                await sqlCommand2.ExecuteNonQueryAsync();
                                            }
                                        }

                                        user.UserState = "";
                                        await SetUserState(user.ChatId, user.UserState);

                                        return;
                                    }
                                }

                                switch (message.Text)
                                {
                                    case "/start":
                                    {
                                        await using (var sqlCommand1 = dataSource.CreateCommand(
                                                         "SELECT EXISTS (SELECT 1 FROM users WHERE chat_id = " +
                                                         user.ChatId + ")"))

                                        await using (var reader = await sqlCommand1.ExecuteReaderAsync())
                                        {
                                            while (await reader.ReadAsync())
                                                chatIdExists = reader.GetBoolean(0);
                                        }

                                        if (!chatIdExists)
                                        {
                                            await telegramBotClient.SendTextMessageAsync(message.Chat,
                                                "Привет, " + message.From?.Username + ". " +
                                                "Я - бот который позволяет отслеживать потребляемые калории и выпитую воду\n" +
                                                "Я тебя ещё не знаю, но очень хочу познакомиться\n" +
                                                "Пожалуйста, добавь свои данные используя встроенную клавиатуру\n" +
                                                "P.S. Если что-то сломалось, используй кнопку Вернуться",
                                                replyMarkup: KeyboardMarkups.UserChangeReplyKeyboard);

                                            await using (var sqlCommand2 =
                                                         dataSource.CreateCommand(
                                                             "INSERT INTO users (chat_id) VALUES ($1)"))
                                            {
                                                sqlCommand2.Parameters.AddWithValue(user.ChatId);
                                                await sqlCommand2.ExecuteNonQueryAsync();
                                            }

                                            await using (var sqlCommand3 =
                                                         dataSource.CreateCommand(
                                                             "INSERT INTO reminders (chat_id) VALUES ($1)"))
                                            {
                                                sqlCommand3.Parameters.AddWithValue(user.ChatId);
                                                await sqlCommand3.ExecuteNonQueryAsync();
                                            }

                                            await using (var sqlCommand4 =
                                                         dataSource.CreateCommand(
                                                             "INSERT INTO fooditem (chat_id) VALUES ($1)"))
                                            {
                                                sqlCommand4.Parameters.AddWithValue(user.ChatId);
                                                await sqlCommand4.ExecuteNonQueryAsync();
                                            }

                                            await using (var sqlCommand5 =
                                                         dataSource.CreateCommand(
                                                             "INSERT INTO fooditems_per_day (chat_id) VALUES ($1)"))
                                            {
                                                sqlCommand5.Parameters.AddWithValue(user.ChatId);
                                                await sqlCommand5.ExecuteNonQueryAsync();
                                            }

                                            user.UserState = "general_data_change";
                                            await SetUserState(user.ChatId, user.UserState);
                                        }

                                        else
                                        {
                                            await telegramBotClient.SendTextMessageAsync(message.Chat, "Привет!",
                                                replyMarkup: KeyboardMarkups.MainMenuReplyKeyboard);
                                        }

                                        return;
                                    }

                                    case "Данные о себе":
                                    {
                                        await telegramBotClient.SendTextMessageAsync(message.Chat, "Меню данных о себе",
                                            replyMarkup: KeyboardMarkups.UserReplyKeyboard);
                                        return;
                                    }

                                    case "Еда":
                                    {
                                        var fooditems = "";
                                        var dishesExists = false;
                                        await using (var sqlCommand1 = dataSource.CreateCommand(
                                                         $"SELECT EXISTS (SELECT 1 FROM fooditem WHERE chat_id = {user.ChatId} and \"name\" is not null)"))
                                        await using (var reader = await sqlCommand1.ExecuteReaderAsync())
                                        {
                                            while (await reader.ReadAsync()) dishesExists = reader.GetBoolean(0);
                                        }

                                        if (dishesExists)
                                        {
                                            await using (var sqlCommand =
                                                         dataSource.CreateCommand(
                                                             $"select f.\"name\" from fooditem f inner join fooditems_per_day fpd on f.chat_id = fpd.chat_id where f.chat_id = {user.ChatId}"))
                                            await using (var reader = await sqlCommand.ExecuteReaderAsync())
                                            {
                                                while (await reader.ReadAsync())
                                                    for (var i = 0; i < reader.FieldCount; i++)
                                                        fooditems = fooditems + reader.GetString(i) + '\n';
                                            }

                                            await telegramBotClient.SendTextMessageAsync(message.Chat,
                                                "Меню на сегодня\n" + fooditems,
                                                replyMarkup: KeyboardMarkups.DishesReplyKeyboard);
                                        }

                                        else
                                        {
                                            await telegramBotClient.SendTextMessageAsync(message.Chat,
                                                "У вас нет ни одного блюда, добавьте хотя-бы одно блюдо" +
                                                " и создайте рацион чтобы увидеть этот список.",
                                                replyMarkup: KeyboardMarkups.DishesReplyKeyboard);
                                            user.UserState = "";
                                            await SetUserState(user.ChatId, user.UserState);
                                        }

                                        return;
                                    }

                                    case "Напоминания":
                                    {
                                        await telegramBotClient.SendTextMessageAsync(message.Chat, "Меню напоминаний",
                                            replyMarkup: KeyboardMarkups.RemindersReplyKeyboard);
                                        return;
                                    }

                                    case "Расчеты":
                                    {
                                        await telegramBotClient.SendTextMessageAsync(message.Chat, "Меню с расчетами",
                                            replyMarkup: KeyboardMarkups.CountsReplyKeyboard);
                                        return;
                                    }

                                    case "Добавить блюдо":
                                    {
                                        user.UserState = "dish_add_name";
                                        await SetUserState(user.ChatId, user.UserState);

                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Введите название блюда");
                                        return;
                                    }

                                    case "Данные о себе сейчас":
                                    {
                                        double userWeight = 0;
                                        var userHeight = 0;
                                        var userAge = 0;
                                        double caloriesPerDay = 0;
                                        double caloriesEaten = 0;
                                        var waterPerDay = 0;
                                        var waterDrunk = 0;
                                        await using (var sqlCommand1 = dataSource.CreateCommand(
                                                         "SELECT weight, height, age, calories_per_day, " +
                                                         "calories_eaten, water_per_day, water_drunk FROM users WHERE chat_id = " +
                                                         user.ChatId))

                                        await using (var reader = await sqlCommand1.ExecuteReaderAsync())
                                        {
                                            while (await reader.ReadAsync())
                                            {
                                                userWeight = reader.GetDouble(0);
                                                userHeight = reader.GetInt32(1);
                                                userAge = reader.GetInt32(2);
                                                caloriesPerDay = reader.GetDouble(3);
                                                caloriesEaten = reader.GetDouble(4);
                                                waterPerDay = reader.GetInt32(5);
                                                waterDrunk = reader.GetInt32(6);
                                            }
                                        }

                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Данные о пользователе\n" +
                                            $"Ваш вес {userWeight}\n" +
                                            $"Ваш рост {userHeight}\n" +
                                            $"Ваш возраст {userAge}\n" +
                                            $"Рекомендуемое дневное количество калорий {caloriesPerDay}\n" +
                                            $"Потребленное количество калорий за сегодня {caloriesEaten}\n" +
                                            $"Рекомендуемое количество выпитой жидкости {waterPerDay}\n" +
                                            $"Выпитое за сегодня количество жидкости {waterDrunk}\n");
                                        return;
                                    }

                                    case "Изменить данные о себе":
                                    {
                                        if (user.UserState != "") 
                                            return;
                                        
                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Что будем менять?",
                                            replyMarkup: KeyboardMarkups.UserChangeReplyKeyboard);
                                        user.UserState = "general_data_change";

                                        await SetUserState(user.ChatId, user.UserState);

                                        return;
                                    }
                                    
                                    case "Посчитать рекомендуемое количество калорий в день":
                                    {
                                        double userWeight = 0;
                                        var userHeight = 0;
                                        var userAge = 0;
                                        string? userGender = null;
                                        await using (var sqlCommand1 = dataSource.CreateCommand(
                                                         "SELECT weight, height, age, gender FROM users WHERE chat_id = " +
                                                         user.ChatId))
                                        await using (var reader = await sqlCommand1.ExecuteReaderAsync())
                                        {
                                            while (await reader.ReadAsync())
                                            {
                                                userWeight = reader.GetDouble(0);
                                                userHeight = reader.GetInt32(1);
                                                userAge = reader.GetInt32(2);
                                                userGender = reader.GetString(3);
                                            }
                                        }

                                        if (userHeight == 0 || userAge == 0 || userWeight == 0 ||
                                            userGender == null)
                                        {
                                            await telegramBotClient.SendTextMessageAsync(user.ChatId,
                                                "Каких-то данных не хватает. Закончите заполнение данных о себе и возвращайтесь!");
                                            return;
                                        }

                                        var userRecommendedCalories = (userWeight * 10 + userHeight * 6.25 -
                                                                         userAge * 5 -
                                                                         (userGender == "М" ? +5 : -161)) * 1.2;

                                        await using (var sqlCommand2 = dataSource.CreateCommand(
                                                         "UPDATE users set calories_per_day = " + userRecommendedCalories +
                                                         " where chat_id = " + user.ChatId))
                                        {
                                            await sqlCommand2.ExecuteNonQueryAsync();
                                        }
                                        
                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "В среднем, рекомендуемое количество калорий в день для вас равно " +
                                            userRecommendedCalories +
                                            " ККал/день", replyMarkup: KeyboardMarkups.CountsReplyKeyboard);
                                        
                                        return;
                                    }
                                    
                                    case "Посчитать водный баланс":
                                    {
                                        double userWeight = 0;
                                        var userHeight = 0;
                                        string? userGender = null;
                                        
                                        await using (var sqlCommand1 = dataSource.CreateCommand(
                                                         "SELECT weight, height, gender FROM users WHERE chat_id = " +
                                                         user.ChatId))
                                        
                                        await using (var reader = await sqlCommand1.ExecuteReaderAsync())
                                        {
                                            while (await reader.ReadAsync())
                                            {
                                                userWeight = reader.GetDouble(0);
                                                userHeight = reader.GetInt32(1);
                                                userGender = reader.GetString(2);
                                            }
                                        }

                                        if (userHeight == 0 || userWeight == 0 || userGender == null)
                                        {
                                            await telegramBotClient.SendTextMessageAsync(user.ChatId,
                                                "Каких-то данных не хватает. Закончите заполнение данных о себе и возвращайтесь!");
                                            return;
                                        }

                                        var userRecommendedWater = (userHeight - 100) *
                                                                     ((userHeight - userWeight <= userHeight - 100
                                                                             ? 30
                                                                             : 35) - (userHeight - 100) *
                                                                         (userGender == "М" ? 0.04 : 0.06));
                                        await using (var sqlCommand2 = dataSource.CreateCommand(
                                                   "UPDATE users set water_per_day = " + userRecommendedWater +
                                                   " where chat_id = " + user.ChatId))
                                        {
                                            await sqlCommand2.ExecuteNonQueryAsync();
                                        }

                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Мы рекомендуем вам пить примерно " + userRecommendedWater +
                                            " миллилитров в день");
                                        return;
                                    }
                                    
                                    case "Количество воды за день":
                                    {
                                        if (user.UserState != "general_data_change") 
                                            return;
                                        
                                        await telegramBotClient.SendTextMessageAsync(user.ChatId,
                                            "Сколько вы бы хотели пить в день (Миллилитров)?");
                                        user.UserState = "water_per_day_change";
                                        await SetUserState(user.ChatId, user.UserState);
                                        return;
                                    }
                                    
                                    case "Количество калорий за день":
                                    {
                                        if (user.UserState != "general_data_change") 
                                            return;
                                        
                                        await telegramBotClient.SendTextMessageAsync(user.ChatId,
                                            "Сколько вы бы хотели есть в день (ККал)?");
                                        user.UserState = "calories_per_day_change";
                                        await SetUserState(user.ChatId, user.UserState);
                                        return;
                                    }
                                    
                                    case "Рост":
                                    {
                                        if (user.UserState != "general_data_change") 
                                            return;
                                        
                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Введите свой рост в сантиметрах");
                                        user.UserState = "height_change";
                                        await SetUserState(user.ChatId, user.UserState);
                                        
                                        return;
                                    }
                                    
                                    case "Вес":
                                    {
                                        if (user.UserState != "general_data_change") 
                                            return;
                                        
                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Введите свой вес в килограммах");
                                        user.UserState = "weight_change";
                                        await SetUserState(user.ChatId, user.UserState);

                                        return;
                                    }
                                    
                                    case "Возраст":
                                    {
                                        if (user.UserState != "general_data_change") 
                                            return;
                                        
                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Введите свой возраст");
                                        user.UserState = "age_change";
                                        await SetUserState(user.ChatId, user.UserState);

                                        return;
                                    }
                                    
                                    case "Пол":
                                    {
                                        if (user.UserState != "general_data_change") 
                                            return;
                                            
                                        await telegramBotClient.SendTextMessageAsync(message.Chat, "Выберите пол",
                                            replyMarkup: KeyboardMarkups.GenderChangeReplyKeyboard);
                                        user.UserState = "gender_change";
                                        await SetUserState(user.ChatId, user.UserState);

                                        return;
                                    }
                                    
                                    case "Настроить график питья":
                                    {
                                        user.UserState = "water_reminder_change_1";
                                        await SetUserState(user.ChatId, user.UserState);

                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Во сколько вы хотите получить первое напоминание? (Введите время в формате HH:MM)");
                                        return;
                                    }
                                    
                                    case "Настроить график питания":
                                    {
                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Во сколько вы хотите получить первое напоминание? (Введите время в формате HH:MM)");
                                        user.UserState = "food_reminder_change_1";
                                        await SetUserState(user.ChatId, user.UserState);
                                        
                                        return;
                                    }
                                    
                                    case "Создать меню на завтра":
                                    {
                                        user.UserState = "menu_create_1";
                                        await SetUserState(user.ChatId, user.UserState);

                                        var dishesExists = false;
                                        await using (var sqlCommand1 = dataSource.CreateCommand(
                                                         $"SELECT EXISTS (SELECT 1 FROM fooditem WHERE chat_id = {user.ChatId} and \"name\" is not null)"))
                                        
                                        await using (var reader = await sqlCommand1.ExecuteReaderAsync())
                                        {
                                            while (await reader.ReadAsync()) dishesExists = reader.GetBoolean(0);
                                        }
                                        
                                        if (dishesExists)
                                        {
                                            var dishesMarkupMaker =
                                                await DishesMarkupMaker(user.ChatId);
                                            await telegramBotClient.SendTextMessageAsync(message.Chat,
                                                "Какое блюдо будем есть на завтрак?",
                                                replyMarkup: dishesMarkupMaker);
                                        }
                                        else
                                        {
                                            await telegramBotClient.SendTextMessageAsync(message.Chat,
                                                "Чтобы сделать меню, нужно добавить хотя-бы одно блюдо");
                                            user.UserState = "";
                                            await SetUserState(user.ChatId, user.UserState);
                                        }

                                        return;
                                    }
                                    
                                    case "Я поел":
                                    {
                                        var menuMarkupMaker = await MenuMarkupMaker(user.ChatId);
                                        await telegramBotClient.SendTextMessageAsync(user.ChatId,
                                            "Хорошо, что вы ели?",
                                            replyMarkup: menuMarkupMaker);
                                        user.UserState = "i_have_eaten";
                                        await SetUserState(user.ChatId, user.UserState);
                                        
                                        return;
                                    }
                                    
                                    case "Я попил":
                                    {
                                        await telegramBotClient.SendTextMessageAsync(user.ChatId,
                                            "Хорошо, введите количество выпитой воды в миллилитрах");
                                        user.UserState = "i_have_drunk";
                                        await SetUserState(user.ChatId, user.UserState);
                                        
                                        return;
                                    }
                                    
                                    default:
                                    {
                                        await telegramBotClient.SendTextMessageAsync(message.Chat,
                                            "Вы что-то нажали и оно сломалось, попробуйте ещё раз");
                                        return;
                                    }
                                }
                            }

                            user.UserState = "";
                            await SetUserState(user.ChatId, user.UserState);
                            await telegramBotClient.SendTextMessageAsync(message.Chat, "Вернул в начало",
                                replyMarkup: KeyboardMarkups.MainMenuReplyKeyboard);
                            
                            return;
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

                    async Task<double> CaloriesGet(long chatId, string? name)
                    {
                        var calories = 0.0;
                        var weight = 0.0;
                        await using (var sqlCommand = dataSource.CreateCommand(
                                         $"Select calories, weight from fooditem where \"name\" like '{name}' and chat_id = " +
                                         chatId))
                        await using (var reader = await sqlCommand.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                calories = reader.GetDouble(0);
                                weight = reader.GetDouble(1);
                            }
                        }

                        return calories * weight;
                    }

                    switch (user.UserState)
                    {
                        case "menu_create_1":
                        {
                            var dishesMarkupMaker = await DishesMarkupMaker(user.ChatId);
                            
                            await telegramBotClient.SendTextMessageAsync(user.ChatId,
                                "Вы добавили " + message.Data + " калорийностью " +
                                await CaloriesGet(user.ChatId, message.Data) + " на завтрак.\n" +
                                "Что добавим на обед?", replyMarkup: dishesMarkupMaker);
                            
                            await using var sqlCommand2 = dataSource.CreateCommand(
                                $"update fooditems_per_day set breakfast = (select fooditem_id from fooditem where \"name\" like '{message.Data}' and chat_id = {user.ChatId}) where chat_id = ({user.ChatId}) ");
                            
                            await sqlCommand2.ExecuteNonQueryAsync();
                            
                            user.UserState = "menu_create_2";
                            await SetUserState(user.ChatId, user.UserState);
                            
                            user.CaloriesSum += await CaloriesGet(user.ChatId, message.Data);
                            
                            return;
                        }
                        
                        case "menu_create_2":
                        {
                            var menuInlineKeyboard = await DishesMarkupMaker(user.ChatId);
                            
                            await telegramBotClient.SendTextMessageAsync(user.ChatId,
                                "Вы добавили " + message.Data + " калорийностью " +
                                await CaloriesGet(user.ChatId, message.Data) + " на обед.\n" +
                                "Что добавим на ужин?", replyMarkup: menuInlineKeyboard);
                            
                            await using var sqlCommand2 = dataSource.CreateCommand(
                                $"update fooditems_per_day set lunch = (select fooditem_id from fooditem where \"name\" like '{message.Data}' and chat_id = {user.ChatId}) where chat_id = ({user.ChatId})");
                            
                            await sqlCommand2.ExecuteNonQueryAsync();
                            
                            user.UserState = "menu_create_3";
                            await SetUserState(user.ChatId, user.UserState);
                            
                            user.CaloriesSum += await CaloriesGet(user.ChatId, message.Data);
                            
                            return;
                        }
                        
                        case "menu_create_3":
                        {
                            user.CaloriesSum += await CaloriesGet(user.ChatId, message.Data);
                            
                            await telegramBotClient.SendTextMessageAsync(user.ChatId,
                                "Вы добавили " + message.Data + " калорийностью " +
                                await CaloriesGet(user.ChatId, message.Data) + " на ужин.\n" +
                                "Составление меню закончено, общая калорийность = " + user.CaloriesSum);
                            
                            await using var sqlCommand2 = dataSource.CreateCommand(
                                $"update fooditems_per_day set dinner = (select fooditem_id from fooditem where \"name\" like '{message.Data}') where chat_id = ({user.ChatId}) and chat_id = {user.ChatId}");
                            
                            await sqlCommand2.ExecuteNonQueryAsync();
                            
                            user.CaloriesSum = 0.0;
                            user.UserState = "";
                            
                            await SetUserState(user.ChatId, user.UserState);
                            
                            return;
                        }
                        
                        case "i_have_eaten":
                        {
                            if (message.Data != "whole_list")
                            {
                                eatenDishName = message.Data;
                                await telegramBotClient.SendTextMessageAsync(user.ChatId,
                                    "Введите вес порции в граммах");
                            }
                            else
                            {
                                var wholeList = await DishesMarkupMaker(user.ChatId);
                                await telegramBotClient.SendTextMessageAsync(user.ChatId, "Всё меню",
                                    replyMarkup: wholeList);
                            }

                            return;
                        }
                    }

                    return;
                }
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}

async Task Schedule()
{
    Console.WriteLine($"Start listening for @{me.Username}");
    Console.ReadLine();

    // Send cancellation request to stop bot
    cts.Cancel();
    
    try
    {
        var stop = false;
        await Task.Delay(20000);
        while (!stop)
        {
            var chatId = new List<long>();
            var fooditemRemind = new List<DateTime>();
            var waterRemind = new List<DateTime>();
            var fooditemInterval = new List<TimeSpan>();
            var waterInterval = new List<TimeSpan>();
            await using (var sqlCommand = dataSource.CreateCommand("select * from reminders"))
            await using (var reader = await sqlCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    chatId.Add(reader.GetInt64(0));
                    fooditemRemind.Add(reader.GetDateTime(1));
                    waterRemind.Add(reader.GetDateTime(2));
                    fooditemInterval.Add(reader.GetTimeSpan(3));
                    waterInterval.Add(reader.GetTimeSpan(4));
                }
            }

            for (var i = 0; i < chatId.Count; i++)
            {
                if (fooditemRemind[i].ToString(CultureInfo.InvariantCulture) != "1/1/2000 12:00:00 AM" 
                    && fooditemInterval[i].ToString() != "00:00:00")
                    
                    if (fooditemRemind[i] <= DateTime.Now)
                    {
                        await botClient.SendTextMessageAsync(chatId[i], "Пора есть");
                        var newFooditemReminder = fooditemRemind[i] + fooditemInterval[i];
                        await using (var sqlCommand = dataSource.CreateCommand(
                                         $"update reminders set fooditem_remind = '{newFooditemReminder}' where chat_id = {chatId[i]}"))
                        {
                            await sqlCommand.ExecuteNonQueryAsync();
                        }
                    }

                if (waterRemind[i].ToString(CultureInfo.InvariantCulture) == "1/1/2000 00:00:00 AM" 
                    || waterInterval[i].ToString() == "00:00:00") 
                    
                    continue;
                
                if (waterRemind[i] > DateTime.Now) 
                    continue;
                
                await botClient.SendTextMessageAsync(chatId[i], "Пора пить");
                
                var newWaterReminder = waterRemind[i] + waterInterval[i];
                await using (var sqlCommand = dataSource.CreateCommand(
                                 $"update reminders set fooditem_remind = '{newWaterReminder}' where chat_id = {chatId[i]}"))
                {
                    await sqlCommand.ExecuteNonQueryAsync();
                }
            }
        }
    }
    
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient telegramBotClient, Exception exception, CancellationToken cancellationToken)
{
    var errorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(errorMessage);
    return Task.CompletedTask;
}
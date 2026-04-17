using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Common2;
using Newtonsoft.Json;
using System.Threading;
using System.IO;
namespace Snake2
{
    internal class Program
    {

        public static List<Leaders> Leaders = new List<Leaders>();
        public static List<ViewModelUserSettings> remoteIPAddress = new List<ViewModelUserSettings>();
        public static List<ViewModelGames> viewModelGames = new List<ViewModelGames>();
        private static int localPort = 5001;
        public static int MaxSpeed = 15;
        private static readonly object _lock = new object(); // Для синхронизации

        static void Main(string[] args)
        {
            try
            {
                LoadLeaders();
                Thread tRec = new Thread(new ThreadStart(Receiver));
                tRec.Start();
                Thread tTime = new Thread(Timer);
                tTime.Start();
                Console.ReadLine(); // Чтобы консоль не закрывалась
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Возникло исключение" + ex.ToString() + "\n" + ex.Message);
            }
        }


        private static void Send()
        {
            lock (_lock)
            {
                foreach (ViewModelUserSettings User in remoteIPAddress)
                {
                    UdpClient sender = new UdpClient();
                    IPEndPoint endPoint = new IPEndPoint(
                        IPAddress.Parse(User.IPAddress),
                        int.Parse(User.Port));
                    try
                    {
                        var gameData = viewModelGames.Find(x => x.IdSnake == User.IdSnake);
                        if (gameData != null)
                        {
                            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(gameData));
                            sender.Send(bytes, bytes.Length, endPoint);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Отправил данные пользователю: {User.IPAddress} : {User.Port}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Возникло исключение" + ex.ToString() + "\n" + ex.Message);
                    }
                    finally
                    {
                        sender.Close();
                    }
                }
            }
        }


        public static void Receiver()
        {
            UdpClient receivingUdpClient = new UdpClient(localPort);
            IPEndPoint RemoteIpEndPoint = null;
            try
            {
                Console.WriteLine("Команды сервера:");
                while (true)
                {
                    byte[] receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                    string returnData = Encoding.UTF8.GetString(receiveBytes);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Получил команду: " + returnData);

                    // Проверяем, является ли команда /start
                    if (returnData.StartsWith("/start"))
                    {
                        // Убираем "/start" из начала
                        string jsonPart = returnData.Substring(6); // удаляем "/start"

                        // Если после /start идет | - пропускаем его тоже
                        if (jsonPart.StartsWith("|"))
                        {
                            jsonPart = jsonPart.Substring(1);
                        }

                        ViewModelUserSettings viewModelUserSettings = JsonConvert.DeserializeObject<ViewModelUserSettings>(jsonPart);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Подключился пользователь: {viewModelUserSettings.IPAddress}:{viewModelUserSettings.Port}");

                        lock (_lock)
                        {
                            remoteIPAddress.Add(viewModelUserSettings);
                            viewModelUserSettings.IdSnake = AddSnake();
                        }
                    }
                    else if (returnData.Contains('|'))
                    {
                        string[] dataMessage = returnData.Split('|');
                        if (dataMessage.Length >= 2)
                        {
                            ViewModelUserSettings viewModelUserSettings = JsonConvert.DeserializeObject<ViewModelUserSettings>(dataMessage[1]);

                            lock (_lock)
                            {
                                int IdPlayer = remoteIPAddress.FindIndex(x => x.IPAddress == viewModelUserSettings.IPAddress &&
                                x.Port == viewModelUserSettings.Port);

                                if (IdPlayer != -1 && IdPlayer < viewModelGames.Count && viewModelGames[IdPlayer]?.SnakesPlayers != null)
                                {
                                    var snake = viewModelGames[IdPlayer].SnakesPlayers;

                                    if (dataMessage[0] == "Up" && snake.direction != Snakes.Direction.Down)
                                        snake.direction = Snakes.Direction.Up;
                                    else if (dataMessage[0] == "Down" && snake.direction != Snakes.Direction.Up)
                                        snake.direction = Snakes.Direction.Down;
                                    else if (dataMessage[0] == "Left" && snake.direction != Snakes.Direction.Right)
                                        snake.direction = Snakes.Direction.Left;
                                    else if (dataMessage[0] == "Right" && snake.direction != Snakes.Direction.Left)
                                        snake.direction = Snakes.Direction.Right;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n" + ex.Message);
            }
        }

        public static int AddSnake()
        {
            ViewModelGames viewModelGamesPlayer = new ViewModelGames();
            viewModelGamesPlayer.SnakesPlayers = new Snakes()
            {
                Points = new List<Snakes.Point>()
                {
                    new Snakes.Point() { X = 300, Y = 200 },
                    new Snakes.Point() { X = 290, Y = 200 },
                    new Snakes.Point() { X = 280, Y = 200 }
                },
                direction = Snakes.Direction.Start
            };
            viewModelGamesPlayer.Points = new Snakes.Point(new Random().Next(20, 760), new Random().Next(20, 410));
            viewModelGamesPlayer.IdSnake = viewModelGames.Count;
            viewModelGames.Add(viewModelGamesPlayer);
            return viewModelGamesPlayer.IdSnake;
        }



        public static void Timer()
        {
            while (true)
            {
                Thread.Sleep(100);

                lock (_lock)
                {
                    // Обработка GameOver
                    List<ViewModelGames> RemoteSnakes = viewModelGames.FindAll(x => x.SnakesPlayers.GameOver);
                    if (RemoteSnakes.Count > 0)
                    {
                        foreach (ViewModelGames DeadSnake in RemoteSnakes)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            var user = remoteIPAddress.Find(x => x.IdSnake == DeadSnake.IdSnake);
                            if (user != null)
                            {
                                Console.WriteLine($"Отключил пользователя: {user.IPAddress}:{user.Port}");
                                // Сохраняем результат в лидеры
                                Leaders.Add(new Leaders()
                                {
                                    Name = user.Name,
                                    Points = DeadSnake.SnakesPlayers.Points.Count - 3
                                });
                                Leaders = Leaders.OrderByDescending(x => x.Points).ThenBy(x => x.Name).ToList();
                                SaveLeaders();
                            }
                        }
                        // Удаляем пользователей
                        remoteIPAddress.RemoveAll(x => viewModelGames.Any(g => g.IdSnake == x.IdSnake && g.SnakesPlayers.GameOver));
                        viewModelGames.RemoveAll(x => x.SnakesPlayers.GameOver);
                    }

                    // Обновление позиций змей
                    foreach (ViewModelUserSettings User in remoteIPAddress)
                    {
                        var gameData = viewModelGames.Find(x => x.IdSnake == User.IdSnake);
                        if (gameData == null) continue;

                        Snakes Snake = gameData.SnakesPlayers;

                        // Двигаем змею
                        for (int i = Snake.Points.Count - 1; i >= 0; i--)
                        {
                            if (i != 0)
                            {
                                Snake.Points[i] = new Snakes.Point(Snake.Points[i - 1].X, Snake.Points[i - 1].Y);
                            }
                            else
                            {
                                int Speed = 10 + (int)Math.Round(Snake.Points.Count / 20f);
                                if (Speed > MaxSpeed) Speed = MaxSpeed;

                                if (Snake.direction == Snakes.Direction.Right)
                                {
                                    Snake.Points[i] = new Snakes.Point(Snake.Points[i].X + Speed, Snake.Points[i].Y);
                                }
                                else if (Snake.direction == Snakes.Direction.Down)
                                {
                                    Snake.Points[i] = new Snakes.Point(Snake.Points[i].X, Snake.Points[i].Y + Speed);
                                }
                                else if (Snake.direction == Snakes.Direction.Up)
                                {
                                    Snake.Points[i] = new Snakes.Point(Snake.Points[i].X, Snake.Points[i].Y - Speed);
                                }
                                else if (Snake.direction == Snakes.Direction.Left)
                                {
                                    Snake.Points[i] = new Snakes.Point(Snake.Points[i].X - Speed, Snake.Points[i].Y);
                                }
                            }
                        }

                        // Проверка границ (с учётом размера змеи 20px)
                        if (Snake.Points[0].X < 0 || Snake.Points[0].X > 780)
                        {
                            Snake.GameOver = true;
                        }
                        else if (Snake.Points[0].Y < 0 || Snake.Points[0].Y > 430)
                        {
                            Snake.GameOver = true;
                        }

                        // Проверка столкновения с собой
                        if (Snake.direction != Snakes.Direction.Start)
                        {
                            for (int i = 1; i < Snake.Points.Count; i++)
                            {
                                if (Math.Abs(Snake.Points[0].X - Snake.Points[i].X) < 10 &&
                                    Math.Abs(Snake.Points[0].Y - Snake.Points[i].Y) < 10)
                                {
                                    Snake.GameOver = true;
                                    break;
                                }
                            }
                        }

                        // Проверка съедания яблока
                        if (Math.Abs(Snake.Points[0].X - gameData.Points.X) < 15 &&
                            Math.Abs(Snake.Points[0].Y - gameData.Points.Y) < 15)
                        {
                            // Новое яблоко
                            gameData.Points = new Snakes.Point(
                                new Random().Next(20, 760),
                                new Random().Next(20, 410));

                            // Добавляем сегмент змее
                            Snake.Points.Add(new Snakes.Point()
                            {
                                X = Snake.Points[Snake.Points.Count - 1].X,
                                Y = Snake.Points[Snake.Points.Count - 1].Y
                            });

                            // Обновляем таблицу лидеров
                            Leaders.Add(new Leaders()
                            {
                                Name = User.Name,
                                Points = Snake.Points.Count - 3
                            });
                            Leaders = Leaders.OrderByDescending(x => x.Points).ThenBy(x => x.Name).ToList();
                            SaveLeaders();

                            // Устанавливаем место игрока
                            gameData.Top = Leaders.FindIndex(x => x.Points == Snake.Points.Count - 3 && x.Name == User.Name) + 1;
                        }
                    }
                }
                Send();
            }
        }

        public static void SaveLeaders()
        {
            string json = JsonConvert.SerializeObject(Leaders);
            StreamWriter SW = new StreamWriter("./leaders.txt");
            SW.WriteLine(json);
            SW.Close();
        }

        public static void LoadLeaders()
        {
            if (File.Exists("./leaders.txt"))
            {
                StreamReader SR = new StreamReader("./leaders.txt");
                string json = SR.ReadLine();
                SR.Close();
                if (!string.IsNullOrEmpty(json))
                    Leaders = JsonConvert.DeserializeObject<List<Leaders>>(json);
                else
                {
                    Leaders = new List<Leaders>();
                }
            }
            else
                Leaders = new List<Leaders>();
        }
    }
}
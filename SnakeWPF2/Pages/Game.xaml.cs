using Common2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SnakeWPF2.Pages
{
    /// <summary>
    /// Логика взаимодействия для Game.xaml
    /// </summary>
    public partial class Game : Page
    {
        public int StepCadr = 0;
        public Game()
        {
            InitializeComponent();
        }

        public void CreateUI()
        {
            Dispatcher.Invoke(() =>
            {
                if (StepCadr == 0) StepCadr = 1;
                else StepCadr = 0;
                canvas.Children.Clear();

                // Отрисовываем змею
                for (int iPoint = 0; iPoint < MainWindow.mainWindow.ViewModelGames.SnakesPlayers.Points.Count; iPoint++)
                {
                    Snakes.Point SnakePoint = MainWindow.mainWindow.ViewModelGames.SnakesPlayers.Points[iPoint];

                    // Копируем точку для анимации, чтобы не изменять оригинал
                    Snakes.Point animatedPoint = new Snakes.Point(SnakePoint.X, SnakePoint.Y);

                    if (iPoint != 0)
                    {
                        // Получаем следующую точку змеи
                        Snakes.Point NextSnakePoint = MainWindow.mainWindow.ViewModelGames.SnakesPlayers.Points[iPoint - 1];

                        // Если точка находится по горизонтали
                        if (SnakePoint.X > NextSnakePoint.X || SnakePoint.X < NextSnakePoint.X)
                        {
                            // если точка чётная
                            if (iPoint % 2 == 0)
                            {
                                // если кадр чётный
                                if (StepCadr % 2 == 0)
                                    // смещаем в одну сторону
                                    animatedPoint.Y -= 1;
                            }
                            else
                                // смещаем в другую сторону
                                animatedPoint.Y += 1;
                        }
                        // Если точка находится по вертикали
                        else if (SnakePoint.Y > NextSnakePoint.Y || SnakePoint.Y < NextSnakePoint.Y)
                        {
                            // если точка чётная
                            if (iPoint % 2 == 0)
                            {
                                // если кадр чётный
                                if (StepCadr % 2 == 0)
                                    // смещаем в одну сторону
                                    animatedPoint.X -= 1;
                            }
                            else
                                // смещаем в другую сторону
                                animatedPoint.X += 1;
                        }
                        else
                        {
                            // если кадр не чётный
                            if (StepCadr % 2 == 0)
                                // смещаем в одну сторону
                                animatedPoint.X += 1;
                            else
                                // смещаем в другую сторону
                                animatedPoint.X -= 1;
                        }
                    }

                    Brush Color;
                    if (iPoint == 0)
                        Color = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 127, 14));
                    else
                        Color = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 198, 19));

                    Ellipse ellipse = new Ellipse()
                    {
                        Width = 20,
                        Height = 20,
                        Margin = new Thickness(animatedPoint.X - 10, animatedPoint.Y - 10, 0, 0),
                        Fill = Color,
                        Stroke = Brushes.Black
                    };
                    canvas.Children.Add(ellipse);
                }

                // Отрисовываем яблоко (один раз, вне цикла)
                if (MainWindow.mainWindow.ViewModelGames.Points != null)
                {
                    Ellipse apple = new Ellipse()
                    {
                        Width = 20,
                        Height = 20,
                        Margin = new Thickness(
                           MainWindow.mainWindow.ViewModelGames.Points.X - 10,
                           MainWindow.mainWindow.ViewModelGames.Points.Y - 10, 0, 0),
                        Fill = Brushes.Red,
                        Stroke = Brushes.Black
                    };
                    canvas.Children.Add(apple);
                }
            });
        }
    }
}
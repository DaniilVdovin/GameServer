using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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

namespace AdminBoard
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            main();
        }
        void main()
        {
           // DetectDamageVector(new Vector2(500, 500),0, new Vector2(700, 600));
        }
       
        bool DetectDamageVector(Vector2 UserPosition, float UserRotation, Vector2 TargetPosition)
        {
            double angleRadian =(UserRotation) * Math.PI / 180;
            float x, y, k, m, n;
             float
                 x1 = UserPosition.x,
                 y1 = UserPosition.y,

                 x2 = (x1 + 300 - UserPosition.x) * (float)Math.Cos(angleRadian) - (y1 + 250 - UserPosition.y) * (float)Math.Sin(angleRadian) + UserPosition.x,
                 y2 = (x1 + 300 - UserPosition.x) * (float)Math.Sin(angleRadian) + (y1 + 250 - UserPosition.y) * (float)Math.Cos(angleRadian) + UserPosition.y,

                 x3 = (x1 + 300 - UserPosition.x) * (float)Math.Cos(angleRadian) - (y1 - 250 - UserPosition.y) * (float)Math.Sin(angleRadian) + UserPosition.x,
                 y3 = (x1 + 300 - UserPosition.x) * (float)Math.Sin(angleRadian) + (y1 - 250 - UserPosition.y) * (float)Math.Cos(angleRadian) + UserPosition.y;
            /*    
            float
            x1 = 200,
            y1 = 200,

            x2 = 800,
            y2 = 200+150,

            x3 = 800,
            y3 = 200-150;*/
            /*
            Polygon polygon = new Polygon();
            var points = new PointCollection();
            points.Add(new Point(x1, y1));
            points.Add(new Point(x2, y2));
            points.Add(new Point(x3, y3));
            polygon.Points = points;
            polygon.Fill = new SolidColorBrush(Colors.Blue);
            polygon.Stroke = new SolidColorBrush(Colors.Black);
            polygon.Opacity = 1;
            plane.Children.Add(polygon);*/
            //координаты вершин треугольника
            x = TargetPosition.x;
            y = TargetPosition.y; //координаты произвольной точки  

            k = (x1 - x) * (y2 - y1) - (x2 - x1) * (y1 - y);
            m = (x2 - x) * (y3 - y2) - (x3 - x2) * (y2 - y);
            n = (x3 - x) * (y1 - y3) - (x1 - x3) * (y3 - y);

            bool result = ((k >= 0 && m >= 0 && n >= 0) || (k <= 0 && m <= 0 && n <= 0) ? true : false);
            Console.WriteLine(result);
            /*Polygon pot = new Polygon();
            var pots = new PointCollection();
            pots.Add(new Point(x, y));
            pots.Add(new Point(x + 10, y + 5));
            pots.Add(new Point(x + 5, y + 10));
            pot.Points = pots;
            pot.Fill = new SolidColorBrush(result?Colors.Aqua:Colors.Red);
            pot.Stroke = new SolidColorBrush(Colors.Black);
            pot.Opacity = 10;
            plane.Children.Add(pot);*/
            return result;
        }
        void GetListRoom()
        {

        }
    }
}

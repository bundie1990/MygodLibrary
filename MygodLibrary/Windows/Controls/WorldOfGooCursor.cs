namespace Mygod.Windows.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Input;
    using System.Windows.Threading;

    /// <summary>
    /// 粘粘世界鼠标。
    /// </summary>
    public class WorldOfGooCursor : Decorator
    {
        static WorldOfGooCursor()
        {
            IsHitTestVisibleProperty.OverrideMetadata(typeof(WorldOfGooCursor), new FrameworkPropertyMetadata(false));
        }

        /// <summary>
        /// 初始化 WorldOfGooCursor 类的新实例。
        /// </summary>
        public WorldOfGooCursor()
        {
            refreshTimer = new DispatcherTimer(TimeSpan.FromSeconds(1 / 60.0), DispatcherPriority.Render,
                                               Refresh, Dispatcher);
            new Thread(Shrink) {IsBackground = true}.Start();
        }

        /// <summary>
        /// 前景色的附加属性。
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register("Foreground",
            typeof(Brush), typeof(WorldOfGooCursor), new PropertyMetadata(Brushes.Black));
        /// <summary>
        /// 边框色的附加属性。
        /// </summary>
        public static readonly DependencyProperty BorderBrushProperty = DependencyProperty.Register("BorderBrush",
            typeof(Brush), typeof(WorldOfGooCursor), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xb8, 0xb8, 0xb8))));
        /// <summary>
        /// 呼气后半径的附加属性。
        /// </summary>
        public static readonly DependencyProperty ExhaledRadiusProperty = DependencyProperty.Register("ExhaledRadius",
            typeof(double), typeof(WorldOfGooCursor), new PropertyMetadata(9.0));
        /// <summary>
        /// 吸气后半径的附加属性。
        /// </summary>
        public static readonly DependencyProperty InhaledRadiusProperty = DependencyProperty.Register("InhaledRadius",
            typeof(double), typeof(WorldOfGooCursor), new PropertyMetadata(10.0));
        /// <summary>
        /// 边框厚度的附加属性。
        /// </summary>
        public static readonly DependencyProperty BorderThicknessProperty = DependencyProperty.Register("BorderThickness",
            typeof(double), typeof(WorldOfGooCursor), new PropertyMetadata(3.0));
        /// <summary>
        /// 呼吸时长的附加属性。
        /// </summary>
        public static readonly DependencyProperty BreathingDurationProperty = DependencyProperty.Register("BreathingDuration",
            typeof(double), typeof(WorldOfGooCursor), new PropertyMetadata(20.0 / 9));
        /// <summary>
        /// 长度的附加属性。
        /// </summary>
        public static readonly DependencyProperty LengthProperty = DependencyProperty.Register("Length",
            typeof(int), typeof(WorldOfGooCursor), new PropertyMetadata(80));
        /// <summary>
        /// 收缩率的附加属性。
        /// </summary>
        public static readonly DependencyProperty ShrinkRateProperty = DependencyProperty.Register("ShrinkRate",
            typeof(double), typeof(WorldOfGooCursor), new PropertyMetadata(200.0));
        /// <summary>
        /// 是否使用贝赛尔曲线的附加属性。
        /// </summary>
        public static readonly DependencyProperty UseBezierCurveProperty = DependencyProperty.Register("UseBezierCurve",
            typeof(bool), typeof(WorldOfGooCursor), new PropertyMetadata(true));
        /// <summary>
        /// 全屏显示的附加属性。
        /// </summary>
        public static readonly DependencyProperty FullscreenModeProperty = DependencyProperty.Register("Fullscreen",
            typeof(bool), typeof(WorldOfGooCursor), new PropertyMetadata(false, FullscreenModeChanged));

        /// <summary>
        /// 获取或设置前景色。
        /// </summary>
        public Brush Foreground
        {
            get { return (Brush)GetValue(ForegroundProperty); } 
            set { SetValue(ForegroundProperty, value); }
        }
        /// <summary>
        /// 获取或设置边框色。
        /// </summary>
        public Brush BorderBrush
        {
            get { return (Brush)GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }
        /// <summary>
        /// 获取或设置呼气后的半径。
        /// </summary>
        public double ExhaledRadius
        {
            get { return (double)GetValue(ExhaledRadiusProperty); }
            set { SetValue(ExhaledRadiusProperty, value); }
        }
        /// <summary>
        /// 获取或设置吸气后的半径。
        /// </summary>
        public double InhaledRadius
        {
            get { return (double)GetValue(InhaledRadiusProperty); }
            set { SetValue(InhaledRadiusProperty, value); }
        }
        /// <summary>
        /// 获取或设置边框厚度。
        /// </summary>
        public double BorderThickness
        {
            get { return (double)GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }
        /// <summary>
        /// 获取或设置呼吸间隔。
        /// </summary>
        public double BreathingDuration
        {
            get { return (double)GetValue(BreathingDurationProperty); }
            set { SetValue(BreathingDurationProperty, value); }
        }
        /// <summary>
        /// 获取或设置长度。
        /// </summary>
        public int Length
        {
            get { return (int)GetValue(LengthProperty); }
            set { SetValue(LengthProperty, value); }
        }
        /// <summary>
        /// 获取或设置收缩率。
        /// </summary>
        public double ShrinkRate
        {
            get { return (double)GetValue(ShrinkRateProperty); }
            set { SetValue(ShrinkRateProperty, value); }
        }
        /// <summary>
        /// 获取或设置是否使用贝赛尔曲线。
        /// </summary>
        public bool UseBezierCurve
        {
            get { return (bool)GetValue(UseBezierCurveProperty); }
            set { SetValue(UseBezierCurveProperty, value); }
        }
        /// <summary>
        /// 获取或设置是否在全屏模式下。在全屏模式下获取鼠标坐标的方法将被重写，而且不会自动刷新。
        /// </summary>
        public bool FullscreenMode
        {
            get { return (bool)GetValue(FullscreenModeProperty); }
            set { SetValue(FullscreenModeProperty, value); }
        }

        private readonly LinkedList<Point> points = new LinkedList<Point>();
        private readonly DispatcherTimer refreshTimer;

        private static void FullscreenModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cursor = (WorldOfGooCursor) d;
            if ((bool) e.NewValue) cursor.refreshTimer.Stop();
            else cursor.refreshTimer.Start();
        }

        private void Shrink()
        {
            double shrinkRate = 400;
            while (true)
            {
                Dispatcher.Invoke((Action) (() =>
                {
                    var current = FullscreenMode ? GetMousePoint() : Mouse.GetPosition(this);
                    if (points.Count > 0)
                    {
                        var last = points.Last.Value;
                        double disx = current.X - last.X, disy = current.Y - last.Y, distance = Math.Sqrt(disx * disx + disy * disy);
                        var dis = (int) Math.Floor(distance);
                        for (var i = 0; i < dis; i++) AddPoint(last.X + disx / dis * i, last.Y + disy / dis * i);
                    }
                    AddPoint(current);
                    shrinkRate = ShrinkRate;
                }));
                Thread.Sleep(TimeSpan.FromSeconds(1 / shrinkRate));
            }
        // ReSharper disable FunctionNeverReturns
        }
        // ReSharper restore FunctionNeverReturns

        private void Refresh(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        private static Point GetMousePoint()
        {
            var point = System.Windows.Forms.Control.MousePosition;
            return new Point(point.X, point.Y);
        }

        private void AddPoint(Point point)
        {
            while (points.Count >= Length) points.RemoveFirst();
            points.AddLast(point);
        }
        private void AddPoint(double x, double y)
        {
            AddPoint(new Point(x, y));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (points.Count == 0) return;
            var size = ((InhaledRadius - ExhaledRadius) * Math.Cos(2 * Math.PI * DateTime.Now.Ticks / 10000000 / BreathingDuration)
                       + InhaledRadius + ExhaledRadius) / 2;
            var end = points.Last;
            var offset = 1;
            // ReSharper disable PossibleNullReferenceException
            if (end.Previous != null) while (end.Previous.Previous != null && (points.Last.Value - end.Previous.Previous.Value).Length < 1)
            // ReSharper restore PossibleNullReferenceException
            {
                end = end.Previous;
                offset++;
            }
            if (UseBezierCurve) BezierDraw(drawingContext, offset, size);
            else NormalDraw(drawingContext, end, offset, size);
        }

        private void NormalDraw(DrawingContext drawingContext, LinkedListNode<Point> end, int offset, double size)
        {
            var current = points.First;
            var i = 0;
            if (BorderThickness > 0) while (current != null && current.Previous != end)
            {
                var radius = ((double)(i++ + offset) / points.Count * 3 + 1) * size / 4;
                drawingContext.DrawEllipse(BorderBrush, null, current.Value, radius + BorderThickness, radius + BorderThickness);
            }
            current = points.First;
            i = 0;
            while (current != null && current.Previous != end)
            {
                var radius = ((double)(i++ + offset) / points.Count * 3 + 1) * size / 4;
                drawingContext.DrawEllipse(Foreground, null, current.Value, radius, radius);
            }
        }

        private void BezierDraw(DrawingContext drawingContext, int offset, double size)
        {
            var curve = BezierCurve.Bezier2D(points.Take(points.Count - offset + 1).ToArray(), points.Count - offset + 1);
            if (BorderThickness > 0) for (var i = 0; i < curve.Length; i++)
            {
                var radius = ((double)(i + offset) / points.Count * 3 + 1) * size / 4;
                drawingContext.DrawEllipse(BorderBrush, null, curve[i], radius + BorderThickness, radius + BorderThickness);
            }
            for (var i = 0; i < curve.Length; i++)
            {
                var radius = ((double)(i + offset) / points.Count * 3 + 1) * size / 4;
                drawingContext.DrawEllipse(Foreground, null, curve[i], radius, radius);
            }
        }
    }

    static class BezierCurve
    {
        private static readonly List<List<BigInteger>> CombinationsLookup = new List<List<BigInteger>>();
        private static readonly List<List<double>> CombinationsResults = new List<List<double>>();

        private static double GetCombination(int n, int i)
        {
            // return Factorial(n) / (Factorial(i) * Factorial(n - i));
            if (i > n >> 1) i = n - i;
            while (n >= CombinationsLookup.Count)
            {
                CombinationsLookup.Add(new List<BigInteger> { 1 });
                CombinationsResults.Add(new List<double> { 1 });
            }
            var list = CombinationsLookup[n];
            var results = CombinationsResults[n];
            while (i >= list.Count)
            {
                list.Add(list[list.Count - 1] * (n - list.Count + 1) / list.Count);
                results.Add((double) list[list.Count - 1]);
            }
            return results[i];
        }

        // Calculate Bernstein basis
        private static double Bernstein(int n, int i, double t)
        {
            double ti; /* t^i */
            double tni; /* (1 - t)^i */
            /* Prevent problems with pow */
            if (Math.Abs(t) < 1e-4 && i == 0) ti = 1.0;
            else ti = Math.Pow(t, i);

            if (n == i && Math.Abs(t - 1.0) < 1e-4) tni = 1.0;
            else tni = Math.Pow((1 - t), (n - i));
            return GetCombination(n, i) * ti * tni;
        }

        public static Point[] Bezier2D(Point[] b, int cpts)
        {
            var p = new Point[cpts];
            double t = 0, step = 1.0 / (cpts - 1);
            for (var j = 0; j != cpts; j++)
            {
                if ((1.0 - t) < 5e-6) t = 1.0;
                p[j].X = 0.0;
                p[j].Y = 0.0;
                for (var i = 0; i != b.Length; i++)
                {
                    var basis = Bernstein(b.Length - 1, i, t);
                    p[j].X += basis * b[i].X;
                    p[j].Y += basis * b[i].Y;
                }
                t += step;
            }
            return p;
        }
    }
}

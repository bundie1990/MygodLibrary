using System.Windows;
using System.Windows.Media;

namespace Mygod
{
    using System;
    using System.Text;
    using System.Windows.Threading;

    /// <summary>
    /// 辅助类。
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// 用与 System.Windows.Threading.Dispatcher 关联的线程上的指定参数同步执行指定委托。
        /// </summary>
        /// <param name="dispatcher">指定 System.Windows.Threading.Dispatcher。</param>
        /// <param name="a">该方法将被送到队列中。</param>
        public static void Invoke(this Dispatcher dispatcher, Action a)
        {
            dispatcher.Invoke(a);
        }

        /// <summary>
        /// 用与 System.Windows.Threading.Dispatcher 关联的线程上的指定参数同步执行指定委托。
        /// </summary>
        /// <param name="dispatcher">指定 System.Windows.Threading.Dispatcher。</param>
        /// <param name="a">该方法将被送到队列中。</param>
        public static T Invoke<T>(this Dispatcher dispatcher, Func<T> a)
        {
            return (T) dispatcher.Invoke(a);
        }


        public static T FindVisualChild<T>(this DependencyObject obj) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T) return (T)child;
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }
    }
}

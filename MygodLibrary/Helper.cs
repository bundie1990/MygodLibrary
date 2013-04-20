using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mygod
{
    using System;
    using System.Windows.Threading;
    using System.Windows;
    using System.Windows.Media;
    using System.Text;

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
        
        /// <summary>
        /// 用于将错误转化为可读的字符串。
        /// </summary>
        /// <param name="e">错误。</param>
        /// <returns>错误字符串。</returns>
        public static string GetMessage(this Exception e)
        {
            var result = new StringBuilder();
            GetMessage(e, result);
            return result.ToString();
        }

        private static void GetMessage(Exception e, StringBuilder result)
        {
            while (e != null && !(e is AggregateException))
            {
                result.AppendFormat("({0}) {1}{2}{3}{2}", e.GetType(), e.Message, Environment.NewLine, e.StackTrace);
                e = e.InnerException;
            }
            var ae = e as AggregateException;
            if (ae != null) foreach (var ex in ae.InnerExceptions) GetMessage(ex, result);
        }
        /// <summary>
        /// Inverts a Matrix. The Invert functionality on the Matrix type is 
        /// internal to the framework only. Since Matrix is a struct, an out 
        /// parameter must be presented.
        /// </summary>
        /// <param name="m">The Matrix object.</param>
        /// <param name="outputMatrix">The matrix to return by an output 
        /// parameter.</param>
        /// <returns>Returns a value indicating whether the type was 
        /// successfully inverted. If the determinant is 0.0, then it cannot 
        /// be inverted and the original instance will remain untouched.</returns>
        public static bool Invert(this Matrix m, out Matrix outputMatrix)
        {
            var determinant = m.M11 * m.M22 - m.M12 * m.M21;
            if (Math.Abs(determinant) < 1e-4)
            {
                outputMatrix = m;
                return false;
            }

            var matCopy = m;
            m.M11 = matCopy.M22 / determinant;
            m.M12 = -1 * matCopy.M12 / determinant;
            m.M21 = -1 * matCopy.M21 / determinant;
            m.M22 = matCopy.M11 / determinant;
            m.OffsetX = (matCopy.OffsetY * matCopy.M21 - matCopy.OffsetX * matCopy.M22) / determinant;
            m.OffsetY = (matCopy.OffsetX * matCopy.M12 - matCopy.OffsetY * matCopy.M11) / determinant;

            outputMatrix = m;
            return true;
        }

        /// <summary>
        /// An implementation of the Contains member of string that takes in a 
        /// string comparison. The traditional .NET string Contains member uses 
        /// StringComparison.Ordinal.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="value">The string value to search for.</param>
        /// <param name="comparison">The string comparison type.</param>
        /// <returns>Returns true when the substring is found.</returns>
        public static bool Contains(this string s, string value, StringComparison comparison)
        {
            return s.IndexOf(value, comparison) >= 0;
        }

        /// <summary>
        /// Retrieves all the visual children of a framework element.
        /// </summary>
        /// <param name="parent">The parent framework element.</param>
        /// <returns>The visual children of the framework element.</returns>
        public static IEnumerable<DependencyObject> GetVisualChildren(this DependencyObject parent)
        {
            Debug.Assert(parent != null, "The parent cannot be null.");
            var childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (var counter = 0; counter < childCount; counter++) yield return VisualTreeHelper.GetChild(parent, counter);
        }

        /// <summary>
        /// Retrieves all the logical children of a framework element using a 
        /// breadth-first search.  A visual element is assumed to be a logical 
        /// child of another visual element if they are in the same namescope.
        /// For performance reasons this method manually manages the queue 
        /// instead of using recursion.
        /// </summary>
        /// <param name="parent">The parent framework element.</param>
        /// <returns>The logical children of the framework element.</returns>
        public static IEnumerable<FrameworkElement> GetLogicalChildrenBreadthFirst(this FrameworkElement parent)
        {
            Debug.Assert(parent != null, "The parent cannot be null.");
            var queue = new Queue<FrameworkElement>(parent.GetVisualChildren().OfType<FrameworkElement>());
            while (queue.Count > 0)
            {
                var element = queue.Dequeue();
                yield return element;
                foreach (var visualChild in element.GetVisualChildren().OfType<FrameworkElement>()) queue.Enqueue(visualChild);
            }
        }
    }
}

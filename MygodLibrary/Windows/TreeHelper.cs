using System.Windows;
using System.Windows.Media;

namespace Mygod.Windows
{
    public static class TreeHelper
    {
        public static T FindVisualParent<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null && !(source is T))
                if (source is Visual) source = VisualTreeHelper.GetParent(source); else source = LogicalTreeHelper.GetParent(source);
            return source as T;
        }

        public static T FindVisualChild<T>(DependencyObject obj, string name = null) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                var test = child as T;
                if (test != null && (name == null || test is FrameworkElement && (test as FrameworkElement).Name == name)) return test;
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }
    }
}

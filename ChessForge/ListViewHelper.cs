using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Collections.ObjectModel;
using System.Xml;
using System.IO;
using System.Text;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessForge
{
    public static class ListViewHelper
    {
        public static TextBlock GetElementFromCellTemplate(ListView listView, Int32 row, Int32 column, String name)
        {
            if (row >= listView.Items.Count || row < 0)
            {
                return null;
            }
            GridView gridView = listView.View as GridView;

            if (gridView == null) { return null; }

            if (column >= gridView.Columns.Count || column < 0)
            {
                return null; 
            }

            ListViewItem item = listView.ItemContainerGenerator.ContainerFromItem(listView.Items[row]) as ListViewItem;

            TextBlock tb = null;

            if (item != null)
            {
                GridViewRowPresenter rowPresenter = GetFrameworkElementByName<GridViewRowPresenter>(item);

                if (rowPresenter != null)
                {
                    ContentPresenter templatedParent = VisualTreeHelper.GetChild(rowPresenter, column) as ContentPresenter;
                    DataTemplate dataTemplate = gridView.Columns[column].CellTemplate;

                    tb = GetFrameworkElementByName<TextBlock>(templatedParent);


                    //if (dataTemplate != null && templatedParent != null)
                    //{
                    //    return dataTemplate.FindName(name, templatedParent) as FrameworkElement;
                    //}
                }
            }
            return tb;
        }
        private static T GetFrameworkElementByName<T>(FrameworkElement referenceElement) where T : FrameworkElement
        {
            FrameworkElement child = null;

            for (Int32 i = 0; i < VisualTreeHelper.GetChildrenCount(referenceElement); i++)
            {
                child = VisualTreeHelper.GetChild(referenceElement, i) as FrameworkElement;
                System.Diagnostics.Debug.WriteLine(child);
                if (child != null && child.GetType() == typeof(T))
                {
                    break;
                }
                else if (child != null)
                {
                    child = GetFrameworkElementByName<T>(child);
                    if (child != null && child.GetType() == typeof(T))
                    {
                        break;
                    }
                }
            }
            return child as T;
        }
    }
}
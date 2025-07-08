using System.Windows;
using System.Windows.Controls;

namespace ChessForge
{
    public class ThicknessUtils
    {
        /// <summary>
        /// Sets the margin of the specified control to the new Thickness value.
        /// Makes a copy of the provided Thickness to avoid modifying the original.
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="thickness"></param>
        public static void SetControlThickness(Control ctrl, Thickness thickness)
        {
            Thickness newThickness = new Thickness(
                thickness.Left,
                thickness.Top,
                thickness.Right,
                thickness.Bottom);

            ctrl.Margin = newThickness;
        }

        /// <summary>
        /// Sets the left margin of the specified control to the new value.
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static void SetControlLeftMargin(Control ctrl, Thickness defaultThickness, double newValue)
        {
            ctrl.Margin = SetLeftMargin(defaultThickness, newValue);
        }

        /// <summary>
        /// Sets the top margin of the specified control to the new value.
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="newValue"></param>
        public static void SetControlTopMargin(Control ctrl, double newValue)
        {
            ctrl.Margin = SetTopMargin(ctrl.Margin, newValue);
        }

        /// <summary>
        /// Sets the right margin of the specified control to the new value.
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="newValue"></param>
        public static void SetControlRightMargin(Control ctrl, Thickness defaultThickness, double newValue)
        {
            ctrl.Margin = SetRightMargin(defaultThickness, newValue);
        }

        /// <summary>
        /// Sets the bottom margin of the specified control to the new value.
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="newValue"></param>
        public static void SetControlBottomMargin(Control ctrl, double newValue)
        {
            ctrl.Margin = SetBottomMargin(ctrl.Margin, newValue);
        }

        /// <summary>
        /// Adjusts the left margin of the specified control to the new value.
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static void AdjustControlLeftMargin(Control ctrl, double newValue)
        {
            ctrl.Margin = AdjustLeftMargin(ctrl.Margin, newValue);
        }

        /// <summary>
        /// Adjusts the top margin of the specified control to the new value.
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="newValue"></param>
        public static void AdjustControlTopMargin(Control ctrl, double newValue)
        {
            ctrl.Margin = AdjustTopMargin(ctrl.Margin, newValue);
        }

        /// <summary>
        /// Adjusts the right margin of the specified control to the new value.
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="newValue"></param>
        public static void AdjustControlRightMargin(Control ctrl, double newValue)
        {
            ctrl.Margin = AdjustRightMargin(ctrl.Margin, newValue);
        }

        /// <summary>
        /// Adjusts the bottom margin of the specified control to the new value.
        /// </summary>
        /// <param name="ctrl"></param>
        /// <param name="newValue"></param>
        public static void AdjustControlBottomMargin(Control ctrl, double newValue)
        {
            ctrl.Margin = AdjustBottomMargin(ctrl.Margin, newValue);
        }

        /// <summary>
        /// Returns a new Thickness with the left margin set to the new value.
        /// </summary>
        /// <param name="thickness"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static Thickness SetLeftMargin(Thickness defaultThickness, double newValue)
        {
            return new Thickness(
                newValue,
                defaultThickness.Top,
                defaultThickness.Right,
                defaultThickness.Bottom);
        }

        /// <summary>
        /// Returns a new Thickness with the top margin set to the new value.
        /// </summary>
        /// <param name="thickness"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static Thickness SetTopMargin(Thickness thickness, double newValue)
        {
            return new Thickness(
                thickness.Left,
                newValue,
                thickness.Right,
                thickness.Bottom);
        }

        /// <summary>
        /// Returns a new Thickness with the right margin set to the new value.
        /// </summary>
        /// <param name="defaultThickness"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static Thickness SetRightMargin(Thickness defaultThickness, double newValue)
        {
            return new Thickness(
                defaultThickness.Left,
                defaultThickness.Top,
                newValue,
                defaultThickness.Bottom);
        }

        /// <summary>
        /// Returns a new Thickness with the bottom margin set to the new value.
        /// </summary>
        /// <param name="thickness"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static Thickness SetBottomMargin(Thickness thickness, double newValue)
        {
            return new Thickness(
                thickness.Left,
                thickness.Top,
                thickness.Right,
                newValue);
        }

        /// <summary>
        /// Returns a new Thickness with the left margin adjusted to the new value.
        /// </summary>
        /// <param name="thickness"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static Thickness AdjustLeftMargin(Thickness thickness, double newValue)
        {
            return new Thickness(
                thickness.Left + newValue,
                thickness.Top,
                thickness.Right,
                thickness.Bottom);
        }

        /// <summary>
        /// Returns a new Thickness with the top margin adjusted to the new value.
        /// </summary>
        /// <param name="thickness"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static Thickness AdjustTopMargin(Thickness thickness, double newValue)
        {
            return new Thickness(
                thickness.Left,
                thickness.Top + newValue,
                thickness.Right,
                thickness.Bottom);
        }

        /// <summary>
        /// Returns a new Thickness with the right margin adjusted to the new value.
        /// </summary>
        /// <param name="thickness"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static Thickness AdjustRightMargin(Thickness thickness, double newValue)
        {
            return new Thickness(
                thickness.Left,
                thickness.Top,
                thickness.Right + newValue,
                thickness.Bottom);
        }

        /// <summary>
        /// Returns a new Thickness with the bottom margin adjusted to the new value.
        /// </summary>
        /// <param name="thickness"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static Thickness AdjustBottomMargin(Thickness thickness, double newValue)
        {
            return new Thickness(
                thickness.Left,
                thickness.Top,
                thickness.Right,
                thickness.Bottom + newValue);
        }
    }
}

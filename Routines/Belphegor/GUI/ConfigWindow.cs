using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Belphegor.Settings;
using Belphegor.Utilities;

namespace Belphegor.GUI
{
    internal class ConfigWindow : Window
    {
        private readonly Grid _mainCanvas = new Grid();

        public ConfigWindow(string title, string logo, string logoTagLine, int width, int height,
                            BelphegorSettings mainSettingsSource)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            Topmost = true;
            Width = width;
            Height = height;
            Title = title;

            // Store _mainCanvas outside this so we don't have to cast and whatnot.
            Content = _mainCanvas;
            _mainCanvas.RowDefinitions.Add(new RowDefinition());
            _mainCanvas.RowDefinitions.Add(new RowDefinition {Height = new GridLength(50)});

            // Setup theme vars to match HB's window.
            //ResourceDictionary theme = Application.Current.Resources;
            //Resources.Source = theme.Source;
            Background = Brushes.LightGray;

            // Add tab controls...
            var tabs = new TabControl();
            Grid.SetRow(tabs, 0);
            _mainCanvas.Children.Add(tabs);

            #region General Tab

            TabItem mainTab = GenerateTab("General", mainSettingsSource);
            tabs.Items.Add(mainTab);

            #endregion

            #region Debug Tab

            if (BelphegorSettings.Instance.Debug.IsDebugTabActive)
            {
                TabItem dTab = GenerateTab("Debug", mainSettingsSource.Debug);
                tabs.Items.Add(dTab);
            }

            #endregion

            #region Avoidance Tab

            TabItem aTab = GenerateTab("Avoidance", mainSettingsSource.Avoidance);
            tabs.Items.Add(aTab);

            #endregion

            #region Kiting Tab

            TabItem kTab = GenerateTab("Kiting", mainSettingsSource.Kiting);
            tabs.Items.Add(kTab);

            #endregion

            #region Barbarian Tab

            TabItem barbTab = GenerateTab("Barbarian", mainSettingsSource.Barbarian);
            tabs.Items.Add(barbTab);

            #endregion

            #region Demon Hunter Tab

            TabItem demonTab = GenerateTab("Demon Hunter", mainSettingsSource.DemonHunter);
            tabs.Items.Add(demonTab);

            #endregion

            #region Monk Tab

            TabItem monkTab = GenerateTab("Monk", mainSettingsSource.Monk);
            tabs.Items.Add(monkTab);

            #endregion

            #region Witch Doctor Tab

            TabItem wdTab = GenerateTab("Witch Doctor", mainSettingsSource.WitchDoctor);
            tabs.Items.Add(wdTab);

            #endregion

            #region Wizard Tab

            TabItem wTab = GenerateTab("Wizard", mainSettingsSource.Wizard);
            tabs.Items.Add(wTab);

            #endregion

            #region Singular "Logo"

            var logoPanel = new StackPanel();
            Grid.SetRow(logoPanel, 1);
            _mainCanvas.Children.Add(logoPanel);

            var logoMain = new TextBlock
                               {
                                   Text = logo,
                                   Foreground = new SolidColorBrush(Colors.White),
                                   FontSize = 20,
                                   FontFamily = new FontFamily("Impact"),
                                   Padding = new Thickness(5, 0, 0, 0)
                               };
            //logoMain.FontWeight = FontWeights.Bold;
            logoPanel.Children.Add(logoMain);

            var logoTag = new TextBlock
                              {
                                  Text = logoTagLine,
                                  Foreground = new SolidColorBrush(Colors.White),
                                  FontWeight = FontWeights.Bold,
                                  Padding = new Thickness(5)
                              };

            logoPanel.Children.Add(logoTag);

            #endregion
        }

        private TabItem GenerateTab(string tabtitle, object source)
        {
            var tab = new TabItem {Header = tabtitle};
            var tabPanel = new StackPanel();
            var sv = new ScrollViewer
                         {Content = tabPanel, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(2)};
            tab.Content = sv;

            IEnumerable<PropertyInfo> settingsProperties =
                source.GetType().GetProperties().Where(p => p.GetCustomAttributes(false).Any(a => a is SettingAttribute));

            BuildSettings(tabPanel, settingsProperties, source);

            return tab;
        }

        private static T GetAttribute<T>(PropertyInfo pi) where T : Attribute
        {
            object attr = pi.GetCustomAttributes(false).FirstOrDefault(a => a is T);
            if (attr != null)
            {
                return attr as T;
            }
            return null;
        }

        private void BuildSettings(Panel tabPanel, IEnumerable<PropertyInfo> settings, object source)
        {
            var categories = new Dictionary<string, StackPanel>();

            foreach (PropertyInfo pi in settings.OrderBy(p => p.PropertyType.Name))
            {
                // First get some display stuff.
                // By default, any settings w/o a category set, go into "Misc"
                string category = GetAttribute<CategoryAttribute>(pi) != null
                                      ? GetAttribute<CategoryAttribute>(pi).Category
                                      : "Miscellaneous";
                string displayName = GetAttribute<DisplayNameAttribute>(pi) != null
                                         ? GetAttribute<DisplayNameAttribute>(pi).DisplayName
                                     // Default to the property name if no display name is given.
                                         : pi.Name;
                string description = GetAttribute<DescriptionAttribute>(pi) != null
                                         ? GetAttribute<DescriptionAttribute>(pi).Description
                                         : null;

                if (!categories.ContainsKey(category))
                {
                    categories.Add(category, new StackPanel());
                }

                StackPanel group = categories[category];

                //Logger.Write(displayName + " -> " + description);

                Type returnType = pi.PropertyType;

                // Deal with enums in a "special" way. The typecode for any enum will be int32 by default (unless marked as something else)
                // So we really want a dropdown, not a textbox.
                if (returnType.IsEnum)
                {
                    AddComboBoxForEnum(group, source, pi, displayName, description, returnType);
                    continue;
                }

                // Easiest way to blanket-statement a bunch of editable values. (Quite a few will just be textbox editors)
                switch (Type.GetTypeCode(returnType))
                {
                    case TypeCode.Boolean:
                        AddCheckbox(group, source, pi, displayName, description);
                        break;
                    case TypeCode.Char:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        AddSlider(group, source, pi, displayName, description);
                        break;
                    case TypeCode.String:
                        AddEditBox(group, source, pi, displayName, description);
                        break;
                    default:
                        Logger.Write("Don't know how to display " + returnType);
                        break;
                }
            }

            foreach (var sp in categories.OrderBy(kv => kv.Key))
            {
                var gb = new GroupBox {Content = sp.Value, Header = sp.Key};
                tabPanel.Children.Add(gb);
            }
        }

        private void AddBinding(FrameworkElement ctrl, string xpath, object source, PropertyInfo bindTo,
                                DependencyProperty depProp)
        {
            var b = new Binding(xpath)
                        {Source = source, Path = new PropertyPath(bindTo.Name), Mode = BindingMode.TwoWay};
            ctrl.SetBinding(depProp, b);
        }

        private void AddCheckbox(Panel ctrl, object source, PropertyInfo bindTo, string label, string tooltip)
        {
            var cb = new CheckBox {Content = label, ToolTip = !string.IsNullOrEmpty(tooltip) ? tooltip : null};

            // And the binding so we don't have to do a lot of nasty event handling.
            AddBinding(cb, "IsChecked", source, bindTo, ToggleButton.IsCheckedProperty);

            ctrl.Children.Add(cb);
        }

        private void AddSlider(Panel ctrl, object source, PropertyInfo bindTo, string label, string tooltip)
        {
            var display = new StackPanel {Orientation = Orientation.Horizontal, Width = ctrl.Width};
            var l = new Label {Content = label, Margin = new Thickness(5, 5, 5, 3), ToolTip = tooltip};
            var sldLbl = new Label();

            var s = new Slider();
            // Find min/max
            var attr = GetAttribute<LimitAttribute>(bindTo);
            if (attr != null)
            {
                s.Maximum = attr.High;
                s.Minimum = attr.Low;
                s.TickFrequency = Math.Abs(attr.High - attr.Low)/100;
                s.SmallChange = s.TickFrequency;
                s.LargeChange = s.TickFrequency*10f;
            }
            s.MinWidth = 65;
            s.TickPlacement = TickPlacement.BottomRight;
            s.ToolTip = tooltip;
            AddBinding(s, "Value", source, bindTo, RangeBase.ValueProperty);

            var b = new Binding("Value") {Source = s, Path = new PropertyPath("Value"), Mode = BindingMode.TwoWay};
            sldLbl.SetBinding(ContentProperty, b);
            sldLbl.ContentStringFormat = "N2";
            sldLbl.Width = 38;

            display.Children.Add(sldLbl);
            display.Children.Add(s);
            display.Children.Add(l);

            ctrl.Children.Add(display);
        }

        private void AddEditBox(Panel ctrl, object source, PropertyInfo bindTo, string label, string tooltip)
        {
            // This is a bit tricky. We want to stack the edit box to the right of the label.
            // We do this via another stack panel, with a changed "stack" way.

            var display = new StackPanel {Orientation = Orientation.Horizontal, Width = ctrl.Width};
            var l = new Label {Content = label, Margin = new Thickness(5, 5, 5, 3)};

            var tb = new TextBox
                         {
                             ToolTip = tooltip,
                             Background = Background,
                             BorderBrush = new SolidColorBrush(Colors.LightGray),
                             Margin = new Thickness(2, 3, 5, 3),
                             MinWidth = 50
                         };

            // Add the textbox/label to the stack panel so we can have it side-to-side.
            display.Children.Add(tb);
            display.Children.Add(l);

            // Don't forget the damned binding.
            AddBinding(tb, "Text", source, bindTo, TextBox.TextProperty);

            // And add it to the main control.
            ctrl.Children.Add(display);
        }

        private void AddComboBoxForEnum(Panel ctrl, object source, PropertyInfo bindTo, string label, string tooltip,
                                        Type enumType)
        {
            var display = new StackPanel {Orientation = Orientation.Horizontal, Width = ctrl.Width};
            var l = new Label {Content = label, Margin = new Thickness(5, 5, 5, 3)};

            var cb = new ComboBox
                         {
                             ToolTip = tooltip,
                             Background = Background,
                             BorderBrush = new SolidColorBrush(Colors.LightGray),
                             BorderThickness = new Thickness(2)
                         };
            foreach (object val in Enum.GetValues(enumType))
            {
                cb.Items.Add(val);
            }

            AddBinding(cb, "SelectedItem", source, bindTo, Selector.SelectedItemProperty);

            display.Children.Add(cb);
            display.Children.Add(l);

            ctrl.Children.Add(display);
        }
    }
}
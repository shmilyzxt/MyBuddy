using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Belphegor.Settings;
using Zeta.Common;
using Zeta.Common.Plugins;

namespace Belphegor.Utilities
{
    internal class PointGenerationDebugDrawer
    {
        private static PointGenerationDebugDrawer _instance;

        private readonly MethodInfo _canvasGetMethdod;
        private readonly List<DebugOutput> _debugPoints = new List<DebugOutput>();
        private readonly MethodInfo _eventGetMethod;
        private readonly MethodInfo _eventSetMethod;

        /// <summary>
        /// Initializes a new instance of the PointGenerationDebugDrawer class.
        /// </summary>
        internal PointGenerationDebugDrawer()
        {
            Type minimapType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).First(ct => ct.Name == "MiniMapViewer");
            PropertyInfo ei = minimapType.GetProperty("OnUpdate", BindingFlags.Public | BindingFlags.Static);
            _eventGetMethod = ei.GetGetMethod();
            _eventSetMethod = ei.GetSetMethod();

            PropertyInfo pi = minimapType.GetProperty("DebugCanvas", BindingFlags.Public | BindingFlags.Static);
            _canvasGetMethdod = pi.GetGetMethod();

            Updating += MiniMapUpdateing;
        }

        internal static PointGenerationDebugDrawer Instance
        {
            get
            {
                if (BelphegorSettings.Instance.Debug.IsDebugDrawingActive && _instance == null &&
                    PluginManager.Plugins.Any(p => p.Plugin.Name == "MiniMap"))
                {
                    _instance = new PointGenerationDebugDrawer();
                }
                return _instance;
            }
        }

        private EventHandler<EventArgs> Updating
        {
            get { return (EventHandler<EventArgs>) _eventGetMethod.Invoke(null, null); }
            set { _eventSetMethod.Invoke(null, new object[] {value}); }
        }

        private Canvas DebugCanvas
        {
            get { return (Canvas) _canvasGetMethdod.Invoke(null, null); }
        }

        internal void AddDebugPoint(Vector3 point, Brush color)
        {
            _debugPoints.Add(new DebugOutput(point, color));
        }

        internal void ClearDebugOutput()
        {
            _debugPoints.Clear();
        }

        private void MiniMapUpdateing(object sender, EventArgs e)
        {
            Canvas debugCanvas = DebugCanvas;
            foreach (DebugOutput debugPoint in _debugPoints)
            {
                var c = new Rectangle {Fill = debugPoint.Color, Width = 2.5, Height = 2.5};
                Canvas.SetTop(c, debugPoint.Point.Y - c.Height/2);
                Canvas.SetLeft(c, debugPoint.Point.X - c.Width/2);
                debugCanvas.Children.Add(c);
            }
        }

        #region Nested type: DebugOutput

        internal sealed class DebugOutput
        {
            public DebugOutput(Vector3 point, Brush color)
            {
                Point = point;
                Color = color;
            }

            public Vector3 Point { get; set; }
            public Brush Color { get; set; }
        }

        #endregion
    }
}
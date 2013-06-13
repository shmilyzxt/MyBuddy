using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Belphegor.Settings;
using Zeta;
using Zeta.Common;
using Zeta.CommonBot;
using Zeta.Internals.Actors;
using Zeta.Navigation;
using Zeta.Pathfinding;

namespace Belphegor.Utilities
{
    internal sealed class BelphegorCachedSearchAreaProvider : ISearchAreaProvider
    {
        private readonly MainGridProvider _cachedProvider = new MainGridProvider();
        private Vector3 _playerPosition;
        private bool _updateRequired = true;

        /// <summary>
        /// Initializes a new instance of the BelphegorCachedSearchAreaProvider class.
        /// </summary>
        private BelphegorCachedSearchAreaProvider()
        {
            Pulsator.OnPulse += OnPulse;
        }

        static BelphegorCachedSearchAreaProvider()
        {
            Instance = new BelphegorCachedSearchAreaProvider();
        }

        public static BelphegorCachedSearchAreaProvider Instance { get; private set; }

        private void OnPulse(object sender, EventArgs e)
        {
            _updateRequired = true;
        }

        #region ISeachAreaProvider implementation

        Vector2 ISearchAreaProvider.BoundsMax
        {
            get { return _cachedProvider.BoundsMax; }
        }

	    public Vector3[] CellPositions { get; private set; }

	    Vector2 ISearchAreaProvider.BoundsMin
        {
            get { return _cachedProvider.BoundsMin; }
        }

        bool ISearchAreaProvider.CanStandAt(Point cell)
        {
            return _cachedProvider.CanStandAt(cell);
        }

        float[] ISearchAreaProvider.GetCellWeights()
        {
            return _cachedProvider.GetCellWeights();
        }

        float ISearchAreaProvider.GetHeight(Vector2 point)
        {
            return _cachedProvider.GetHeight(point);
        }

        Vector2 ISearchAreaProvider.GridToWorld(Point cell)
        {
            return _cachedProvider.GridToWorld(cell);
        }

        int ISearchAreaProvider.Height
        {
            get { return _cachedProvider.Height; }
        }

        bool ISearchAreaProvider.Raycast(Vector2 start, Vector2 end, out Vector2 hitPoint)
        {
            return _cachedProvider.Raycast(start, end, out hitPoint);
        }

        BitArray ISearchAreaProvider.SearchArea
        {
            get { return _cachedProvider.SearchArea; }
        }

        void ISearchAreaProvider.Update()
        {
            using (
                new PerformanceLogger(BelphegorSettings.Instance.Debug.IsDebugSearchAreaProviderLoggingActive,
                                      "SearchAreaProvider Update"))
            {
                if (_updateRequired)
                {
                    _updateRequired = false;
                    _cachedProvider.Update();
                    _playerPosition = ZetaDia.Me.Position;
                    IEnumerable<DiaObject> units =
                        CombatTargeting.Instance.LastObjects.Where(
                            u => u.Position.DistanceSqr(_playerPosition) > 2.5*2.5);
                    float[] weights = _cachedProvider.GetCellWeights();

                    if (weights != null)
                    {
                        foreach (DiaObject item in units)
                        {
                            Point cell = _cachedProvider.WorldToGrid(item.Position.ToVector2());
                            int cellIndex = cell.Y*_cachedProvider.Width + cell.X;
                            _cachedProvider.SearchArea[cellIndex] = false;
                            float currentWeight = weights[cellIndex];
                            weights[cellIndex] = currentWeight*2;
                        }
                    }
                }
            }
        }

        int ISearchAreaProvider.Width
        {
            get { return _cachedProvider.Width; }
        }

        bool ISearchAreaProvider.WorldRequiresPathfinding
        {
            get { return _cachedProvider.WorldRequiresPathfinding; }
        }

        Point ISearchAreaProvider.WorldToGrid(Vector2 pos)
        {
            return _cachedProvider.WorldToGrid(pos);
        }

        #endregion
    }
}
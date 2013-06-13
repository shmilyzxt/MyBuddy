using System;
using System.Collections.Generic;
using System.Linq;
using Belphegor.Settings;
using Zeta.Common;

namespace Belphegor.Utilities
{
    internal class PointCollisionTree
    {
        internal class PointNode : IComparable<PointNode>
        {
            private readonly Vector3 _position;
            private readonly float _radius;
            private readonly Dictionary<PointNode, float> _weightSet = new Dictionary<PointNode, float>();
            
            internal Vector3 Position
            {
                get
                {
                    return _position;
                }
            }

            internal float Radius
            {
                get
                {
                    return _radius;
                }
            }

            internal float Weight
            {
                get
                {
                    float weight = 0f;
                    if(_weightSet.Any())
                        weight = _weightSet.Values.Average();
                    if(BelphegorSettings.Instance.Debug.IsDebugAvoidanceLog)
                        Logger.WriteVerbose("The weight of the current node is {0}", weight);
                    return weight;
                }
            }

            /// <summary>
            /// Initializes a new instance of the PointNode class.
            /// </summary>
            /// <param name="position"></param>
            /// <param name="radius"></param>
            internal PointNode(Vector3 position, float radius)
            {
                _position = position;
                _radius = radius;
            }

            /// <summary>
            /// Compares the current object with another object of the same type.
            /// </summary>
            /// <returns>
            /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This object is equal to <paramref name="other" />. Greater than zero This object is greater than <paramref name="other" />. 
            /// </returns>
            /// <param name="other">
            /// An object to compare with this object.
            /// </param>
            public int CompareTo(PointNode other)
            {
                return Weight.CompareTo(other.Weight);
            }

            internal void AddConnection(PointNode other)
            {
                if (!_weightSet.ContainsKey(other))
                {
                    float weight = Math.Abs(Position.Distance(other.Position) - Radius - other.Radius);
                    _weightSet.Add(other, weight);
                    other._weightSet.Add(this, weight);
                }
            }
        }

        private readonly List<PointNode> _points = new List<PointNode>();
        private List<PointNode> _orderedPoints;

        internal PointCollisionTree()
        {
            
        }
        
        internal void AddPoint(PointNode point)
        {
            _points.ForEach(p => p.AddConnection(point));
            _points.Add(point);
        }

        internal PointNode BestPoint
        {
            get
            {
                return Nodes.FirstOrDefault();
            }
        }

        internal List<PointNode> Nodes
        {
            get
            {
                return _orderedPoints ?? (_orderedPoints = _points.OrderBy(p => p.Weight).ToList());
            }
        }
    }
}

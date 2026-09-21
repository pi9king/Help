using System.Collections.Generic;
using UnityEngine;

namespace Help.Dungeon
{
    public sealed class ReachabilitySet
    {
        private readonly HashSet<Vector2Int> _nodes;

        public ReachabilitySet(HashSet<Vector2Int> nodes) => _nodes = nodes;

        public bool Contains(Vector2Int cell) => _nodes.Contains(cell);
        public IEnumerable<Vector2Int> Nodes => _nodes;
    }
}

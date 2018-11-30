using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Assets.Scripts
{
    public class Bucket
    {
        private readonly Vector2 _lowerCorner;
        private readonly Vector2 _upperCorner;

        private readonly Dictionary<Faction, List<Unit>> _containedUnits;

        public Bucket(Vector2 lowerCorner, Vector2 size)
        {
            _lowerCorner = lowerCorner;
            _upperCorner = lowerCorner + size;

            _containedUnits = new Dictionary<Faction, List<Unit>>();
            foreach (var faction in (Faction[])Enum.GetValues(typeof(Faction)))
            {
                _containedUnits.Add(faction, new List<Unit>());
            }
        }

        public bool Contains(Vector3 point)
        {
            return _lowerCorner.x < point.x &&
                   point.x < _upperCorner.x &&
                   _lowerCorner.y < point.z &&
                   point.z < _upperCorner.y;
        }

        public void Enter(Unit unit)
        {
            _containedUnits[unit.Faction].Add(unit);
        }

        public void Leave(Unit unit)
        {
            _containedUnits[unit.Faction].Remove(unit);
        }

        public bool ContainsAny(Faction faction)
        {
            return _containedUnits[faction].Count > 0;
        }

        public List<Unit> GetUnits(Faction faction)
        {
            return _containedUnits[faction];
        }
    }
}

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
        private readonly Vector2 _size;
        private Vector2 _center;

        private List<List<List<Bucket>>> _adjacentBuckets;

        private readonly Dictionary<Faction, List<Unit>> _containedUnits;

        public Bucket(Vector2 lowerCorner, Vector2 size)
        {
            _lowerCorner = lowerCorner;
            _upperCorner = lowerCorner + size;
            _size = size;
            _center = lowerCorner + size / 2;

            _containedUnits = new Dictionary<Faction, List<Unit>>();
            foreach (var faction in (Faction[])Enum.GetValues(typeof(Faction)))
            {
                _containedUnits.Add(faction, new List<Unit>());
            }
        }

        public void SetAdjacentBuckets(
            Bucket lowerXLowerYBucket,   Bucket lowerXSameYBucket,   Bucket lowerXGreaterYBucket,
            Bucket sameXLowerYBucket,                                Bucket sameXGreaterYBucket, 
            Bucket greaterXlowerYBucket, Bucket greaterXSameYBucket, Bucket greaterXGreaterYBucket)
        {
            _adjacentBuckets = new List<List<List<Bucket>>>();
            for (var x = 0; x < 3; x++)
            {
                _adjacentBuckets.Add(new List<List<Bucket>>());
                for (var y = 0; y < 3; y++)
                {
                    _adjacentBuckets[x].Add(new List<Bucket>());
                }
            }

            // corner buckets are only added cor corners
            _adjacentBuckets[0][0].Add(lowerXLowerYBucket);
            _adjacentBuckets[2][0].Add(greaterXlowerYBucket);
            _adjacentBuckets[0][2].Add(lowerXGreaterYBucket);
            _adjacentBuckets[2][2].Add(greaterXGreaterYBucket);

            // edge buckets are added for whole edge
            for (var i = 0; i < 3; i++)
            {
                _adjacentBuckets[0][i].Add(lowerXSameYBucket);
                _adjacentBuckets[2][i].Add(greaterXSameYBucket);
                _adjacentBuckets[i][0].Add(sameXLowerYBucket);
                _adjacentBuckets[i][2].Add(sameXGreaterYBucket);
            }

            foreach (var x in _adjacentBuckets)
            {
                foreach (var y in x)
                {
                    y.RemoveAll(b => b == null);
                }
            }
        }

        public bool Contains(Vector3 point)
        {
            return _lowerCorner.x < point.x &&
                   point.x < _upperCorner.x &&
                   _lowerCorner.y < point.z &&
                   point.z < _upperCorner.y;
        }

        public List<Bucket> Candidates(Vector3 point)
        {
            var xThird = Mathf.RoundToInt(Mathf.Clamp((point.x - _lowerCorner.x) * 3 / _size.x, 0, 2));
            var yThird = Mathf.RoundToInt(Mathf.Clamp((point.z - _lowerCorner.y) * 3 / _size.y, 0, 2));

            return _adjacentBuckets[xThird][yThird];
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
    }
}

using System;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    public class Situation
    {
        public SerializableUnit[] Units;
        public UnitStats[] Stats;

        [Serializable]
        public class SerializableUnit
        {
            public Unit.Class Class;
            public Unit.Faction Faction;
            public Vector2 Position;
        }

        [Serializable]
        public class UnitStats
        {
            
        }
    }
}

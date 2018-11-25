using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts
{
    public class Unit : MonoBehaviour
    {
        public int MaxHealth;
        public double MaxMoral;
        public double Accuracy;
        public double Damage; // reduziert evasion
        public double Evasion;
        public double Range;
        public double MoralDamage;
        public double Spacing;
        public double AreaDamage;

        private NavMeshAgent _agent;
        private int _health;
        private double _moral;

        public enum Class
        {
            Infantry,
            Cavalry,
            Artillery
        }

        public enum Faction
        {
            Prussia,
            Austria
        }
    }
}

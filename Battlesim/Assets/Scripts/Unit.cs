using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts
{
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
    
    public class Unit : MonoBehaviour
    {
        public Class Class;
        public Faction Faction;

        /*
         * The overall count of soldiers in this unit an the unit's moral value.
         * If the count is reduced to zero, the unit is destroyed.
         * Reducing the count also reduces the damage dealt to enemies.
         * If the moral falls to zero, the unit surrenders.
         */
        public int InitialCount;
        public double InitialMorale;

        /*
         * Physical damage stats:
         * The lower the unit's health, the higher the chance a soldier is killed.
         * Damage reduces this threshold over time.
         * Accuracy is the offensive counterpart: Higher accuracy leads to higher chance of killing a soldier.
         * The range states how close a unit has to get to the enemy to start doing damage.
         */
        public double Health;
        public double Damage;
        public double Accuracy;
        public double Range;

        /*
         * Psychological damage stats:
         * How much of the enemy's moral is depleted when attacking.
         */
        public double MoraleDamage;

        /*
         * Area damage stats:
         * Artillery has a higher AreaDamage value, meaning it can harm multiple soldiers in one attack.
         * This is modified by the defending unit's Spacing value: If higher, the soldiers are spread out more and less prone to area damage.
         */
        public double AreaDamage;
        public double Spacing;

        private NavMeshAgent _agent;
        private Animator _animator;
        private int _health;
        private double _moral;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();

            _agent.updatePosition = false;
        }

        private void Update()
        {
            var moving = _agent.remainingDistance > _agent.stoppingDistance;
            _animator.SetBool("Moving", moving);
            if (!moving)
            {
                _agent.velocity = Vector3.zero;
            }
            else
            {
                _agent.Move(transform.forward * Time.deltaTime * _agent.velocity.magnitude);
            }
        }

        private void OnAnimatorMove()
        {
            if (_agent != null)
                transform.position = _agent.nextPosition;
        }
    }
}

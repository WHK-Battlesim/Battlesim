using UnityEngine;
using UnityEngine.AI;

namespace Assets.Scripts
{
    [RequireComponent (typeof (NavMeshAgent))]
    [RequireComponent (typeof (Animator))]
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
        private Animator _animator;
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

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();

            _agent.updatePosition = false;
        }

        private void Update()
        {
            var moving = _agent.remainingDistance > _agent.radius;
            _animator.SetBool("Moving", _agent.remainingDistance > _agent.radius);
            if (!moving)
            {
                _agent.velocity = Vector3.zero;
            }
        }

        private void OnAnimatorMove()
        {
            if(_agent!=null)
                transform.position = _agent.nextPosition;
        }
    }
}

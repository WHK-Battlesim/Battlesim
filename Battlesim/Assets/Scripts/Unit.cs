using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour
{
    public int MaxHealth;
    public double MaxMoral;

    private NavMeshAgent _agent;
    private int _health;
    private double _moral;
}

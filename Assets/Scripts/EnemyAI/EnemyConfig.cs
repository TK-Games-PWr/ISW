using Sirenix.OdinInspector;
using UnityEngine;
using EnemySystem;
using UnityEngine.AI;

public enum AgentState
{
    Patrol,
    Alert,
    Combat
}

public enum AlertLevel
{
    None,
    Low,
    Medium,
    High,
    Extreme
}

[CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "AI/Enemy Configuration")]
public class EnemyConfig : ScriptableObject
{
    [Title("General")]
    public float maxHealth = 100f;

    [Title("Movement Settings")]
    public MovementConfig movement;

    [Title("Sensors Settings")]
    public SensorConfig sensors;

    [Title("Alert Settings")]
    public AlertConfig alert;

    [Title("Combat Settings")]
    [SerializeReference]
    public CombatConfig combat = new NormalCombatConfig();
}

[System.Serializable]
public class MovementConfig
{
    [Header("Speed Settings")]
    public float basePlayerSpeed = 4f;

    public float patrolSpeedMultiplier = 1f;
    public float baseAngularSpeed = 120f;
    public float combatAngularSpeed = 360f;

    [Header("Patrol Settings")]
    public float waitTimeAtWaypoint = 2f;
    
    [Header("Agent Parameters")]
    public float agentStopDistance = 1f;
}

[System.Serializable]
public class SensorConfig
{
    [Header("Targeting")]
    public string playerTag = "Player";

    public LayerMask enemyLayer;

    [Header("Vision Settings")]
    public LayerMask sightObstaclesMask;

    public float eyeLevel = 1.7f;
    public float horizontalFOV = 100f;
    public float verticalFOV = 40f;

    [Tooltip("Vertical FOV used when in Alert or Combat state. Lets enemies spot elevated targets once aware.")]
    public float alertedVerticalFOV = 120f;

    public float maxSightDistance = 40f;

    [Header("Hearing Settings")]
    public float hearingRadius = 30f;
}

[System.Serializable]
public class AlertConfig
{
    public float alertSensitivity = 5f;

    public float timeToLoseAlertLevel = 3f;

    [Tooltip("Delay before agents starts shooting in seconds")]
    public float fightDelay = 1f;

    [Tooltip("Time before TM starts decreasing")]
    public float triggerMultiplierTimeout = 2f;

    [Tooltip("Time after which enemy ends combat and returns to patrol")]
    public float endCombatTimeout = 10f;
}

[System.Serializable]
public abstract class CombatConfig
{
    public float weaponRange = 25f;
    public float optimalCombatDistancePct = 0.7f;
    public float combatSpeedMultiplier = 1.3f;
    public float retreatSpeedMultiplier = 0.3f;
    [Tooltip("Extra delay added between shots for semi-automatic weapons to simulate an AI's trigger finger.")]
    public float singleShotDelay = 0.6f;

    public abstract EnemyCombat CreateCombatInstance(AICore brain, Transform transform, NavMeshAgent agent, EnemySensors sensors, EnemyMovement movement, EnemyResources resources);
}

[System.Serializable]
public class NormalCombatConfig : CombatConfig
{
    public override EnemyCombat CreateCombatInstance(AICore brain, Transform transform, NavMeshAgent agent, EnemySensors sensors, EnemyMovement movement, EnemyResources resources)
    {
        return new EnemyCombat(brain, transform, agent, sensors, movement, resources, this);
    }
}

[System.Serializable]
public class RambenemyCombatConfig : CombatConfig
{
    public override EnemyCombat CreateCombatInstance(AICore brain, Transform transform, NavMeshAgent agent, EnemySensors sensors, EnemyMovement movement, EnemyResources resources)
    {
        return new RamberCombat(brain, transform, agent, sensors, movement, resources, this);
    }
}

[System.Serializable]
public class CoverGuyCombatConfig : CombatConfig
{
    [Header("Cover Guy Specific")]
    public float hidingSpeedMultiplier = 2f;

    public override EnemyCombat CreateCombatInstance(AICore brain, Transform transform, NavMeshAgent agent, EnemySensors sensors, EnemyMovement movement, EnemyResources resources)
    {
        return new CoverGuyCombat(brain, transform, agent, sensors, movement, resources, this);
    }
}
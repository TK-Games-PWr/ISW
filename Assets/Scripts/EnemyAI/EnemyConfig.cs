using Sirenix.OdinInspector;
using UnityEngine;

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
    public CombatConfig combat;
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
public class CombatConfig
{
}
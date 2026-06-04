using System.Collections.Generic;
using UnityEngine;
using EnemySystem;
using static EnemySystem.EnemyBrain;

public class AlertNotificator : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("The player's transform to calculate relative angle (usually the camera or player body).")]
    [SerializeField] Transform playerTransform;
    [Tooltip("The prefab that has the AlertIndicatorUI script attached.")]
    [SerializeField] AlertIndicatorUI indicatorPrefab;
    [Tooltip("The UI container where these indicators will be spawned.")]
    [SerializeField] Transform container;

    private Dictionary<EnemyBrain, AlertIndicatorUI> activeIndicators = new Dictionary<EnemyBrain, AlertIndicatorUI>();

    private void OnEnable()
    {
        OnAlertChanged += HandleAlertChanged;
        EnemyResources.OnEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        OnAlertChanged -= HandleAlertChanged;
        EnemyResources.OnEnemyDied -= HandleEnemyDied;
    }

    private void Update()
    {
        if (playerTransform == null || activeIndicators.Count == 0) return;

        foreach (var kvp in activeIndicators)
        {
            EnemyBrain enemy = kvp.Key;
            AlertIndicatorUI indicator = kvp.Value;

            if (enemy != null && indicator != null)
            {
                Vector3 directionToEnemy = enemy.transform.position - playerTransform.position;
                directionToEnemy.y = 0;

                Vector3 playerForward = playerTransform.forward;
                playerForward.y = 0;

                float angle = Vector3.SignedAngle(playerForward, directionToEnemy, Vector3.up);

                indicator.rectTransform.localEulerAngles = new Vector3(0, 0, -angle);
            }
        }
    }

    private void HandleAlertChanged(EnemyBrain enemy, float value, AlertLevel alertLevel)
    {
        if (alertLevel == AlertLevel.None && value <= 0f)
        {
            RemoveIndicator(enemy);
            return;
        }

        if (!activeIndicators.TryGetValue(enemy, out AlertIndicatorUI indicator))
        {
            indicator = Instantiate(indicatorPrefab, container);
            activeIndicators[enemy] = indicator;
        }

        indicator.SetAlertProgress(value, alertLevel);
    }

    private void HandleEnemyDied(EnemyResources enemyResources)
    {
        EnemyBrain enemy = enemyResources.GetComponent<EnemyBrain>();
        if (enemy != null)
        {
            RemoveIndicator(enemy);
        }
    }

    private void RemoveIndicator(EnemyBrain enemy)
    {
        if (activeIndicators.TryGetValue(enemy, out AlertIndicatorUI indicator))
        {
            if (indicator != null)
            {
                Destroy(indicator.gameObject);
            }
            activeIndicators.Remove(enemy);
        }
    }

}
using System.Collections.Generic;
using UnityEngine;
using EnemySystem;
using static EnemySystem.AICore;

public class AlertNotificator : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("The player's transform to calculate relative angle (usually the camera or player body).")]
    [SerializeField] Transform playerTransform;
    [Tooltip("The prefab that has the AlertIndicatorUI script attached.")]
    [SerializeField] AlertIndicatorUI indicatorPrefab;
    [Tooltip("The UI container where these indicators will be spawned.")]
    [SerializeField] Transform container;

    private Dictionary<AICore, AlertIndicatorUI> activeIndicators = new Dictionary<AICore, AlertIndicatorUI>();

    private void OnEnable()
    {
        OnAlertChanged += HandleAlertChanged;
        EnemyHealth.OnEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        OnAlertChanged -= HandleAlertChanged;
        EnemyHealth.OnEnemyDied -= HandleEnemyDied;
    }

    private void Update()
    {
        if (playerTransform == null || activeIndicators.Count == 0) return;

        foreach (var kvp in activeIndicators)
        {
            AICore enemy = kvp.Key;
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

    private void HandleAlertChanged(AICore enemy, float value, AlertLevel alertLevel)
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

    private void HandleEnemyDied(EnemyHealth enemyHealth)
    {
        AICore enemy = enemyHealth.GetComponent<AICore>();
        if (enemy != null)
        {
            RemoveIndicator(enemy);
        }
    }

    private void RemoveIndicator(AICore enemy)
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
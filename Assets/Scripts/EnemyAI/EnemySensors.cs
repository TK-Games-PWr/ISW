using UnityEngine;
using TK_Shared._3DPlayerMovement;

namespace EnemySystem
{
    public class EnemySensors : MonoBehaviour
    {
        [Header("Targeting")]
        [SerializeField] string playerTag = "Player";
        [SerializeField] LayerMask enemyLayer;

        [Header("Vision Settings")]
        [SerializeField] LayerMask sightObstaclesMask;
        [SerializeField] float eyeLevel = 1.7f;
        [SerializeField] float fieldOfViewAngle = 120f;
        [SerializeField] float maxSightDistance = 40f;

        [Header("Hearing Settings")]
        [SerializeField] float hearingRadius = 30f;

        internal Transform PlayerTransform { get; private set; }
        internal Vector3 LastKnownPosition { get; private set; }

        private float playerEyeLevel => PlayerTransform != null ? PlayerTransform.GetComponent<PlayerActionsController>().eyeLevel : 1.7f;

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null) PlayerTransform = player.transform;
        }

        internal bool HasLineOfSight()
        {
            if (PlayerTransform == null) return false;

            float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
            if (distanceToPlayer > maxSightDistance) return false;

            Vector3 rayStartOrigin = transform.position + Vector3.up * eyeLevel;
            Vector3 targetPosition = PlayerTransform.position + Vector3.up * playerEyeLevel;
            Vector3 directionToPlayer = (targetPosition - rayStartOrigin).normalized;

            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
            if (angleToPlayer > fieldOfViewAngle / 2f)
            {
                return false; // Player is outside the vision cone
            }

            if (Physics.Raycast(rayStartOrigin, directionToPlayer, out RaycastHit hit, maxSightDistance, sightObstaclesMask))
            {
                if (hit.collider.CompareTag(playerTag)) return true;
            }
            return false;
        }

        internal void UpdateLastKnownPosition()
        {
            if (PlayerTransform != null) LastKnownPosition = PlayerTransform.position;
        }

        internal void AlertNearbyEnemies()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, hearingRadius, enemyLayer);
            foreach (var hitCollider in hitColliders)
            {
                AICore nearbyEnemy = hitCollider.GetComponent<AICore>();
                if (nearbyEnemy != null && nearbyEnemy != GetComponent<AICore>() && nearbyEnemy.currentState != AICore.AIState.Combat)
                {
                    nearbyEnemy.triggerMultiplier = 2f;
                    nearbyEnemy.DetermineAlertLevel();
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (PlayerTransform == null) return;

            Vector3 rayStartOrigin = transform.position + Vector3.up * eyeLevel;
            Vector3 targetPosition = PlayerTransform.position + Vector3.up * playerEyeLevel;
            Vector3 directionToPlayer = (targetPosition - rayStartOrigin).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

            Gizmos.color = Color.black;

            if (Physics.Raycast(rayStartOrigin, directionToPlayer, out RaycastHit hit, maxSightDistance, sightObstaclesMask))
            {
                DrawVisibilityRaycast(rayStartOrigin, hit, angleToPlayer);
            }
            else
            {
                // Didn't hit anything
                Gizmos.DrawLine(rayStartOrigin, rayStartOrigin + directionToPlayer * maxSightDistance);
            }
        }

        private void DrawVisibilityRaycast(Vector3 rayStartOrigin, RaycastHit hit, float angleToPlayer)
        {
            if (hit.collider.CompareTag(playerTag))
            {
                if (angleToPlayer > fieldOfViewAngle / 2f)
                {
                    Gizmos.color = Color.blue; // Blue: Player behind field of view
                }
                else
                {
                    Gizmos.color = Color.green; // Green: Sees Player
                }
                Gizmos.DrawLine(rayStartOrigin, hit.point);
                Gizmos.DrawSphere(hit.point, 0.1f);
            }
            else
            {
                Gizmos.color = Color.red; // Red: Hit a wall/obstacle
                Gizmos.DrawLine(rayStartOrigin, hit.point);
                Gizmos.DrawSphere(hit.point, 0.1f);
            }
        }
#endif
    }
}
using Sirenix.OdinInspector;
using UnityEngine;
using TK_Shared._3DPlayerMovement;

namespace EnemySystem
{
    public class EnemySensorsOld : MonoBehaviour
    {
        [InfoBox("This script is deprecated. Remove it after migrating settings to overrides in AICore and/or EnemyConfig Scriptable Object.", InfoMessageType.Warning)]
        
        [Header("Targeting")]
        [SerializeField] internal string playerTag = "Player";
        [SerializeField] LayerMask enemyLayer;

        [Header("Vision Settings")]
        [SerializeField] internal LayerMask sightObstaclesMask;
        [SerializeField] float eyeLevel = 1.7f;
        [SerializeField] float horizontalFOV = 100f;
        [SerializeField] float verticalFOV = 40f;
        [Tooltip("Vertical FOV used when in Alert or Combat state. Lets enemies spot elevated targets once aware.")]
        [SerializeField] float alertedVerticalFOV = 120f;
        [SerializeField] float maxSightDistance = 40f;

        [Header("Hearing Settings")]
        [SerializeField] float hearingRadius = 30f;

        internal Transform PlayerTransform { get; private set; }
        internal Vector3 LastKnownPosition { get; private set; }
        internal PlayerResources PlayerResources { get; private set; }

        AICore _brain;

        float playerEyeLevel => PlayerActionsController.EyeLevel;

        float CurrentVerticalFOV =>
            (_brain != null && _brain.currentAgentState != AgentState.Patrol) ? alertedVerticalFOV : verticalFOV;

        void Start()
        {
            _brain = GetComponent<AICore>();
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                PlayerTransform = player.transform;
                PlayerResources = player.GetComponent<PlayerResources>();
            }
        }

        internal bool HasLineOfSight()
        {
            if (PlayerTransform == null) return false;

            float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
            if (distanceToPlayer > maxSightDistance) return false;

            Vector3 rayStartOrigin = transform.position + Vector3.up * eyeLevel;
            Vector3 targetPosition = PlayerTransform.position + Vector3.up * playerEyeLevel;
            Vector3 directionToPlayer = (targetPosition - rayStartOrigin).normalized;

            Vector3 localDirToPlayer = transform.InverseTransformDirection(directionToPlayer);

            float horizontalAngle = Mathf.Abs(Mathf.Atan2(localDirToPlayer.x, localDirToPlayer.z) * Mathf.Rad2Deg);

            float flatDistance = Mathf.Sqrt(localDirToPlayer.x * localDirToPlayer.x + localDirToPlayer.z * localDirToPlayer.z);
            float verticalAngle = Mathf.Abs(Mathf.Atan2(localDirToPlayer.y, flatDistance) * Mathf.Rad2Deg);

            if (horizontalAngle > horizontalFOV / 2f || verticalAngle > CurrentVerticalFOV / 2f)
            {
                return false;
            }

            if (Physics.Raycast(rayStartOrigin, directionToPlayer, out RaycastHit hit, maxSightDistance, sightObstaclesMask))
            {
                if (hit.collider.CompareTag(playerTag)) return true;
            }

            return false;
        }

        internal void UpdateLastKnownPosition()
        {
            if (PlayerTransform != null) UpdateLastKnownPosition(PlayerTransform.position);
        }
        
        internal void UpdateLastKnownPosition(Vector3 position)
        {
            LastKnownPosition = position;
        }

        internal void AlertNearbyEnemies()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, hearingRadius, enemyLayer);
            foreach (var hitCollider in hitColliders)
            {
                AICore nearbyEnemy = hitCollider.GetComponent<AICore>();
                if (nearbyEnemy != null && nearbyEnemy != GetComponent<AICore>() && nearbyEnemy.currentAgentState != AgentState.Combat)
                {
                    nearbyEnemy.triggerMultiplier = 2f;
                    // nearbyEnemy.DetermineAlertLevel();
                }
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (PlayerTransform == null || _brain.IsDead) return;

            Vector3 rayStartOrigin = transform.position + Vector3.up * eyeLevel;
            Vector3 targetPosition = PlayerTransform.position + Vector3.up * playerEyeLevel;
            Vector3 directionToPlayer = (targetPosition - rayStartOrigin).normalized;

            Vector3 localDirToPlayer = transform.InverseTransformDirection(directionToPlayer);

            float horizontalAngle = Mathf.Abs(Mathf.Atan2(localDirToPlayer.x, localDirToPlayer.z) * Mathf.Rad2Deg);
            float flatDistance = Mathf.Sqrt(localDirToPlayer.x * localDirToPlayer.x + localDirToPlayer.z * localDirToPlayer.z);
            float verticalAngle = Mathf.Abs(Mathf.Atan2(localDirToPlayer.y, flatDistance) * Mathf.Rad2Deg);

            Gizmos.color = Color.black;

            if (Physics.Raycast(rayStartOrigin, directionToPlayer, out RaycastHit hit, maxSightDistance, sightObstaclesMask))
            {
                DrawVisibilityRaycast(rayStartOrigin, hit, horizontalAngle, verticalAngle);
            }
            else
            {
                // Didn't hit anything
                Gizmos.DrawLine(rayStartOrigin, rayStartOrigin + directionToPlayer * maxSightDistance);
            }
        }

        void OnDrawGizmosSelected()
        {
            Vector3 rayStartOrigin = transform.position + Vector3.up * eyeLevel;

            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);

            float halfV = verticalFOV / 2f;
            float halfH = horizontalFOV / 2f;

            Vector3 topLeftDir = transform.rotation * Quaternion.Euler(-halfV, -halfH, 0) * Vector3.forward;
            Vector3 topRightDir = transform.rotation * Quaternion.Euler(-halfV, halfH, 0) * Vector3.forward;
            Vector3 bottomLeftDir = transform.rotation * Quaternion.Euler(halfV, -halfH, 0) * Vector3.forward;
            Vector3 bottomRightDir = transform.rotation * Quaternion.Euler(halfV, halfH, 0) * Vector3.forward;

            Vector3 topLeft = rayStartOrigin + topLeftDir * maxSightDistance;
            Vector3 topRight = rayStartOrigin + topRightDir * maxSightDistance;
            Vector3 bottomLeft = rayStartOrigin + bottomLeftDir * maxSightDistance;
            Vector3 bottomRight = rayStartOrigin + bottomRightDir * maxSightDistance;

            // edges
            Gizmos.DrawLine(rayStartOrigin, topLeft);
            Gizmos.DrawLine(rayStartOrigin, topRight);
            Gizmos.DrawLine(rayStartOrigin, bottomLeft);
            Gizmos.DrawLine(rayStartOrigin, bottomRight);

            // far plane rectangle
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);

            // visibility shadow
            UnityEditor.Handles.color = new Color(0f, 0f, 0f, 0.1f);

            Vector3 flatForward = transform.forward;
            flatForward.y = 0;

            if (flatForward.sqrMagnitude > 0.001f)
            {
                flatForward.Normalize();

                Vector3 groundPos = transform.position;

                Vector3 leftBoundary = Quaternion.Euler(0, -horizontalFOV / 2f, 0) * flatForward;

                UnityEditor.Handles.DrawSolidArc(groundPos, Vector3.up, leftBoundary, horizontalFOV, maxSightDistance);
            }
        }

        void DrawVisibilityRaycast(Vector3 rayStartOrigin, RaycastHit hit, float horizontalAngle, float verticalAngle)
        {
            if (hit.collider.CompareTag(playerTag))
            {
                if (horizontalAngle > horizontalFOV / 2f || verticalAngle > CurrentVerticalFOV / 2f)
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
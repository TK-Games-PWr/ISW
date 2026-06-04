using UnityEngine;
using TK_Shared._3DPlayerMovement;

namespace EnemySystem
{
    public class EnemySensors
    {
        internal Transform PlayerTransform { get; private set; }
        internal Vector3 LastKnownPosition { get; private set; }
        internal PlayerResources PlayerResources { get; private set; }
        internal bool IsPlayerVisible { get; private set; }

        SensorConfig _config;
        Transform _transform;
        AICore _brain;

        float playerEyeLevel => PlayerActionsController.EyeLevel;

        float CurrentVerticalFOV =>
            (_brain != null && _brain.currentAgentState != AgentState.Patrol) ? _config.alertedVerticalFOV : _config.verticalFOV;

        public EnemySensors(AICore brain, Transform transform, SensorConfig config)
        {
            _brain = brain;
            _transform = transform;
            _config = config;

            GameObject player = GameObject.FindGameObjectWithTag(_config.playerTag);
            if (player != null)
            {
                PlayerTransform = player.transform;
                PlayerResources = player.GetComponent<PlayerResources>();
            }
        }
        
        public void Tick()
        {
            IsPlayerVisible = CheckLineOfSight();
        }

        bool CheckLineOfSight()
        {
            if (PlayerTransform == null) return false;
            
            float distanceToPlayer = Vector3.Distance(_transform.position, PlayerTransform.position);
            if (distanceToPlayer > _config.maxSightDistance) return false;

            Vector3 rayStartOrigin = _transform.position + Vector3.up * _config.eyeLevel;
            Vector3 targetPosition = PlayerTransform.position + Vector3.up * playerEyeLevel;
            Vector3 directionToPlayer = (targetPosition - rayStartOrigin).normalized;

            Vector3 localDirToPlayer = _transform.InverseTransformDirection(directionToPlayer);

            float horizontalAngle = Mathf.Abs(Mathf.Atan2(localDirToPlayer.x, localDirToPlayer.z) * Mathf.Rad2Deg);

            float flatDistance = Mathf.Sqrt(localDirToPlayer.x * localDirToPlayer.x + localDirToPlayer.z * localDirToPlayer.z);
            float verticalAngle = Mathf.Abs(Mathf.Atan2(localDirToPlayer.y, flatDistance) * Mathf.Rad2Deg);

            if (horizontalAngle > _config.horizontalFOV / 2f || verticalAngle > CurrentVerticalFOV / 2f)
            {
                return false;
            }

            if (Physics.Raycast(rayStartOrigin, directionToPlayer, out RaycastHit hit, _config.maxSightDistance, _config.sightObstaclesMask))
            {
                if (hit.collider.CompareTag(_config.playerTag)) return true;
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
            Collider[] hitColliders = Physics.OverlapSphere(_transform.position, _config.hearingRadius, _config.enemyLayer);
            foreach (var hitCollider in hitColliders)
            {
                AICore nearbyEnemy = hitCollider.GetComponent<AICore>();
                if (nearbyEnemy != null && nearbyEnemy != _brain && nearbyEnemy.currentAgentState != AgentState.Combat)
                {
                    nearbyEnemy.AlertSystem.TriggerMultiplier = 2f;
                    nearbyEnemy.AlertSystem.DetermineAlertLevel();
                }
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (PlayerTransform == null || _brain.IsDead) return;

            Vector3 rayStartOrigin = _transform.position + Vector3.up * _config.eyeLevel;
            Vector3 targetPosition = PlayerTransform.position + Vector3.up * playerEyeLevel;
            Vector3 directionToPlayer = (targetPosition - rayStartOrigin).normalized;

            Vector3 localDirToPlayer = _transform.InverseTransformDirection(directionToPlayer);

            float horizontalAngle = Mathf.Abs(Mathf.Atan2(localDirToPlayer.x, localDirToPlayer.z) * Mathf.Rad2Deg);
            float flatDistance = Mathf.Sqrt(localDirToPlayer.x * localDirToPlayer.x + localDirToPlayer.z * localDirToPlayer.z);
            float verticalAngle = Mathf.Abs(Mathf.Atan2(localDirToPlayer.y, flatDistance) * Mathf.Rad2Deg);

            Gizmos.color = Color.black;

            if (Physics.Raycast(rayStartOrigin, directionToPlayer, out RaycastHit hit, _config.maxSightDistance, _config.sightObstaclesMask))
            {
                DrawVisibilityRaycast(rayStartOrigin, hit, horizontalAngle, verticalAngle);
            }
            else
            {
                // Didn't hit anything
                Gizmos.DrawLine(rayStartOrigin, rayStartOrigin + directionToPlayer * _config.maxSightDistance);
            }
        }

        void OnDrawGizmosSelected()
        {
            Vector3 rayStartOrigin = _transform.position + Vector3.up * _config.eyeLevel;

            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);

            float halfV = _config.verticalFOV / 2f;
            float halfH = _config.horizontalFOV / 2f;

            Vector3 topLeftDir = _transform.rotation * Quaternion.Euler(-halfV, -halfH, 0) * Vector3.forward;
            Vector3 topRightDir = _transform.rotation * Quaternion.Euler(-halfV, halfH, 0) * Vector3.forward;
            Vector3 bottomLeftDir = _transform.rotation * Quaternion.Euler(halfV, -halfH, 0) * Vector3.forward;
            Vector3 bottomRightDir = _transform.rotation * Quaternion.Euler(halfV, halfH, 0) * Vector3.forward;

            Vector3 topLeft = rayStartOrigin + topLeftDir * _config.maxSightDistance;
            Vector3 topRight = rayStartOrigin + topRightDir * _config.maxSightDistance;
            Vector3 bottomLeft = rayStartOrigin + bottomLeftDir * _config.maxSightDistance;
            Vector3 bottomRight = rayStartOrigin + bottomRightDir * _config.maxSightDistance;

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

            Vector3 flatForward = _transform.forward;
            flatForward.y = 0;

            if (flatForward.sqrMagnitude > 0.001f)
            {
                flatForward.Normalize();

                Vector3 groundPos = _transform.position;

                Vector3 leftBoundary = Quaternion.Euler(0, -_config.horizontalFOV / 2f, 0) * flatForward;

                UnityEditor.Handles.DrawSolidArc(groundPos, Vector3.up, leftBoundary, _config.horizontalFOV, _config.maxSightDistance);
            }
        }

        void DrawVisibilityRaycast(Vector3 rayStartOrigin, RaycastHit hit, float horizontalAngle, float verticalAngle)
        {
            if (hit.collider.CompareTag(_config.playerTag))
            {
                if (horizontalAngle > _config.horizontalFOV / 2f || verticalAngle > CurrentVerticalFOV / 2f)
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
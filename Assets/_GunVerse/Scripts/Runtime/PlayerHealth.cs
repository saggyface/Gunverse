using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace GunVerse
{
    public class PlayerHealth : NetworkBehaviour
    {
        public const float MAX_HEALTH = 100f;

        private const float RESPAWN_DELAY = 3f;
        private const float SPAWN_PROTECTION_DURATION = 1f;

        private readonly NetworkVariable<float> health = new NetworkVariable<float>(
            MAX_HEALTH,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> isDead = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> spawnProtected = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private Coroutine respawnCoroutine;
        private Coroutine spawnProtectionCoroutine;
        private GameObject shieldVisual;
        private TestTargetScore testTargetScore;

        public float CurrentHealth => health.Value;
        public bool IsDead => isDead.Value;
        public bool IsSpawnProtected => spawnProtected.Value;
        public bool IsTestTarget => testTargetScore != null;
        public ulong DamageTargetId => IsTestTarget ? testTargetScore.ScoreId : OwnerClientId;

        public event Action<float, float> HealthChanged;
        public event Action<bool> DeathStateChanged;

        public override void OnNetworkSpawn()
        {
            testTargetScore = GetComponent<TestTargetScore>();
            health.OnValueChanged += OnHealthValueChanged;
            isDead.OnValueChanged += OnDeathStateChanged;
            spawnProtected.OnValueChanged += OnSpawnProtectionChanged;

            if (IsServer)
            {
                health.Value = MAX_HEALTH;
                isDead.Value = false;
                StartSpawnProtection();
            }

            if (IsClient)
            {
                CreateShieldVisual();
            }
        }

        public override void OnNetworkDespawn()
        {
            health.OnValueChanged -= OnHealthValueChanged;
            isDead.OnValueChanged -= OnDeathStateChanged;
            spawnProtected.OnValueChanged -= OnSpawnProtectionChanged;

            if (respawnCoroutine != null)
            {
                StopCoroutine(respawnCoroutine);
                respawnCoroutine = null;
            }

            StopSpawnProtectionCoroutine();
        }

        public void ApplyDamage(float amount, ulong attackerClientId, HitCue hitCue = HitCue.Generic)
        {
            if (!IsServer || isDead.Value || spawnProtected.Value || !GameModeController.IsGameplayActive || amount <= 0f)
            {
                return;
            }

            if (!IsTestTarget && !GameModeController.IsDamageAllowed(attackerClientId, OwnerClientId))
            {
                return;
            }

            if (!IsAttackerConnected(attackerClientId))
            {
                return;
            }

            health.Value = Mathf.Max(0f, health.Value - amount);
            bool isKill = health.Value <= 0f;

            if (IsTestTarget)
            {
                ClientRpcParams attackerParams = CreateClientRpcParams(attackerClientId);
                ShowHitMarkerClientRpc(isKill, hitCue, attackerParams);
            }
            else if (attackerClientId != OwnerClientId)
            {
                ClientRpcParams attackerParams = CreateClientRpcParams(attackerClientId);
                ShowHitMarkerClientRpc(isKill, hitCue, attackerParams);

                ClientRpcParams victimParams = CreateClientRpcParams(OwnerClientId);
                ShowDamageFeedbackClientRpc(GetPlayerPosition(attackerClientId), victimParams);
            }

            if (isKill)
            {
                Die(attackerClientId);
            }
        }

        private static ClientRpcParams CreateClientRpcParams(ulong clientId)
        {
            return new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            };
        }

        private Vector3 GetPlayerPosition(ulong clientId)
        {
            if (NetworkManager == null || NetworkManager.SpawnManager == null)
            {
                return transform.position;
            }

            NetworkObject playerObject = NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
            return playerObject != null ? playerObject.transform.position : transform.position;
        }

        private void Die(ulong attackerClientId)
        {
            if (isDead.Value)
            {
                return;
            }

            EndSpawnProtection();
            isDead.Value = true;
            PlayDeathSoundClientRpc();

            if (IsTestTarget)
            {
                GameModeController.ReportElimination(DamageTargetId, attackerClientId);
            }
            else
            {
                GameModeController.ReportElimination(OwnerClientId, attackerClientId);
                GameModeController.Instance?.CancelGunGameSwap(OwnerClientId);
                GunItem.HandlePlayerRemoved(OwnerClientId);
            }

            respawnCoroutine = StartCoroutine(RespawnRoutine());
        }

        public void RecoverFromFall()
        {
            if (!IsServer || isDead.Value || health.Value <= 0f)
            {
                return;
            }

            PlayerSpawnManager.TryGetSpawnPose(OwnerClientId, out Vector3 spawnPosition, out Quaternion spawnRotation);
            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { OwnerClientId }
                }
            };
            RespawnClientRpc(spawnPosition, spawnRotation, rpcParams);
            StartSpawnProtection();
        }

        public void CancelRespawnServer()
        {
            if (!IsServer)
            {
                return;
            }

            if (respawnCoroutine != null)
            {
                StopCoroutine(respawnCoroutine);
                respawnCoroutine = null;
            }
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(RESPAWN_DELAY);

            if (!GameModeController.IsGameplayActive)
            {
                respawnCoroutine = null;
                yield break;
            }

            if (IsTestTarget)
            {
                ulong targetId = DamageTargetId;
                Vector3 targetPosition = PlayerSpawnPoint.GetSpawnPosition(targetId);
                Quaternion targetRotation = PlayerSpawnPoint.GetSpawnRotation(targetId);
                transform.SetPositionAndRotation(targetPosition, targetRotation);
                health.Value = MAX_HEALTH;
                isDead.Value = false;
                StartSpawnProtection();
                respawnCoroutine = null;
                yield break;
            }

            PlayerSpawnManager.TryGetSpawnPose(OwnerClientId, out Vector3 spawnPosition, out Quaternion spawnRotation);
            ClientRpcParams rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { OwnerClientId }
                }
            };
            RespawnClientRpc(spawnPosition, spawnRotation, rpcParams);

            health.Value = MAX_HEALTH;
            isDead.Value = false;
            StartSpawnProtection();
            respawnCoroutine = null;
        }

        public void EndSpawnProtection()
        {
            if (!IsServer)
            {
                return;
            }

            StopSpawnProtectionCoroutine();
            if (spawnProtected.Value)
            {
                spawnProtected.Value = false;
            }
        }

        private void StartSpawnProtection()
        {
            if (!IsServer)
            {
                return;
            }

            StopSpawnProtectionCoroutine();
            spawnProtected.Value = true;
            spawnProtectionCoroutine = StartCoroutine(SpawnProtectionRoutine());
        }

        private IEnumerator SpawnProtectionRoutine()
        {
            yield return new WaitForSeconds(SPAWN_PROTECTION_DURATION);
            spawnProtectionCoroutine = null;
            spawnProtected.Value = false;
        }

        private void StopSpawnProtectionCoroutine()
        {
            if (spawnProtectionCoroutine != null)
            {
                StopCoroutine(spawnProtectionCoroutine);
                spawnProtectionCoroutine = null;
            }
        }

        private void OnSpawnProtectionChanged(bool previousValue, bool newValue)
        {
            if (shieldVisual != null)
            {
                shieldVisual.SetActive(newValue);
            }
        }

        private void CreateShieldVisual()
        {
            if (shieldVisual != null)
            {
                return;
            }

            shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            shieldVisual.name = "SpawnShield";
            Collider shieldCollider = shieldVisual.GetComponent<Collider>();
            if (shieldCollider != null)
            {
                shieldCollider.enabled = false;
                Destroy(shieldCollider);
            }

            shieldVisual.transform.SetParent(transform, false);
            shieldVisual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            shieldVisual.transform.localScale = new Vector3(1.2f, 0.9f, 1.2f);

            MeshRenderer renderer = shieldVisual.GetComponent<MeshRenderer>();
            Material material = Resources.Load<Material>("SpawnShield");
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            shieldVisual.SetActive(spawnProtected.Value);
        }

        private void OnHealthValueChanged(float previousValue, float newValue)
        {
            HealthChanged?.Invoke(newValue, MAX_HEALTH);
        }

        private bool IsAttackerConnected(ulong attackerClientId)
        {
            return NetworkManager != null && NetworkManager.ConnectedClients.ContainsKey(attackerClientId);
        }

        private void OnDeathStateChanged(bool previousValue, bool newValue)
        {
            DeathStateChanged?.Invoke(newValue);
        }

        [ClientRpc]
        private void RespawnClientRpc(Vector3 position, Quaternion rotation, ClientRpcParams clientRpcParams = default)
        {
            GetComponent<PlayerController>().Respawn(position, rotation);
        }

        [ClientRpc]
        private void ShowHitMarkerClientRpc(bool isKill, HitCue hitCue, ClientRpcParams clientRpcParams = default)
        {
            GameHudController.Instance?.ShowHitFeedback(isKill, hitCue);
        }

        [ClientRpc]
        private void PlayDeathSoundClientRpc()
        {
            ProceduralAudio.Play(
                ProceduralAudio.DeathClip,
                transform.position + Vector3.up,
                0.9f,
                0.9f);
        }

        [ClientRpc]
        private void ShowDamageFeedbackClientRpc(Vector3 attackerPosition, ClientRpcParams clientRpcParams = default)
        {
            GameHudController hud = GameHudController.Instance;
            if (hud != null)
            {
                hud.ShowDamageFrom(attackerPosition);
            }
        }
    }
}

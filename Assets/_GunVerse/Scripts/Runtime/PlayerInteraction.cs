using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GunVerse
{
    [DefaultExecutionOrder(100)]
    public class PlayerInteraction : NetworkBehaviour
    {
        private const float RAYCAST_SKIN = 0.5f;
        private const float RELOAD_REQUEST_COOLDOWN = 0.25f;
        private const float HELD_ITEM_SEARCH_INTERVAL = 0.25f;
        private const string VIEWMODEL_LAYER_NAME = "ViewModel";
        private const float AIM_MAX_DISTANCE = 250f;
        private const float FIRE_ORIGIN_OFFSET = 0.35f;
        private const float MIN_FIRE_DIRECTION_SQR_MAGNITUDE = 0.000001f;
        private const int AIM_HIT_BUFFER_SIZE = 64;
        private const int MAX_SHOTS_PER_VOLLEY = 16;
        private const float MAX_LAUNCH_ORIGIN_DISTANCE = 3f;
        private const float MAX_LAUNCH_ORIGIN_BELOW_CAMERA = 2f;

        [SerializeField] private Transform interactionCamera;

        private readonly RaycastHit[] aimHits = new RaycastHit[AIM_HIT_BUFFER_SIZE];
        private GunItem hoveredItem;
        private Vector3 hoveredPoint;
        private GunItem heldItem;
        private Camera aimCamera;
        private int viewmodelLayerMask;
        private float nextFireRequestTime;
        private float nextReloadRequestTime;
        private float nextHeldItemSearchTime;
        private bool wasReloading;
        private bool lastAmmoVisible;
        private int lastAmmo = -1;
        private int lastMagazineSize = -1;
        private bool lastReloading;
        private bool lastDrawing;
        private Coroutine clientBurstCoroutine;
        private uint nextShotGroupId;
        private PlayerHealth playerHealth;
        private PlayerCameraFeel playerCameraFeel;

        public GunItem HeldItem => heldItem;

        public override void OnNetworkSpawn()
        {
            playerHealth = GetComponent<PlayerHealth>();
            playerCameraFeel = GetComponent<PlayerCameraFeel>();
            viewmodelLayerMask = LayerMask.GetMask(VIEWMODEL_LAYER_NAME);

            if (!IsOwner)
            {
                return;
            }

            aimCamera = Camera.main;
            ProjectileVisualPool.Initialize(NetworkManager.Singleton);
            GunItem.HolderChanged += OnHolderChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner)
            {
                return;
            }

            StopClientBurst();
            GunItem.HolderChanged -= OnHolderChanged;
            ClearHoveredItem();
        }

        private void OnHolderChanged(ulong clientId, GunItem item)
        {
            if (clientId != NetworkManager.LocalClientId)
            {
                return;
            }

            if (item != null && item != heldItem)
            {
                ProceduralAudio.Play(ProceduralAudio.PickupClip, item.transform.position, 0.4f);
                GunEffects.SpawnPickupBurst(item.WorldCenter, item.MainColor);
            }

            if (heldItem != item)
            {
                StopClientBurst();
                nextFireRequestTime = 0f;
                nextReloadRequestTime = 0f;
                wasReloading = false;
                lastAmmoVisible = false;
                lastAmmo = -1;
                lastMagazineSize = -1;
                lastReloading = false;
                lastDrawing = false;
            }

            heldItem = item;
            ClearHoveredItem();
        }

        private void TryRecoverHeldItem()
        {
            if (Time.time < nextHeldItemSearchTime)
            {
                return;
            }

            nextHeldItemSearchTime = Time.time + HELD_ITEM_SEARCH_INTERVAL;
            GunItem foundItem = GunItem.FindHeld(NetworkManager.LocalClientId);
            if (foundItem != null)
            {
                OnHolderChanged(NetworkManager.LocalClientId, foundItem);
            }
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            if (playerHealth != null && playerHealth.IsDead)
            {
                ClearHoveredItem();
                return;
            }

            UpdateAmmoHud();

            if (!GameModeController.IsGameplayActive)
            {
                ClearHoveredItem();
                return;
            }

            if (heldItem == null)
            {
                TryRecoverHeldItem();
            }

            bool isGunGame = GameModeController.Instance != null &&
                             GameModeController.Instance.Mode == GameModeType.GunGame;

            UpdateHover();

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.eKey.wasPressedThisFrame && hoveredItem != null)
                {
                    hoveredItem.RequestPickUpServerRpc(hoveredPoint);
                }

                if (!isGunGame && keyboard.gKey.wasPressedThisFrame && heldItem != null)
                {
                    heldItem.RequestDropServerRpc(heldItem.transform.position, heldItem.transform.rotation);
                }

                if (keyboard.rKey.wasPressedThisFrame && heldItem != null && heldItem.CanReload)
                {
                    RequestReload(heldItem);
                }
            }
        }

        private void LateUpdate()
        {
            if (!IsOwner || (playerHealth != null && playerHealth.IsDead) || !GameModeController.IsGameplayActive)
            {
                return;
            }

            if (heldItem != null)
            {
                bool isReloading = heldItem.IsReloading;
                if (wasReloading && !isReloading)
                {
                    nextFireRequestTime = 0f;
                }

                wasReloading = isReloading;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || heldItem == null)
            {
                return;
            }

            if (GameModeController.IsClientVisualProjectileMode)
            {
                TryClientVisualFire(heldItem, mouse);
                return;
            }

            bool wantsToFire = heldItem.FireMode == GunFireMode.Automatic
                ? mouse.leftButton.isPressed
                : mouse.leftButton.wasPressedThisFrame;
            if (!wantsToFire)
            {
                return;
            }

            if (heldItem.IsDrawing || heldItem.IsReloading)
            {
                return;
            }

            if (heldItem.CurrentAmmo <= 0)
            {
                if (heldItem.CanReload)
                {
                    RequestReload(heldItem);
                }

                return;
            }

            if (Time.time < nextFireRequestTime)
            {
                return;
            }

            nextFireRequestTime = Time.time + 60f / heldItem.RoundsPerMinute;

            GetAimRay(out Vector3 aimOrigin, out Vector3 aimForward);
            Vector3 fireOrigin = aimOrigin + aimForward * FIRE_ORIGIN_OFFSET;
            Vector3 aimPoint = FindAimPoint(heldItem);
            Vector3 fireDirection = aimPoint - fireOrigin;
            if (fireDirection.sqrMagnitude < MIN_FIRE_DIRECTION_SQR_MAGNITUDE ||
                Vector3.Dot(fireDirection, aimForward) <= 0f)
            {
                fireDirection = aimForward;
            }
            else
            {
                fireDirection.Normalize();
            }

            ProjectileTrace.Log(
                "client fire t=" + Time.time.ToString("F3") +
                " client=" + NetworkManager.LocalClientId +
                " gun=" + heldItem.name +
                " gunId=" + heldItem.NetworkObjectId +
                " ammo=" + heldItem.CurrentAmmo +
                " origin=" + fireOrigin.ToString("F3") +
                " aim=" + aimPoint.ToString("F3") +
                " dir=" + fireDirection.ToString("F3"));

            heldItem.GetProjectileSpawnPose(
                fireOrigin,
                fireDirection,
                out Vector3 spawnPosition,
                out _);

            Vector3 feedbackPosition = heldItem.GetMuzzleTipWorldPosition();
            if (Vector3.Distance(feedbackPosition, aimOrigin) > MAX_LAUNCH_ORIGIN_DISTANCE ||
                !IsMuzzleWithinLaunchBounds(feedbackPosition, aimOrigin, aimForward))
            {
                feedbackPosition = spawnPosition;
            }

            float recoilPitch = heldItem.RecoilPitch;
            float cameraShake = heldItem.CameraShake;
            float fieldOfViewPunch = heldItem.FieldOfViewPunch;
            heldItem.PlayLocalFireFeedback(feedbackPosition, fireDirection);
            heldItem.ApplyLocalRecoil();
            heldItem.RequestFireServerRpc(fireOrigin, fireDirection);

            if (playerCameraFeel != null)
            {
                playerCameraFeel.ApplyShotKick(recoilPitch, cameraShake, fieldOfViewPunch);
            }
        }

        private void TryClientVisualFire(GunItem item, Mouse mouse)
        {
            bool wantsToFire = item.FireMode == GunFireMode.Automatic
                ? mouse.leftButton.isPressed
                : mouse.leftButton.wasPressedThisFrame;

            if (!wantsToFire ||
                item.IsDrawing ||
                item.IsReloading ||
                clientBurstCoroutine != null)
            {
                return;
            }

            if (item.CurrentAmmo <= 0)
            {
                if (item.CanReload)
                {
                    RequestReload(item);
                }

                return;
            }

            if (Time.time < nextFireRequestTime)
            {
                return;
            }

            nextFireRequestTime = Time.time + 60f / Mathf.Max(1f, item.RoundsPerMinute);

            nextShotGroupId++;
            if (nextShotGroupId == 0)
            {
                nextShotGroupId = 1;
            }

            int burstCount = Mathf.Max(1, item.BurstCount);
            FireClientVisualVolley(item, nextShotGroupId, 0, burstCount);

            if (burstCount > 1)
            {
                clientBurstCoroutine = StartCoroutine(ClientBurstRoutine(item, nextShotGroupId, burstCount));
            }
        }

        private IEnumerator ClientBurstRoutine(GunItem item, uint shotGroupId, int burstCount)
        {
            for (int burstIndex = 1; burstIndex < burstCount; burstIndex++)
            {
                yield return new WaitForSeconds(item.BurstInterval);

                if (heldItem != item ||
                    item.State != GunItemState.Held ||
                    item.IsDrawing ||
                    item.IsReloading ||
                    item.CurrentAmmo <= 0 ||
                    playerHealth.IsDead ||
                    !GameModeController.IsGameplayActive)
                {
                    break;
                }

                FireClientVisualVolley(item, shotGroupId, burstIndex, burstCount);
            }

            clientBurstCoroutine = null;
        }

        private void FireClientVisualVolley(GunItem item, uint shotGroupId, int burstIndex, int burstCount)
        {
            GetAimRay(out Vector3 cameraOrigin, out Vector3 aimForward);
            Camera camera = aimCamera != null ? aimCamera : Camera.main;
            Vector3 cameraRight = camera != null ? camera.transform.right : Vector3.right;
            Vector3 aimPoint = FindAimPoint(item);
            Vector3 itemLaunchOrigin = item.GetProjectileLaunchOrigin();
            Vector3 rawLaunchOrigin = itemLaunchOrigin;
            if (Vector3.Distance(rawLaunchOrigin, cameraOrigin) > MAX_LAUNCH_ORIGIN_DISTANCE ||
                rawLaunchOrigin.y < cameraOrigin.y - MAX_LAUNCH_ORIGIN_BELOW_CAMERA)
            {
                rawLaunchOrigin = cameraOrigin;
            }

            Vector3 centeredMuzzle = ProjectileLaunchUtility.CenterMuzzleHorizontally(
                rawLaunchOrigin,
                cameraOrigin,
                cameraRight);
            Vector3 fireDirection = aimPoint - centeredMuzzle;

            if (fireDirection.sqrMagnitude < MIN_FIRE_DIRECTION_SQR_MAGNITUDE ||
                Vector3.Dot(fireDirection, aimForward) <= 0f)
            {
                fireDirection = aimForward;
            }
            else
            {
                fireDirection.Normalize();
            }

            Vector3 muzzlePosition = centeredMuzzle;
            if (!item.FireAsSelfProjectile &&
                !ProjectileCollision.TryResolveMuzzle(
                    centeredMuzzle,
                    fireDirection,
                    item.MuzzleProbeRadius,
                    out muzzlePosition))
            {
                ProjectileTrace.Log(
                    "client muzzle fallback t=" + Time.time.ToString("F3") +
                    " gun=" + item.name +
                    " probe=" + item.MuzzleProbeRadius.ToString("F3") +
                    " muzzle=" + centeredMuzzle.ToString("F3"));
                muzzlePosition = centeredMuzzle;
            }

            if (!IsMuzzleWithinLaunchBounds(muzzlePosition, cameraOrigin, aimForward))
            {
                muzzlePosition = IsMuzzleWithinLaunchBounds(centeredMuzzle, cameraOrigin, aimForward)
                    ? centeredMuzzle
                    : cameraOrigin;
            }

            bool isItemMuzzleUsable =
                Vector3.Distance(itemLaunchOrigin, cameraOrigin) <= MAX_LAUNCH_ORIGIN_DISTANCE &&
                IsMuzzleWithinLaunchBounds(itemLaunchOrigin, cameraOrigin, aimForward);
            Vector3 visualMuzzle = isItemMuzzleUsable ? itemLaunchOrigin : muzzlePosition;

            Vector3 muzzleTip = item.GetMuzzleTipWorldPosition();
            bool isMuzzleTipUsable =
                Vector3.Distance(muzzleTip, cameraOrigin) <= MAX_LAUNCH_ORIGIN_DISTANCE &&
                IsMuzzleWithinLaunchBounds(muzzleTip, cameraOrigin, aimForward);
            Vector3 flashMuzzle = isMuzzleTipUsable ? muzzleTip : visualMuzzle;

            item.GetProjectileSpawnPose(
                muzzlePosition,
                fireDirection,
                out Vector3 spawnCenter,
                out _);
            float releaseDistance = Mathf.Max(
                0f,
                Vector3.Dot(spawnCenter - muzzlePosition, fireDirection.normalized));

            int pelletCount = Mathf.Min(
                MAX_SHOTS_PER_VOLLEY,
                Mathf.Min(item.ProjectilesPerShot, item.CurrentAmmo));
            if (pelletCount <= 0)
            {
                return;
            }

            double serverTime = NetworkManager.ServerTime.Time;
            ProjectileShotData[] shots = new ProjectileShotData[pelletCount];
            Vector3 rotationUp = Mathf.Abs(Vector3.Dot(fireDirection, Vector3.up)) > 0.99f
                ? item.transform.up
                : Vector3.up;
            Quaternion baseRotation = Quaternion.LookRotation(fireDirection, rotationUp);

            for (int index = 0; index < pelletCount; index++)
            {
                Vector3 pelletDirection = fireDirection;
                if (pelletCount > 1 && item.SpreadAngle > 0f)
                {
                    float yaw = Random.Range(-item.SpreadAngle, item.SpreadAngle);
                    float pitch = Random.Range(-item.SpreadAngle, item.SpreadAngle);
                    pelletDirection = baseRotation * (Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward);
                    pelletDirection.Normalize();
                }

                bool useItemShape = item.FireAsSelfProjectile ||
                                    (item.NetworkObject != null &&
                                     item.ProjectileVisualHash == item.NetworkObject.PrefabIdHash);
                Vector3 scaledExtents = Vector3.Scale(item.LocalBounds.extents, item.OriginalWorldScale);
                Vector3 scaledCenter = Vector3.Scale(item.LocalBounds.center, item.OriginalWorldScale);
                Vector3 scaledLaunch = Vector3.Scale(item.ProjectileLaunchLocalPosition, item.OriginalWorldScale);
                if (!IsFinite(scaledExtents) ||
                    !IsFinite(scaledCenter) ||
                    !IsFinite(scaledLaunch) ||
                    scaledExtents.x <= 0f ||
                    scaledExtents.y <= 0f ||
                    scaledExtents.z <= 0f)
                {
                    useItemShape = false;
                }

                ProjectileShotData shot = new ProjectileShotData
                {
                    shotId = ProjectileShotIds.Next(),
                    shotGroupId = shotGroupId,
                    shooter = NetworkManager.LocalClientId,
                    selfProjectile = item.FireAsSelfProjectile,
                    selfProjectileNetworkObjectId = item.FireAsSelfProjectile ? item.NetworkObjectId : 0UL,
                    origin = muzzlePosition,
                    direction = pelletDirection,
                    spin = item.FireAsSelfProjectile ? item.CreateClientThrowSpin() : Vector3.zero,
                    speed = item.ProjectileSpeed,
                    radius = item.ProjectileRadius,
                    gravityScale = item.ProjectileGravityScale,
                    lifetime = item.ProjectileLifetime,
                    bounceDamping = item.ProjectileBounceDamping,
                    angularDamping = item.ProjectileAngularDamping,
                    mass = item.ProjectileMass,
                    damage = item.Damage,
                    releaseDistance = releaseDistance,
                    useItemShape = useItemShape,
                    shapeType = useItemShape
                        ? (byte)ProjectileShapeUtility.ResolveShapeType(scaledExtents)
                        : (byte)ProjectileShapeType.Sphere,
                    localHalfExtents = useItemShape ? scaledExtents : Vector3.zero,
                    localCenterOffset = useItemShape ? scaledCenter - scaledLaunch : Vector3.zero,
                    localCenter = useItemShape ? scaledCenter : Vector3.zero,
                    spawnGraceSeconds = useItemShape ? ProjectileFlightSim.SelfProjectileSpawnGraceSeconds : 0f,
                    maxBounces = item.ProjectileMaxBounces,
                    visualPrefabHash = unchecked((int)item.ProjectileVisualHash),
                    weaponHash = item.NetworkObject != null ? unchecked((int)item.NetworkObject.PrefabIdHash) : 0,
                    pelletIndex = index,
                    pelletCount = pelletCount,
                    burstIndex = burstIndex,
                    burstCount = burstCount,
                    serverTime = serverTime
                };

                shots[index] = shot;
                ProjectileVisualPool.Spawn(
                    shot,
                    true,
                    visualMuzzle,
                    item.FireAsSelfProjectile ? item.gameObject : null);
            }

            ProjectileShotVolley volley = new ProjectileShotVolley { Shots = shots };
            item.FireClientVisualShotsServerRpc(volley);

            ProjectileTrace.Log(
                "client visual fire t=" + Time.time.ToString("F3") +
                " client=" + NetworkManager.LocalClientId +
                " gun=" + item.name +
                " player=" + transform.position.ToString("F3") +
                " camera=" + cameraOrigin.ToString("F3") +
                " rawMuzzle=" + itemLaunchOrigin.ToString("F3") +
                " probe=" + item.MuzzleProbeRadius.ToString("F3") +
                " centered=" + centeredMuzzle.ToString("F3") +
                " muzzle=" + muzzlePosition.ToString("F3") +
                " visualMuzzle=" + visualMuzzle.ToString("F3") +
                " flashMuzzle=" + flashMuzzle.ToString("F3") +
                " aim=" + aimPoint.ToString("F3") +
                " dir=" + fireDirection.ToString("F3") +
                " shots=" + pelletCount +
                " group=" + shotGroupId +
                " burst=" + burstIndex + "/" + burstCount);

            item.PlayLocalFireFeedback(flashMuzzle, fireDirection);
            item.ApplyLocalRecoil();

            if (playerCameraFeel != null)
            {
                playerCameraFeel.ApplyShotKick(item.RecoilPitch, item.CameraShake, item.FieldOfViewPunch);
            }
        }

        private static bool IsMuzzleWithinLaunchBounds(
            Vector3 muzzlePosition,
            Vector3 cameraOrigin,
            Vector3 aimForward)
        {
            return Vector3.Dot(muzzlePosition - cameraOrigin, aimForward) >= 0f &&
                   muzzlePosition.y >= cameraOrigin.y - MAX_LAUNCH_ORIGIN_BELOW_CAMERA;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                   !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                   !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }

        private void StopClientBurst()
        {
            if (clientBurstCoroutine == null)
            {
                return;
            }

            StopCoroutine(clientBurstCoroutine);
            clientBurstCoroutine = null;
        }

        private void GetAimRay(out Vector3 origin, out Vector3 direction)
        {
            Camera camera = aimCamera != null ? aimCamera : Camera.main;
            if (camera != null)
            {
                aimCamera = camera;
                Ray centerRay = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                origin = centerRay.origin;
                direction = centerRay.direction;
                return;
            }

            Transform fallback = interactionCamera != null ? interactionCamera : transform;
            origin = fallback.position;
            direction = fallback.forward;
        }

        private Vector3 FindAimPoint(GunItem held)
        {
            GetAimRay(out Vector3 origin, out Vector3 forward);
            Vector3 fallbackPoint = origin + forward * AIM_MAX_DISTANCE;

            int hitCount = Physics.RaycastNonAlloc(
                origin,
                forward,
                aimHits,
                AIM_MAX_DISTANCE,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);

            Vector3 closestPoint = fallbackPoint;
            float closestDistance = float.MaxValue;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = aimHits[index];
                if (hit.distance >= closestDistance || ShouldIgnoreAimHit(hit, held))
                {
                    continue;
                }

                closestDistance = hit.distance;
                closestPoint = hit.point;
            }

            return closestPoint;
        }

        private bool ShouldIgnoreAimHit(RaycastHit hit, GunItem held)
        {
            Transform hitTransform = hit.transform;
            if (hitTransform == null)
            {
                return true;
            }

            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                return true;
            }

            GunItem hitItem = hitTransform.GetComponentInParent<GunItem>();
            if (hitItem != null && hitItem == held)
            {
                return true;
            }

            int hitLayerMask = 1 << hitTransform.gameObject.layer;
            return (hitLayerMask & viewmodelLayerMask) != 0;
        }

        private void RequestReload(GunItem item)
        {
            if (!item.CanReload || Time.time < nextReloadRequestTime)
            {
                return;
            }

            nextReloadRequestTime = Time.time + RELOAD_REQUEST_COOLDOWN;
            item.RequestReloadServerRpc();
        }

        private void UpdateAmmoHud()
        {
            GameHudController hud = GameHudController.Instance;
            if (hud == null)
            {
                return;
            }

            bool hasItem = heldItem != null;
            bool isReloading = hasItem && heldItem.IsReloading;
            bool isDrawing = hasItem && heldItem.IsDrawing;
            int ammo = hasItem ? heldItem.CurrentAmmo : 0;
            int magazineSize = hasItem ? heldItem.MagazineSize : 0;

            if (lastAmmoVisible == hasItem &&
                lastAmmo == ammo &&
                lastMagazineSize == magazineSize &&
                lastReloading == isReloading &&
                lastDrawing == isDrawing)
            {
                return;
            }

            lastAmmoVisible = hasItem;
            lastAmmo = ammo;
            lastMagazineSize = magazineSize;
            lastReloading = isReloading;
            lastDrawing = isDrawing;

            if (hasItem)
            {
                hud.ShowAmmo(isDrawing
                    ? "DRAWING..."
                    : isReloading
                        ? "RELOADING..."
                        : $"{ammo} / {magazineSize}");
            }
            else
            {
                hud.HideAmmo();
            }
        }

        private void UpdateHover()
        {
            GunItem newHover = null;
            Vector3 newHoverPoint = Vector3.zero;

            GetAimRay(out Vector3 aimOrigin, out Vector3 aimDirection);
            Vector3 origin = aimOrigin + aimDirection * RAYCAST_SKIN;
            float reach = GunItem.PICKUP_RANGE + RAYCAST_SKIN;
            int hoverMask = Physics.DefaultRaycastLayers & ~(1 << GunItem.WorldHullLayerIndex);
            if (Physics.Raycast(origin, aimDirection, out RaycastHit hit, reach, hoverMask))
            {
                GunItem candidate = hit.collider.GetComponentInParent<GunItem>();
                if (candidate != null &&
                    candidate.State == GunItemState.World &&
                    candidate.IsWithinPickupRange(transform.position) &&
                    IsHoverAllowed(candidate))
                {
                    newHover = candidate;
                    newHoverPoint = hit.point;
                }
            }

            if (newHover != hoveredItem)
            {
                if (hoveredItem != null)
                {
                    hoveredItem.SetLabelVisible(false);
                }

                hoveredItem = newHover;

                if (hoveredItem != null)
                {
                    hoveredItem.SetLabelVisible(true);
                }

                UpdateHud();
            }

            if (hoveredItem != null)
            {
                hoveredPoint = newHoverPoint;
            }
        }

        private bool IsHoverAllowed(GunItem candidate)
        {
            GameModeController controller = GameModeController.Instance;
            bool isGunGame = controller != null && controller.Mode == GameModeType.GunGame;
            if (!isGunGame)
            {
                return true;
            }

            if (!candidate.FireAsSelfProjectile)
            {
                return false;
            }

            return controller.IsCurrentGunGameWeapon(
                NetworkManager.LocalClientId,
                candidate.NetworkObject.PrefabIdHash);
        }

        private void ClearHoveredItem()
        {
            if (hoveredItem == null)
            {
                return;
            }

            hoveredItem.SetLabelVisible(false);
            hoveredItem = null;
            UpdateHud();
        }

        private void UpdateHud()
        {
            GameHudController hud = GameHudController.Instance;
            if (hud == null)
            {
                return;
            }

            if (heldItem != null)
            {
                hud.ShowHeldItem(heldItem.DisplayName);
            }
            else
            {
                hud.HideHeldItem();
            }

            if (hoveredItem != null && hoveredItem != heldItem)
            {
                string action = heldItem != null ? "swap for" : "pick up";
                hud.ShowPrompt("Press E to " + action + " " + hoveredItem.DisplayName);
            }
            else
            {
                hud.HidePrompt();
            }
        }
    }
}

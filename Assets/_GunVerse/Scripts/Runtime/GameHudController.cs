using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace GunVerse
{
    public class GameHudController : MonoBehaviour
    {
        private const float HIT_MARKER_DURATION = 0.15f;
        private const float KILL_MARKER_DURATION = 0.4f;
        private const float HIT_MARKER_START_SCALE = 1.4f;
        private const float KILL_MARKER_START_SCALE = 1.8f;
        private const float STATUS_MESSAGE_DURATION = 1f;
        private const float KILL_FEED_DURATION = 5f;
        private const int KILL_FEED_MAX_LINES = 4;
        private const float DAMAGE_INDICATOR_DURATION = 0.9f;
        private const float DAMAGE_INDICATOR_RADIUS = 110f;
        private const float MIN_DAMAGE_DIRECTION_SQR = 0.0001f;
        private const float SCREEN_FLASH_DURATION = 0.35f;
        private const float DAMAGE_FLASH_MIN_ALPHA = 0.08f;
        private const float DAMAGE_FLASH_MAX_ALPHA = 0.3f;
        private const float DAMAGE_FLASH_SCALE = 2f;
        private const float DEATH_FLASH_ALPHA = 0.5f;
        private const float RESPAWN_FLASH_ALPHA = 0.2f;
        private const int MATCH_TIME_WARNING_SECONDS = 30;
        private const float GUN_GAME_NOTIFICATION_DURATION = 1.5f;

        private static Color HitMarkerColor => ArtPalette.Ui.Secondary;
        private static Color KillMarkerColor => ArtPalette.Ui.Danger;
        private static Color DamageIndicatorColor => ArtPalette.Ui.Danger;
        private static Color DamageFlashColor => ArtPalette.Ui.Danger;
        private static Color RespawnFlashColor => ArtPalette.Ui.Secondary;
        private static Color MatchTimeColor => ArtPalette.Ui.Cream;
        private static Color MatchTimeWarningColor => ArtPalette.Ui.Danger;
        private static Color GunGameNotificationColor => ArtPalette.Ui.Accent;

        public static GameHudController Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticInstance()
        {
            Instance = null;
        }

        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private TextMeshProUGUI heldItemText;
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI hitMarkerText;
        [SerializeField] private RectTransform hitMarkerRoot;
        [SerializeField] private CanvasGroup hitMarkerCanvasGroup;
        [SerializeField] private Image[] hitMarkerTicks;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI killFeedText;
        [SerializeField] private TextMeshProUGUI damageIndicatorText;
        [SerializeField] private TextMeshProUGUI modeText;
        [SerializeField] private TextMeshProUGUI teamText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI matchTimeText;
        [SerializeField] private TextMeshProUGUI gunGameProgressText;
        [SerializeField] private TextMeshProUGUI gunGameNotificationText;
        [SerializeField] private Image screenFlashImage;

        private readonly List<string> killFeedMessages = new List<string>();

        private PlayerHealth boundPlayerHealth;
        private PlayerGunGame boundGunGame;
        private float previousHealth;
        private int previousGunGameLevel = -1;
        private Coroutine hitMarkerCoroutine;
        private Coroutine statusCoroutine;
        private Coroutine killFeedCoroutine;
        private Coroutine damageIndicatorCoroutine;
        private Coroutine screenFlashCoroutine;
        private Coroutine gunGameNotificationCoroutine;
        private int lastMatchTimeSeconds = -1;
        private int lastGunGameLevel = -1;
        private int lastGunGameTotal = -1;
        private string lastGunGameWeaponName = string.Empty;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            UpdateMatchHud();
        }

        private void UpdateMatchHud()
        {
            GameModeController gameMode = GameModeController.Instance;

            if (modeText != null)
            {
                bool hasMode = gameMode != null;
                modeText.gameObject.SetActive(hasMode);
                if (hasMode)
                {
                    modeText.text = gameMode.Mode switch
                    {
                        GameModeType.TeamDeathmatch => "TEAM DEATHMATCH",
                        GameModeType.GunGame => "GUN GAME",
                        _ => "FREE FOR ALL"
                    };
                }
            }

            if (teamText != null)
            {
                bool isTeamDeathmatch = gameMode != null && gameMode.Mode == GameModeType.TeamDeathmatch;
                PlayerTeam localTeam = isTeamDeathmatch ? GetLocalPlayerTeam() : null;
                bool showTeam = isTeamDeathmatch &&
                                localTeam != null &&
                                localTeam.Team != TeamType.None;
                teamText.gameObject.SetActive(showTeam);
                if (showTeam)
                {
                    teamText.text = TeamPalette.GetName(localTeam.Team);
                    teamText.color = TeamPalette.GetColor(localTeam.Team);
                }
            }

            if (countdownText != null)
            {
                bool waitingForPlayers = gameMode != null && gameMode.Phase == MatchPhase.WaitingForLoad;
                bool showCountdown = gameMode != null && gameMode.Phase == MatchPhase.Countdown;
                countdownText.gameObject.SetActive(waitingForPlayers || showCountdown);

                if (waitingForPlayers)
                {
                    countdownText.text = "WAITING FOR PLAYERS...";
                }
                else if (showCountdown)
                {
                    countdownText.text = gameMode.CountdownSeconds.ToString();
                }
            }

            if (matchTimeText != null)
            {
                bool showMatchTime = gameMode != null &&
                                     (gameMode.Phase == MatchPhase.Playing || gameMode.Phase == MatchPhase.Results);
                matchTimeText.gameObject.SetActive(showMatchTime);
                if (showMatchTime)
                {
                    int remainingSeconds = gameMode.RemainingSeconds;
                    if (remainingSeconds != lastMatchTimeSeconds)
                    {
                        lastMatchTimeSeconds = remainingSeconds;
                        matchTimeText.text = FormatMatchTime(remainingSeconds);
                        matchTimeText.color = remainingSeconds <= MATCH_TIME_WARNING_SECONDS
                            ? MatchTimeWarningColor
                            : MatchTimeColor;
                    }
                }
                else
                {
                    lastMatchTimeSeconds = -1;
                }
            }

            UpdateGunGameHud(gameMode);
        }

        private void UpdateGunGameHud(GameModeController gameMode)
        {
            bool isGunGame = gameMode != null &&
                             gameMode.Mode == GameModeType.GunGame &&
                             gameMode.Phase != MatchPhase.Results;

            if (!isGunGame)
            {
                HideGunGameHud();
                return;
            }

            PlayerGunGame localProgress = GetLocalPlayerGunGame();
            bool show = localProgress != null && localProgress.IsSpawned;

            if (gunGameProgressText != null)
            {
                gunGameProgressText.gameObject.SetActive(show);
                if (show)
                {
                    int total = Mathf.Max(1, gameMode.GunGameStepCount);
                    int level = Mathf.Clamp(localProgress.Level, 0, total - 1);
                    string weaponName = string.IsNullOrWhiteSpace(localProgress.WeaponName)
                        ? gameMode.GetGunGameWeaponName(level)
                        : localProgress.WeaponName;

                    if (level != lastGunGameLevel ||
                        total != lastGunGameTotal ||
                        !string.Equals(weaponName, lastGunGameWeaponName))
                    {
                        lastGunGameLevel = level;
                        lastGunGameTotal = total;
                        lastGunGameWeaponName = weaponName;

                        gunGameProgressText.text = string.IsNullOrWhiteSpace(weaponName)
                            ? $"GUN GAME  {level + 1} / {total}"
                            : $"GUN GAME  {level + 1} / {total}  -  {weaponName.ToUpperInvariant()}";
                    }
                }
            }

            if (!show)
            {
                ResetGunGameNotification();
                return;
            }

            if (boundGunGame != localProgress)
            {
                boundGunGame = localProgress;
                previousGunGameLevel = localProgress.Level;
                return;
            }

            if (localProgress.Level > previousGunGameLevel)
            {
                previousGunGameLevel = localProgress.Level;
                ShowGunGameNotification(localProgress.WeaponName);
            }
            else if (localProgress.Level < previousGunGameLevel)
            {
                previousGunGameLevel = localProgress.Level;
            }
        }

        private void HideGunGameHud()
        {
            if (gunGameProgressText != null)
            {
                gunGameProgressText.gameObject.SetActive(false);
            }

            ResetGunGameNotification();
            lastGunGameLevel = -1;
            lastGunGameTotal = -1;
            lastGunGameWeaponName = string.Empty;
        }

        private static PlayerGunGame GetLocalPlayerGunGame()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || networkManager.LocalClient == null)
            {
                return null;
            }

            NetworkObject playerObject = networkManager.LocalClient.PlayerObject;
            return playerObject != null && playerObject.TryGetComponent(out PlayerGunGame progress)
                ? progress
                : null;
        }

        private void ShowGunGameNotification(string weaponName)
        {
            if (gunGameNotificationText == null)
            {
                return;
            }

            gunGameNotificationText.text = string.IsNullOrWhiteSpace(weaponName)
                ? "LEVEL UP"
                : $"LEVEL UP  -  {weaponName.ToUpperInvariant()}";
            gunGameNotificationText.color = GunGameNotificationColor;
            gunGameNotificationText.gameObject.SetActive(true);

            if (gunGameNotificationCoroutine != null)
            {
                StopCoroutine(gunGameNotificationCoroutine);
            }

            gunGameNotificationCoroutine = StartCoroutine(HideGunGameNotificationAfterDelay());
        }

        private IEnumerator HideGunGameNotificationAfterDelay()
        {
            yield return new WaitForSeconds(GUN_GAME_NOTIFICATION_DURATION);
            gunGameNotificationCoroutine = null;

            if (gunGameNotificationText != null)
            {
                gunGameNotificationText.gameObject.SetActive(false);
            }
        }

        private void ResetGunGameNotification()
        {
            if (gunGameNotificationCoroutine != null)
            {
                StopCoroutine(gunGameNotificationCoroutine);
                gunGameNotificationCoroutine = null;
            }

            boundGunGame = null;
            previousGunGameLevel = -1;

            if (gunGameNotificationText != null)
            {
                gunGameNotificationText.gameObject.SetActive(false);
            }
        }

        private static string FormatMatchTime(int seconds)
        {
            int clampedSeconds = Mathf.Max(0, seconds);
            int minutes = clampedSeconds / 60;
            int remainingSeconds = clampedSeconds % 60;
            return $"{minutes}:{remainingSeconds:00}";
        }

        private static PlayerTeam GetLocalPlayerTeam()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || networkManager.LocalClient == null)
            {
                return null;
            }

            NetworkObject playerObject = networkManager.LocalClient.PlayerObject;
            return playerObject != null && playerObject.TryGetComponent(out PlayerTeam playerTeam)
                ? playerTeam
                : null;
        }

        private void OnDestroy()
        {
            UnbindPlayerHealth();
            HideKillFeed();
            ResetGunGameNotification();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void BindPlayerHealth(PlayerHealth playerHealth)
        {
            UnbindPlayerHealth();

            boundPlayerHealth = playerHealth;
            if (boundPlayerHealth == null)
            {
                return;
            }

            boundPlayerHealth.HealthChanged += OnHealthChanged;
            boundPlayerHealth.DeathStateChanged += OnDeathStateChanged;

            previousHealth = boundPlayerHealth.CurrentHealth;

            if (healthText != null)
            {
                healthText.gameObject.SetActive(true);
            }

            OnHealthChanged(boundPlayerHealth.CurrentHealth, PlayerHealth.MAX_HEALTH);

            if (boundPlayerHealth.IsDead)
            {
                ShowStatus("ELIMINATED");
            }
            else
            {
                HideStatus();
            }
        }

        public void UnbindPlayerHealth()
        {
            if (boundPlayerHealth != null)
            {
                boundPlayerHealth.HealthChanged -= OnHealthChanged;
                boundPlayerHealth.DeathStateChanged -= OnDeathStateChanged;
            }

            boundPlayerHealth = null;
            previousHealth = 0f;

            if (healthText != null)
            {
                healthText.gameObject.SetActive(false);
            }

            HideHitMarker();
            HideStatus();
            HideDamageIndicator();
            HideScreenFlash();
        }

        public void ShowPrompt(string message)
        {
            promptText.text = message;
            promptText.gameObject.SetActive(true);
        }

        public void HidePrompt()
        {
            promptText.gameObject.SetActive(false);
        }

        public void ShowHeldItem(string displayName)
        {
            heldItemText.text = displayName;
            heldItemText.gameObject.SetActive(true);
        }

        public void HideHeldItem()
        {
            heldItemText.gameObject.SetActive(false);
        }

        public void ShowAmmo(string text)
        {
            ammoText.text = text;
            ammoText.gameObject.SetActive(true);
        }

        public void HideAmmo()
        {
            ammoText.gameObject.SetActive(false);
        }

        public void ShowDamageFrom(Vector3 worldSourcePosition)
        {
            ProceduralAudio.Play2D(ProceduralAudio.DamageClip, 0.55f, Random.Range(0.9f, 1.05f));

            if (damageIndicatorText == null || boundPlayerHealth == null)
            {
                return;
            }

            Vector3 direction = worldSourcePosition - boundPlayerHealth.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < MIN_DAMAGE_DIRECTION_SQR)
            {
                return;
            }

            Vector3 localDirection = boundPlayerHealth.transform.InverseTransformDirection(direction.normalized);
            float angle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            float angleRadians = angle * Mathf.Deg2Rad;

            RectTransform indicator = damageIndicatorText.rectTransform;
            indicator.anchoredPosition = new Vector2(Mathf.Sin(angleRadians), Mathf.Cos(angleRadians)) * DAMAGE_INDICATOR_RADIUS;
            indicator.localEulerAngles = new Vector3(0f, 0f, -angle);

            damageIndicatorText.color = DamageIndicatorColor;
            damageIndicatorText.gameObject.SetActive(true);

            if (damageIndicatorCoroutine != null)
            {
                StopCoroutine(damageIndicatorCoroutine);
            }

            damageIndicatorCoroutine = StartCoroutine(FadeDamageIndicator());
        }

        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
            float damageTaken = previousHealth - currentHealth;
            previousHealth = currentHealth;

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
            }

            if (damageTaken <= 0f || maxHealth <= 0f)
            {
                return;
            }

            float flashAlpha = Mathf.Clamp(
                damageTaken / maxHealth * DAMAGE_FLASH_SCALE,
                DAMAGE_FLASH_MIN_ALPHA,
                DAMAGE_FLASH_MAX_ALPHA);
            FlashScreen(DamageFlashColor, flashAlpha);
        }

        private void OnDeathStateChanged(bool isDead)
        {
            if (isDead)
            {
                HidePrompt();
                HideHeldItem();
                HideAmmo();
                HideHitMarker();
                HideDamageIndicator();
                ShowStatus("ELIMINATED");
                FlashScreen(DamageFlashColor, DEATH_FLASH_ALPHA);
                return;
            }

            ShowStatus("RESPAWNED");
            ProceduralAudio.Play2D(ProceduralAudio.RespawnClip, 0.5f, 1f);
            FlashScreen(RespawnFlashColor, RESPAWN_FLASH_ALPHA);
            statusCoroutine = StartCoroutine(HideStatusAfterDelay());
        }

        public void ShowHitFeedback(bool isKill, HitCue hitCue)
        {
            Color markerColor = isKill ? KillMarkerColor : HitMarkerColor;
            float startScale = isKill ? KILL_MARKER_START_SCALE : HIT_MARKER_START_SCALE;
            float duration = isKill ? KILL_MARKER_DURATION : HIT_MARKER_DURATION;

            if (hitMarkerRoot != null)
            {
                hitMarkerRoot.gameObject.SetActive(true);
                hitMarkerRoot.localScale = Vector3.one * startScale;

                if (hitMarkerCanvasGroup != null)
                {
                    hitMarkerCanvasGroup.alpha = 1f;
                }

                SetHitMarkerTickColor(markerColor);
            }
            else if (hitMarkerText != null)
            {
                hitMarkerText.color = markerColor;
                hitMarkerText.rectTransform.localScale = Vector3.one * startScale;
                hitMarkerText.gameObject.SetActive(true);
            }
            else
            {
                return;
            }

            HitFeedbackAudio.PlayHitFeedback(isKill, hitCue);

            if (hitMarkerCoroutine != null)
            {
                StopCoroutine(hitMarkerCoroutine);
            }

            hitMarkerCoroutine = StartCoroutine(AnimateHitMarker(markerColor, startScale, duration));
        }

        private void SetHitMarkerTickColor(Color color)
        {
            if (hitMarkerTicks == null)
            {
                return;
            }

            foreach (Image tick in hitMarkerTicks)
            {
                if (tick != null)
                {
                    tick.color = color;
                }
            }
        }

        private void HideHitMarker()
        {
            if (hitMarkerCoroutine != null)
            {
                StopCoroutine(hitMarkerCoroutine);
                hitMarkerCoroutine = null;
            }

            ResetHitMarkerVisual();
        }

        private void ResetHitMarkerVisual()
        {
            if (hitMarkerRoot != null)
            {
                hitMarkerRoot.localScale = Vector3.one;
                hitMarkerRoot.gameObject.SetActive(false);
            }

            if (hitMarkerCanvasGroup != null)
            {
                hitMarkerCanvasGroup.alpha = 1f;
            }

            if (hitMarkerText != null)
            {
                hitMarkerText.rectTransform.localScale = Vector3.one;
                hitMarkerText.gameObject.SetActive(false);
            }
        }

        private void ShowStatus(string message)
        {
            if (statusCoroutine != null)
            {
                StopCoroutine(statusCoroutine);
                statusCoroutine = null;
            }

            if (statusText == null)
            {
                return;
            }

            statusText.text = message;
            statusText.gameObject.SetActive(true);
        }

        public void ShowKillFeed(string killerName, string victimName, TeamType killerTeam = TeamType.None, TeamType victimTeam = TeamType.None)
        {
            if (killFeedText == null)
            {
                return;
            }

            string killer = FormatKillFeedName(killerName, killerTeam, ArtPalette.Ui.Primary);
            string victim = FormatKillFeedName(victimName, victimTeam, ArtPalette.Ui.CreamMuted);
            killFeedMessages.Insert(0, $"{killer} eliminated {victim}");
            if (killFeedMessages.Count > KILL_FEED_MAX_LINES)
            {
                killFeedMessages.RemoveAt(killFeedMessages.Count - 1);
            }

            killFeedText.text = string.Join("\n", killFeedMessages);
            killFeedText.gameObject.SetActive(true);

            if (killFeedCoroutine != null)
            {
                StopCoroutine(killFeedCoroutine);
            }

            killFeedCoroutine = StartCoroutine(HideKillFeedAfterDelay());
        }

        private static string FormatKillFeedName(string displayName, TeamType team, Color fallbackColor)
        {
            return team == TeamType.None
                ? $"<color=#{ArtPalette.ToHtml(fallbackColor)}>{displayName}</color>"
                : $"<color=#{TeamPalette.GetColorHex(team)}>{displayName}</color>";
        }

        private void HideStatus()
        {
            if (statusCoroutine != null)
            {
                StopCoroutine(statusCoroutine);
                statusCoroutine = null;
            }

            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }
        }

        private void HideKillFeed()
        {
            if (killFeedCoroutine != null)
            {
                StopCoroutine(killFeedCoroutine);
                killFeedCoroutine = null;
            }

            killFeedMessages.Clear();

            if (killFeedText != null)
            {
                killFeedText.gameObject.SetActive(false);
            }
        }

        private void HideDamageIndicator()
        {
            if (damageIndicatorCoroutine != null)
            {
                StopCoroutine(damageIndicatorCoroutine);
                damageIndicatorCoroutine = null;
            }

            if (damageIndicatorText != null)
            {
                damageIndicatorText.gameObject.SetActive(false);
            }
        }

        private void FlashScreen(Color color, float alpha)
        {
            if (screenFlashImage == null)
            {
                return;
            }

            Color flashColor = color;
            flashColor.a = alpha;
            screenFlashImage.color = flashColor;
            screenFlashImage.gameObject.SetActive(true);

            if (screenFlashCoroutine != null)
            {
                StopCoroutine(screenFlashCoroutine);
            }

            screenFlashCoroutine = StartCoroutine(FadeScreenFlash(alpha));
        }

        private void HideScreenFlash()
        {
            if (screenFlashCoroutine != null)
            {
                StopCoroutine(screenFlashCoroutine);
                screenFlashCoroutine = null;
            }

            if (screenFlashImage != null)
            {
                screenFlashImage.gameObject.SetActive(false);
            }
        }

        private IEnumerator AnimateHitMarker(Color markerColor, float startScale, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                if (hitMarkerRoot != null)
                {
                    hitMarkerRoot.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, progress);

                    if (hitMarkerCanvasGroup != null)
                    {
                        hitMarkerCanvasGroup.alpha = 1f - progress;
                    }
                }
                else if (hitMarkerText != null)
                {
                    hitMarkerText.rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, progress);

                    Color color = markerColor;
                    color.a = 1f - progress;
                    hitMarkerText.color = color;
                }

                yield return null;
            }

            hitMarkerCoroutine = null;
            ResetHitMarkerVisual();
        }

        private IEnumerator FadeDamageIndicator()
        {
            float elapsed = 0f;
            Color color = DamageIndicatorColor;

            while (elapsed < DAMAGE_INDICATOR_DURATION)
            {
                elapsed += Time.deltaTime;
                color.a = 1f - Mathf.Clamp01(elapsed / DAMAGE_INDICATOR_DURATION);
                damageIndicatorText.color = color;
                yield return null;
            }

            damageIndicatorText.gameObject.SetActive(false);
            damageIndicatorCoroutine = null;
        }

        private IEnumerator FadeScreenFlash(float startAlpha)
        {
            float elapsed = 0f;
            Color color = screenFlashImage.color;

            while (elapsed < SCREEN_FLASH_DURATION)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / SCREEN_FLASH_DURATION));
                screenFlashImage.color = color;
                yield return null;
            }

            screenFlashImage.gameObject.SetActive(false);
            screenFlashCoroutine = null;
        }

        private IEnumerator HideStatusAfterDelay()
        {
            yield return new WaitForSeconds(STATUS_MESSAGE_DURATION);
            statusCoroutine = null;
            statusText.gameObject.SetActive(false);
        }

        private IEnumerator HideKillFeedAfterDelay()
        {
            yield return new WaitForSeconds(KILL_FEED_DURATION);
            killFeedCoroutine = null;
            killFeedMessages.Clear();

            if (killFeedText != null)
            {
                killFeedText.gameObject.SetActive(false);
            }
        }
    }
}

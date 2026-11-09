using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PesonShooting.VFX
{
    // 선택 UI와 분리된 일회성 연출입니다. 씬에 붙이거나 원본 카드/총 재질을 수정할 필요가 없습니다.
    public sealed class PerkAbsorbVfx : MonoBehaviour
    {
        private VfxConfig.PerkAbsorbSettings settings;
        private readonly List<GameObject> overlays = new List<GameObject>();
        private Material flashMaterial;

        public static void Play(Button selectedCard)
        {
            if (!Application.isPlaying || selectedCard == null) return;

            VfxConfig.PerkAbsorbSettings settings = VfxService.PerkAbsorb;
            if (settings == null || !settings.enabled) return;

            PlayerMove player = Object.FindObjectOfType<PlayerMove>();
            Camera camera = Camera.main;
            if (player == null || camera == null) return;

            Transform gun = null;
            foreach (Transform child in player.GetComponentsInChildren<Transform>())
            {
                if (child.name != "Gun_Rifle") continue;
                gun = child;
                break;
            }
            if (gun == null) return;

            var instance = new GameObject("Perk Absorb VFX", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, player.gameObject.scene);
            instance.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            instance.GetComponent<Canvas>().sortingOrder = 1000;
            CanvasGroup group = instance.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            Canvas.ForceUpdateCanvases();
            PerkAbsorbVfx effect = instance.AddComponent<PerkAbsorbVfx>();
            effect.settings = settings;
            effect.Begin(selectedCard, gun, camera);
        }

        private void Begin(Button source, Transform gun, Camera camera)
        {
            // 원본이 비활성화되어도 선택한 카드의 이미지와 글자가 끝까지 이동하도록 복사합니다.
            RectTransform sourceRect = (RectTransform)source.transform;
            Canvas sourceCanvas = source.GetComponentInParent<Canvas>();
            Camera uiCamera = sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? sourceCanvas.worldCamera : null;
            var corners = new Vector3[4];
            sourceRect.GetWorldCorners(corners);
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]);

            RectTransform card = Instantiate(sourceRect, transform, false);
            card.name = "Selected Card (VFX)";
            card.anchorMin = card.anchorMax = Vector2.zero;
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = sourceRect.rect.size;
            card.position = (bottomLeft + topRight) * 0.5f;
            card.rotation = sourceRect.rotation;
            card.localScale = new Vector3(
                Vector2.Distance(bottomLeft, RectTransformUtility.WorldToScreenPoint(uiCamera, corners[3])) / Mathf.Max(1f, sourceRect.rect.width),
                Vector2.Distance(bottomLeft, RectTransformUtility.WorldToScreenPoint(uiCamera, corners[1])) / Mathf.Max(1f, sourceRect.rect.height), 1f);
            foreach (Selectable selectable in card.GetComponentsInChildren<Selectable>()) selectable.enabled = false;
            foreach (Graphic graphic in card.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
            CanvasGroup fade = card.GetComponent<CanvasGroup>();
            if (fade == null) fade = card.gameObject.AddComponent<CanvasGroup>();
            fade.interactable = false;
            fade.blocksRaycasts = false;
            StartCoroutine(Animate(card, fade, gun, camera));
        }

        private IEnumerator Animate(RectTransform card, CanvasGroup fade, Transform gun, Camera camera)
        {
            Vector3 start = card.position;
            Vector3 scale = card.localScale;
            Renderer[] gunRenderers = gun.GetComponentsInChildren<Renderer>();
            // 2026-11-09: 코드로 설정한 값도 범위를 보정합니다. 재생 시간 변경은 다음 연출부터 반영합니다.
            float absorbDuration = Mathf.Max(0.01f, settings.absorbDuration);
            float flashDuration = Mathf.Max(0f, settings.flashDuration);
            float elapsed = 0f;
            while (elapsed < absorbDuration)
            {
                if (gun == null || camera == null) { Destroy(gameObject); yield break; }
                float t = Mathf.Clamp01(elapsed / absorbDuration);
                float progress = Mathf.Pow(t, Mathf.Max(0.1f, settings.absorbPower));
                Vector3 center = gun.position;
                foreach (Renderer renderer in gunRenderers)
                {
                    if (renderer == null || !renderer.enabled) continue;
                    center = renderer.bounds.center;
                    break;
                }
                Vector3 destination = camera.WorldToScreenPoint(center);
                if (destination.z <= 0f) { Destroy(gameObject); yield break; }
                destination.z = 0f;
                // 초반에는 카드를 읽을 수 있게 두고, 후반에 빠르게 작아지며 총의 현재 위치로 빨려 들어갑니다.
                card.position = Vector3.Lerp(start, destination, progress)
                    + Vector3.up * (Mathf.Sin(t * Mathf.PI) * Screen.height * settings.arcHeightRatio);
                card.localScale = scale * Mathf.Lerp(1f, Mathf.Clamp01(settings.endScale), progress);
                fade.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Mathf.Clamp(settings.fadeStart, 0f, 0.99f), 1f, t));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            card.gameObject.SetActive(false);
            if (gun == null || flashDuration <= 0f) { Destroy(gameObject); yield break; }

            // 별도 메시를 잠깐 겹쳐 총만 발광시킵니다. 원본 sharedMaterial/MaterialPropertyBlock은 건드리지 않습니다.
            Shader shader = Resources.Load<Shader>("VFX/UpgradeFlash");
            if (shader != null)
            {
                flashMaterial = new Material(shader);
                foreach (MeshFilter filter in gun.GetComponentsInChildren<MeshFilter>())
                {
                    MeshRenderer original = filter.GetComponent<MeshRenderer>();
                    if (filter.sharedMesh == null || original == null || !original.enabled) continue;
                    var overlay = new GameObject("Upgrade Flash (VFX)", typeof(MeshFilter), typeof(MeshRenderer));
                    overlays.Add(overlay);
                    overlay.layer = filter.gameObject.layer;
                    overlay.transform.SetParent(filter.transform, false);
                    overlay.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    MeshRenderer renderer = overlay.GetComponent<MeshRenderer>();
                    var materials = new Material[filter.sharedMesh.subMeshCount];
                    for (int i = 0; i < materials.Length; i++) materials[i] = flashMaterial;
                    renderer.sharedMaterials = materials;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
                elapsed = 0f;
                while (elapsed < flashDuration && gun != null)
                {
                    float t = elapsed / flashDuration;
                    float depth = Mathf.Clamp(settings.flashPulseDepth, 0f, 0.5f);
                    float pulse = Mathf.Pow(1f - t, 2f) * (1f - depth
                        + depth * Mathf.Cos(t * Mathf.PI * 2f * Mathf.Max(0f, settings.flashPulseCount)));
                    Color color = settings.flashColor * Mathf.Max(0f, settings.flashIntensity);
                    color.a = Mathf.Clamp01(settings.flashColor.a) * pulse;
                    flashMaterial.SetColor("_FlashColor", color);
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // 씬 전환으로 코루틴이 중단되어도 총의 자식 VFX와 생성한 재질을 정리합니다.
            foreach (GameObject overlay in overlays) if (overlay != null) Destroy(overlay);
            if (flashMaterial != null) Destroy(flashMaterial);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Infobulle qui apparaît au-dessus d'un personnage quand la souris survole sa case.
/// Se construit seule si panel == null. Ajoutez-la via Oracle > Build Character Tooltip.
/// </summary>
public class HpTooltipWidget : MonoBehaviour
{
    [Header("Références (auto si vides)")]
    public Camera cam;

    [Header("Panel (auto-créé si vide)")]
    public RectTransform    panel;
    public TextMeshProUGUI  nameText;
    public TextMeshProUGUI  hpText;

    [Header("Position au-dessus du personnage")]
    [Tooltip("Décalage vertical en unités monde au-dessus du transform du personnage.")]
    public float worldOffsetY = 1.4f;

    Canvas _canvas;

    // ------------------------------------------------------------------ lifecycle

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null) _canvas = FindObjectOfType<Canvas>();

        if (panel == null)
            BuildPanel();
        else
            panel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || GridManager.Instance == null) { Hide(); return; }

        // Visible uniquement pendant la phase de combat
        if (CombatInitializer.Instance != null &&
            CombatInitializer.Instance.CurrentPhase != CombatInitializer.CombatPhase.Combat)
        { Hide(); return; }

        Cell cell = GridManager.Instance.GetCellAtScreenPosition(cam, Input.mousePosition);
        if (cell == null || !cell.IsOccupied) { Hide(); return; }

        var tc = cell.Occupant != null ? cell.Occupant.GetComponent<TacticalCharacter>() : null;
        if (tc == null || tc.stats == null) { Hide(); return; }

        Show(tc);
    }

    // ------------------------------------------------------------------ show / hide

    void Show(TacticalCharacter tc)
    {
        if (panel == null) return;

        // ---- textes ----
        if (nameText != null)
            nameText.text = tc.name;

        if (hpText != null)
        {
            float ratio = tc.stats.maxHP > 0 ? (float)tc.CurrentHP / tc.stats.maxHP : 0f;
            hpText.color = ratio > 0.5f  ? new Color(0.35f, 0.85f, 0.40f, 1f)   // vert
                         : ratio > 0.25f ? new Color(0.90f, 0.75f, 0.10f, 1f)   // jaune
                                         : new Color(0.90f, 0.25f, 0.25f, 1f);  // rouge
            hpText.text = $"{tc.CurrentHP} / {tc.stats.maxHP} PV";
        }

        // ---- position : au-dessus du personnage ----
        Vector3 worldAbove = tc.transform.position + Vector3.up * worldOffsetY;
        Vector3 screenPos  = cam.WorldToScreenPoint(worldAbove);

        // Si le personnage est derrière la caméra, masquer
        if (screenPos.z < 0f) { Hide(); return; }

        Vector2 localPos;
        if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            localPos = new Vector2(screenPos.x, screenPos.y);
        }
        else
        {
            var canvasRt = _canvas != null
                ? _canvas.GetComponent<RectTransform>()
                : panel.parent as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, screenPos, cam, out localPos);
        }

        panel.anchoredPosition = localPos;
        panel.gameObject.SetActive(true);
    }

    void Hide()
    {
        if (panel != null) panel.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ auto-construction du panel

    void BuildPanel()
    {
        if (_canvas == null) { Debug.LogWarning("[HpTooltipWidget] Aucun Canvas trouvé."); return; }

        var panelGo = new GameObject("CharacterTooltipPanel", typeof(RectTransform));
        panelGo.transform.SetParent(_canvas.transform, false);

        panel       = (RectTransform)panelGo.transform;
        panel.pivot = new Vector2(0.5f, 0f);          // centré horizontalement, ancre en bas
        panel.anchorMin = panel.anchorMax = Vector2.zero;
        panel.sizeDelta = new Vector2(160f, 48f);

        // Fond
        var bg = panelGo.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.09f, 0.88f);

        // Layout vertical
        var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(8, 8, 6, 6);

        // Nom du personnage
        var nameGo  = new GameObject("Name", typeof(RectTransform));
        nameGo.transform.SetParent(panelGo.transform, false);
        nameText = nameGo.AddComponent<TextMeshProUGUI>();
        nameText.fontSize    = 13f;
        nameText.fontStyle   = FontStyles.Bold;
        nameText.color       = new Color(0.788f, 0.659f, 0.298f, 1f); // or du HUD
        nameText.alignment   = TextAlignmentOptions.Center;
        nameText.raycastTarget = false;
        nameGo.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // Points de vie
        var hpGo  = new GameObject("HP", typeof(RectTransform));
        hpGo.transform.SetParent(panelGo.transform, false);
        hpText = hpGo.AddComponent<TextMeshProUGUI>();
        hpText.fontSize    = 11f;
        hpText.color       = new Color(0.35f, 0.85f, 0.40f, 1f);
        hpText.alignment   = TextAlignmentOptions.Center;
        hpText.raycastTarget = false;
        hpGo.AddComponent<LayoutElement>().flexibleWidth = 1f;

        panel.gameObject.SetActive(false);
    }
}

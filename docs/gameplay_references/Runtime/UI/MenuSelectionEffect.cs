using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpiderManRebuild
{
    [RequireComponent(typeof(Selectable))]
    public sealed class MenuSelectionEffect : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        public TMP_Text Label;
        public Image Accent;
        public string Description;
        public MainMenuManager Owner;
        Vector3 initialScale;
        bool selected;
        void Awake() { initialScale = transform.localScale; }
        public void OnSelect(BaseEventData data)
        { selected = true; if (Owner != null) Owner.Highlight(this); }
        public void OnDeselect(BaseEventData data) => selected = false;
        public void OnPointerEnter(PointerEventData data)
        {
            if (GetComponent<Selectable>().IsInteractable() && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }
        void Update()
        {
            float blend = 1 - Mathf.Exp(-18 * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, initialScale * (selected ? 1.045f : 1), blend);
            if (Label != null) Label.color = Color.Lerp(Label.color, selected ? new Color(1, .86f, .12f) : Color.white, blend);
            if (Accent != null) Accent.color = Color.Lerp(Accent.color,
                selected ? new Color(1, .78f, .08f, 1) : new Color(.8f, .12f, .12f, .4f), blend);
        }
    }
}

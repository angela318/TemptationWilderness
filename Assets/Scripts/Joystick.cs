using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Knob")]
    public RectTransform knob;
    public float maxRadius = 80f;

    Vector2 _input = Vector2.zero;
    public float Horizontal => _input.x;
    public float Vertical   => _input.y;

    void Awake()
    {
        gameObject.SetActive(false); // hidden by default; React enables on mobile
    }

    public void OnPointerDown(PointerEventData data) => UpdateKnob(data);
    public void OnDrag(PointerEventData data)        => UpdateKnob(data);

    public void OnPointerUp(PointerEventData data)
    {
        _input = Vector2.zero;
        knob.anchoredPosition = Vector2.zero;
    }

    void UpdateKnob(PointerEventData data)
    {
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, data.position, data.pressEventCamera, out local);
        local  = Vector2.ClampMagnitude(local, maxRadius);
        knob.anchoredPosition = local;
        _input = local / maxRadius;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

public class Clickable : MonoBehaviour, IPointerClickHandler
{
    public UnityEngine.Events.UnityEvent onClicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        onClicked?.Invoke(); // Unity calls this automatically
    }
}
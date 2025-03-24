using EditorAttributes;
using UnityEngine;
using UnityEngine.EventSystems;

public class MovingButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Info")]
    [ReadOnly][SerializeField] private Vector3 _StartingPos;

    [Header("Animate")]
    [SerializeField] private float _MoveBackTimer = 1.0f;
    public bool MoveAwayActive = true;
    public void DisableMoveAway() => MoveAwayActive = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _StartingPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (!MoveAwayActive) return;
        transform.position = Vector3.Lerp(transform.position, _StartingPos, _MoveBackTimer * Time.deltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!MoveAwayActive) return;
        Vector3 mousePos = eventData.position;
        Vector3 difPos = transform.position - mousePos;
        transform.position += difPos;
    }

    public void OnPointerExit(PointerEventData eventData)
    {

    }
}

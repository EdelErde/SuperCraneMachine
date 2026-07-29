using UnityEngine;

public class ToggleActive : MonoBehaviour
{
    [SerializeField] private GameObject target;

    public void Toggle() => (target != null ? target : gameObject).ToggleActive();
    public void Show()   => (target != null ? target : gameObject).SetActive(true);
    public void Hide()   => (target != null ? target : gameObject).SetActive(false);
}
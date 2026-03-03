using UnityEngine;

public class PipeTokenSelector : MonoBehaviour
{
    [SerializeField] private GameObject[] tokenVariants;

    private void OnEnable()
    {
        ApplyToken();
    }

    private void ApplyToken()
    {
        if (tokenVariants == null || tokenVariants.Length == 0) return;
        if (GameManager.instance == null) return;

        int index = Mathf.Clamp(
            GameManager.instance.SelectedIndex,
            0,
            tokenVariants.Length - 1
        );

        for (int i = 0; i < tokenVariants.Length; i++)
        {
            if (tokenVariants[i] != null)
                tokenVariants[i].SetActive(i == index);
        }
    }
}
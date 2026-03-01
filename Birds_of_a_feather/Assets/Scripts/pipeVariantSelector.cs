using System;
using UnityEngine;

public class PipeVariantSelector : MonoBehaviour
{
    [SerializeField] private GameObject[] upperVariants;
    [SerializeField] private GameObject[] lowerVariants;

    private void Awake()
    {
        // turn all off
        foreach (var go in upperVariants) go.SetActive(false);
        foreach (var go in lowerVariants) go.SetActive(false);

        // pick one of each
        if (upperVariants.Length > 0)
            upperVariants[UnityEngine.Random.Range(0, upperVariants.Length)].SetActive(true);

        if (lowerVariants.Length > 0)
            lowerVariants[UnityEngine.Random.Range(0, lowerVariants.Length)].SetActive(true);
    }
}
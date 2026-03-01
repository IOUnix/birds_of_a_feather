using UnityEngine;

public class LoopGround : MonoBehaviour
{


    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        UnityEngine.Debug.Log("Ground Tile World Width: " + sr.bounds.size.x);
    }

}
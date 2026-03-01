using UnityEngine;

public class GroundScroller : MonoBehaviour
{
    [SerializeField] private Transform[] tiles; // size 2
    [SerializeField] private float speed = 2f;

    [Header("Looping")]
    [SerializeField] private float loopWidth = 3.934f;   // <-- your desired width
    [SerializeField] private float wrapX = -1f;          // <-- x position where a tile wraps (matches your Tile A start)

    private void Update()
    {
        float move = speed * Time.deltaTime;

        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i].position += Vector3.left * move;

            if (tiles[i].position.x <= wrapX - loopWidth)
            {
                tiles[i].position += Vector3.right * (loopWidth * tiles.Length);
            }
        }
    }
}
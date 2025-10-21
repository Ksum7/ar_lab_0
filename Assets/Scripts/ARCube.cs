using UnityEngine;

public class ARCube : MonoBehaviour
{
    private Renderer renderer;
    private Color[] colors = { Color.red, Color.blue, Color.green, Color.yellow };
    private int colorIndex = 0;

    void Start()
    {
        renderer = GetComponent<Renderer>();
    }

    public void ChangeColor()
    {
        colorIndex = (colorIndex + 1) % colors.Length;
        renderer.material.color = colors[colorIndex];
    }

}
using UnityEngine;

public class GenerateFieldVisualization : MonoBehaviour
{
    // A script to generate a field visualization. I kept this instead of just using sprites in case the user wants to toggle on/off to a different color, or
    // to have a dynamically resizable field without having to call resize directly on multiple sprites.
    #region Properties
    [Tooltip("Add this script if you want to procedurally generate a field visualization using a specified sprite renderer. You can assign one or both ends of the field.")]
    public SpriteRenderer PosEnd;
    public SpriteRenderer NegEnd;

    public Color PosColor = Color.blue;
    public Color NegColor = Color.red;

    // Property because we want to be able to change this dynamically and rescale both ends when changed.
    public float Size
    {
        get { return size; }
        set
        {
            if (value != size)
            {
                // Repeat because we need to set before generating the visual
                size = value;
                Resize();
            }
            else
            {
                size = value;
            }
        }
    }

    private float size;
    private Vector3 posInitLocalScale;
    private Vector3 negInitLocalScale;
    #endregion

    private void Awake ()
    {
        // Initialize scale and size.
        if(PosEnd)
        {
            posInitLocalScale = PosEnd.transform.localScale;
        }

        if(NegEnd)
        {
            negInitLocalScale = NegEnd.transform.localScale;
        }

        // Magic number: Don't let the field be invisible, set the size to 1.
        Size = 1.0f;

        GenerateFieldVisual();
	}

    // Assign field visual colors and resize.
    private void GenerateFieldVisual()
    {
        if(PosEnd)
        {
            PosEnd.color = PosColor;
        }

        if (NegEnd)
        {
            NegEnd.color = NegColor;
        }

        Resize();
    }

    // Scale the objects accordingly.
    private void Resize()
    {
        if(PosEnd)
        {
            PosEnd.transform.localScale = posInitLocalScale * size;
        }
        if (NegEnd)
        {
            NegEnd.transform.localScale = negInitLocalScale * size;
        }
    }
}
using System.Collections;
using UnityEngine;

public class DestroyAfterSeconds : MonoBehaviour
{
    [SerializeField] public float duration = 1.2f;

    private void Start()
    {
        Destroy(gameObject, duration);
    }
}


using UnityEngine;

public class Rotate : MonoBehaviour
{
    public float speed;
    public Vector3 direction;
    
    void Update()
    {
        transform.Rotate(direction, speed * Time.deltaTime);
    }
}

using UnityEngine;
public class CloudDrift : MonoBehaviour {
    public float speed = .25f;
    void Update() {
        transform.position += Vector3.right * (speed * Time.deltaTime);
        if (transform.position.x > 60) transform.position = new Vector3(-60, transform.position.y, transform.position.z);
    }
}

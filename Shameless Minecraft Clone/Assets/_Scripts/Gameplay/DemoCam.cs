using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoCam : MonoBehaviour
{
    float x = 0;
    float y = 0;

    public float speedX = 4f;
    public float lengthX = 3f;
    public float powerX = 10f;
    public float speedY = 3;

    // Start is called before the first frame update
    void Start()
    {
        x = Random.Range(0, 100);
        y = Random.Range(-180, 180);
    }

    // Update is called once per frame
    void Update()
    {
        x += speedX * Time.deltaTime;
        float sinX = Mathf.Sin(x * lengthX) * powerX;
        y += speedY * Time.deltaTime;

        transform.rotation = Quaternion.Euler(new Vector3(sinX, y,0));
    }
}

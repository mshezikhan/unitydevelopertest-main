using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform player;


    void LateUpdate()
    {
        // Take ONLY player's X and Z rotation (ignore Y/player facing)
        Vector3 playerEuler = player.eulerAngles;
        Vector3 cameraEuler = transform.eulerAngles;

        // Copy player's X and Z rotation only
        cameraEuler.x = playerEuler.x;
        cameraEuler.z = playerEuler.z;

        transform.eulerAngles = cameraEuler;
    }
}

using UnityEngine;
[CreateAssetMenu(fileName = "Missile Data", menuName = "Scriptable Objects/Data/Missile Data", order = 3)]
public class MissileDataSO : BulletDataSO
{
    public float initialVerticalSpeed;
    public float curveAcceleration;
    public float verticalAcceleration;
    public float startChasingTime;
    public float canDamageHeight;

}

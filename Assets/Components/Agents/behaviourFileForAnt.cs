using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class behaviourFileForAnt : MonoBehaviour
{
   public float moveSpeed = 2f;
   private Vector3 moveDirection;
   public float changeDirectionInterval = 2f;
   private float changeDirectionTimer;

   private float minX, maxX, minZ, maxZ;

   void Start (){
    changeDirectionTimer = changeDirectionInterval;

    minX = 0;
    maxX = 100;
    minZ = 0;
    maxZ = 100;

    ChooseNewDirection();
   }

   void Update(){
    transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);

    changeDirectionTimer -= Time.deltaTime;
    if(changeDirectionTimer <= 0){
        ChooseNewDirection();
        changeDirectionTimer = changeDirectionInterval;
    }
    transform.position = new Vector3(
        Mathf.Clamp(transform.position.x, minX, maxX),
        transform.position.y, 
        Mathf.Clamp(transform.position.z, minZ, maxZ)
    );
   }

   void ChooseNewDirection(){
    float randomAngle = Random.Range(0f, 360f);
    moveDirection = new Vector3(Mathf.Cos(randomAngle * Mathf.Deg2Rad), 0f, Mathf.Sin(randomAngle * Mathf.Deg2Rad));

    transform.rotation = Quaternion.LookRotation(moveDirection);
   }
}

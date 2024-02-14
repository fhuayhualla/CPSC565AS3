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
   
   private Rigidbody rb;

   void Start (){
    rb = GetComponent<Rigidbody>();
    changeDirectionTimer = changeDirectionInterval;
    
    ChooseNewDirection();
   }

   void Update(){
    MoveAnt();

    if (changeDirectionTimer <= 0){
        ChooseNewDirection();
        changeDirectionTimer = changeDirectionInterval;
    }
    else{
        changeDirectionTimer -= Time.fixedDeltaTime;
    }
    var clampedX = Mathf.Clamp(rb.position.x, minX, maxX);
    var clampedZ = Mathf.Clamp(rb.position.z, minZ, maxZ);
    rb.position = new Vector3(clampedX, rb.position.y, clampedZ);
   }

   void MoveAnt(){
    Vector3 newPosition = rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
    rb.MovePosition(newPosition);
   }

   void ChooseNewDirection(){
    float randomAngle = Random.Range(0f, 360f);
    moveDirection = new Vector3(Mathf.Cos(randomAngle * Mathf.Deg2Rad), 0, Mathf.Sin(randomAngle * Mathf.Deg2Rad));

    transform.rotation = Quaternion.LookRotation(moveDirection);
   }

   void OnCollisionEnter(Collision collision){
    ChooseNewDirection();
   }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerController : MonoBehaviour {

    [Range(0, 1)] public float movementSpeed;
    [Range(0, 5)] public float rotationSpeed;

    public GameObject sword;
    public Material blueTransparency, blueTint;
    public ParticleSystem warpTrails;
    public Shader blueTransparencyShader;
    public Material[] originalSword;
    public GameObject aim, locked;

    public GameObject cineMachine;

    private float moveHor, moveFor, warpDuration = 0.5f;
    private Animator animControl;
    private CharacterController charControl;
    private Vector3 swordPosition, aimOffset;
    private Quaternion swordRotation;
    private Transform swordParent;
    private GameObject objClosest; 


    void Start () {

        FindTarget();
        aimOffset = new Vector3(0, 1, -.5f);
        aim = Instantiate(aim, objClosest.transform.position + aimOffset, objClosest.transform.rotation);
        locked = Instantiate(locked, objClosest.transform.position + aimOffset, objClosest.transform.rotation);

        locked.SetActive(false);

        swordParent = sword.transform.parent;
        swordPosition = new Vector3(0.16f, -0.118f, 0.599f);
        swordRotation = Quaternion.Euler(-12.9f, 90, 0);
        animControl = GetComponent<Animator>();
        charControl = GetComponent<CharacterController>();

        sword.SetActive(false);
	}

	void Update () {

        FindTarget();

        if (!animControl.GetBool("isAttacking"))                                // The player is currently not attacking.
            Movement();

        aim.transform.position = objClosest.transform.position + aimOffset;
        aim.transform.rotation = objClosest.transform.rotation; 

        Combat();
	}
    
    private void Combat()
    {
        if (Input.GetButton("Fire1") && !animControl.GetBool("isAttacking"))
        {
            locked.transform.SetPositionAndRotation(objClosest.transform.position + aimOffset, objClosest.transform.rotation);
            locked.SetActive(true);

            sword.SetActive(true);
            animControl.SetBool("isAttacking", true);
        }
    }

    // Called when Slash animation begins.
    private void CallOutSword()
    {
        MeshRenderer swordMesh = sword.GetComponentInChildren<MeshRenderer>();

        for (int i = 0; i < 4; i++)
            swordMesh.materials[i].DOFloat(0, "_AlphaThreshold", 1f);


        sword.SetActive(true);
    }

    // Called when Slash animation starts slash.
    private void Warp()
    {
        cineMachine.GetComponent<Cinemachine.CinemachineImpulseSource>().GenerateImpulse();

        Vector3 enemyLocation = objClosest.transform.position;
        Vector3 tempTarget = new Vector3(enemyLocation.x, 1, enemyLocation.z);
        MeshRenderer swordMesh = sword.GetComponentInChildren<MeshRenderer>();

        for (int i = 0; i < 4; i++)
        {
            swordMesh.materials[i].shader = originalSword[i].shader;
            swordMesh.materials[i] = originalSword[i];
        }

        warpTrails.Play();

        transform.DOLookAt(enemyLocation, 1f);
        transform.DOMove(enemyLocation, warpDuration);                          // Moves player to the new target.

        sword.transform.parent = null;
        sword.transform.DOMove(tempTarget, warpDuration/2);
        sword.transform.DORotate(new Vector3(0, 90, 0), 0.3f);                  // Rotates sword to be direct.

        CreateProjection();
        ShowBody(false);
    }

    // Called when Slash animation is almost completed.
    private void FinishWarp()
    {
        animControl.SetBool("isAttacking", false);

        SkinnedMeshRenderer[] skinMeshList = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach(SkinnedMeshRenderer smr in skinMeshList)
        {
            smr.material = blueTint;
            smr.material.DOVector(new Vector4(0,0,0,0), "_GlowControl", 1f);
        }

        ShowBody(true);

        sword.transform.SetParent(swordParent);
        sword.transform.localPosition = swordPosition;
        sword.transform.localRotation = swordRotation;

        MeshRenderer swordMesh = sword.GetComponentInChildren<MeshRenderer>();

        for(int i = 0; i < 4; i++)
        {
            swordMesh.materials[i].shader = blueTransparencyShader;
            swordMesh.materials[i] = blueTransparency;
            swordMesh.materials[i].DOFloat(2, "_AlphaThreshold", 1.8f);
        }


        locked.SetActive(false);
    }

    private void ShowBody(bool state)
    {
        SkinnedMeshRenderer[] skinMeshList = GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer smr in skinMeshList)
            smr.enabled = state;
    }

    private void Movement()
    {
        moveHor = Input.GetAxis("Horizontal");
        moveFor = Input.GetAxis("Vertical");

        Vector3 newMovement = new Vector3(0, 0, moveFor) * movementSpeed;       // Obtains the new coordinates, dependent on input
        newMovement = transform.TransformDirection(newMovement);                // Changes new coordinates to their world space coordinates

        if (moveHor != 0 || moveFor != 0)
            animControl.SetBool("isMoving", true);
        else
            animControl.SetBool("isMoving", false);

        charControl.Move(newMovement);                                      
        transform.Rotate(0, moveHor * rotationSpeed, 0);
    }

    private void CreateProjection()
    {
        GameObject projection = Instantiate(this.gameObject, this.transform.position, this.transform.rotation);
        Destroy(projection.GetComponent<Animator>());
        Destroy(projection.GetComponent<PlayerController>());
        Destroy(projection.GetComponent<CharacterController>());

        SkinnedMeshRenderer[] skinMeshList = projection.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer smr in skinMeshList)
        {
            smr.material = blueTransparency;
            smr.material.DOFloat(2, "_AlphaThreshold", 1f).OnComplete(() => Destroy(projection));   // This tells the material that its AlphaThreshold will drop to 2 om 5f             
        }                                                                                           // And when the tween is done, Destroy the projection. I think that's a lambda expression.
    }

    private void FindTarget()
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag("Enemy");
        float dot = -2;

        foreach (GameObject obj in objects)                                                                             // Goes through all objects with the "Enemy" tag.  
        {
            Vector3 localPoint = Camera.main.transform.InverseTransformPoint(obj.transform.position).normalized;        // 
            float test = Vector3.Dot(localPoint, Vector3.forward);
            if (test > dot)
            {
                dot = test;
                objClosest = obj;

                
            }
        }

    }

}

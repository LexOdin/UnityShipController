using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipBotController : MonoBehaviour
{

    public GameObject targetObject;

    private state mystate = state.idle;

    public LayerMask targettingMask;
    public LayerMask collisionMask;
    public LayerMask pathingMask;

    public Rigidbody myRB;
    public Animator myAnim;

    public float bankSpeed = 1.5f;

    private float checkTimer = 0f;
    private float nextCheck = 0.5f;

    private float moveTimer = 0f;
    private float endMove = 3.0f;

    private float moveLerp = 0f;
    private float resetLerp = 1.0f;

    private float diveTimer = 0f;
    private float nextDive = 2.0f;


    private bool lockedRotation = false;
    private Vector3 diveTarget;
    private Vector3 moveTarget;
    private GameObject lastMovePoint;
    //private Vector3 moveStart;
    private bool resetMove = true;

    private List<Vector3> bezPoints = new List<Vector3>();

    public List<GameObject> cubes = new List<GameObject>();
    public GameObject cube;

    public GameObject stateCube;

    // Start is called before the first frame update
    void Start()
    {
        for (int z = 0; z < 4; z++)
        {
            bezPoints.Add(Vector3.zero);
        }
        moveTarget = Vector3.zero;

        moveTimer = endMove;

    }

    // Update is called once per frame
    void Update()
    {


        switch (mystate)
        {
            case state.moving:
                if (moveTimer >= endMove)
                {
                    if(!Physics.Linecast(transform.position, targetObject.transform.position, collisionMask))
                    {
                        mystate = state.diving;
                        SetDiveTarget();
                        moveTimer = 0f;
                        myRB.drag = 0.1f;
                    }
                    else
                    {
                        moveTimer = 0f;
                    }
                }
                else
                {
                    
                    if (DampenAndReset() == true)
                    {
                        SetSlowDrag();
                        moveTarget = GetValidMoveTarget();
                    }
                    else
                    {
                        myRB.drag = 0.1f;
                        ContinueMove();
                        moveTimer += Time.deltaTime;
                    }


                }
                if (lockedRotation == false)
                {
                    TargetFacing();
                }
                break;



            case state.diving:                
                if (diveTimer >= nextDive)
                {
                    diveTimer = 0f;
                    mystate = state.moving;
                    resetMove = true;
                    lockedRotation = false;
                    SetMediumDrag();
                }
                else
                {
                    
                    
                    if(DampenAndReset() == true)
                    {
                        diveTimer = 0f;
                        mystate = state.moving;
                        resetMove = true;
                        lockedRotation = false;
                        SetSlowDrag();
                    }
                    else
                    {
                        myRB.drag = 0.1f;
                        diveTimer += Time.deltaTime;
                        ContinueDive();
                    }
                    
                }
                if (lockedRotation == false)
                {
                    TargetFacing();
                }
                break;



            case state.idle:
                if (targetObject == null)
                {
                    if (checkTimer >= nextCheck)
                    {
                        GameObject go = DetectNearby();
                        targetObject = DetectNearby();
                        checkTimer = 0f;
                    }
                    else
                    {
                        checkTimer += Time.deltaTime;
                    }
                }
                else
                {
                    mystate = state.moving;
                }
                break;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        BankQuantity();
    }

    private void SetSlowDrag()
    {
        myRB.drag = 1.5f;
    }

    private void SetMediumDrag()
    {
        myRB.drag = 0.5f;
    }

    private void SetDiveTarget()
    {
        diveTarget = targetObject.transform.position;
        Vector3 diff = diveTarget - transform.position;
        Vector3 separator = new Vector3(1, 0, 1);

        float multiplier = diff.magnitude/3;
        if (multiplier > 2.5f)
            multiplier = 2.5f;

        Vector3 unit1 = Random.insideUnitSphere * multiplier;
        unit1.y = 0;
        Vector3 unit2 = Random.insideUnitSphere * multiplier;
        unit2.y = 0;

        bezPoints[0] = transform.position;
        bezPoints[1] = Vector3.Lerp(transform.position, diveTarget, 0.333f) + unit1;
        bezPoints[2] = Vector3.Lerp(transform.position, diveTarget, 0.666f) + unit2;
        bezPoints[3] = diveTarget;

        BezierSetup();

        myRB.isKinematic = false;
        //myAnim.enabled = false;
        //lockedRotation = true;
    }

    private void BezierSetup()
    {
        for (int x = 0; x < 4; x++)
        {
            cubes[x].transform.position = bezPoints[x];
        }
    }

    private void ContinueDive()
    {
        Vector3 dir = CalculateBezierTarget(bezPoints, diveTimer);
        //Vector3 dir = diveTarget - transform.position;
        dir = dir.normalized;

        Debug.DrawRay(transform.position, dir * 2f, Color.red);

        float diveVar = diveTimer / (2 * nextDive) + 0.5f;

        myRB.velocity = dir * 8f * diveVar;

    }

    private void ContinueMove()
    {
        if (moveTarget == Vector3.zero || moveLerp >= 0.95f || Vector3.Distance(transform.position, moveTarget) < 2.0f || resetMove == true)
        {
            moveTarget = GetValidMoveTarget();
            //moveStart = transform.position;
            moveLerp = 0f;
            resetMove = false;
        }
        if(moveLerp <= 0.999999f)
        {
            moveLerp += Time.deltaTime;
        }

        if(moveTarget != Vector3.zero)
        {
            Vector3 dir = (moveTarget - transform.position).normalized;
            //stateCube.transform.position = moveTarget;
            //stateCube.GetComponent<MeshRenderer>().material.color = Color.yellow;


            myRB.AddForce(dir * 3.0f, ForceMode.Force);

            //transform.position = Vector3.Lerp(moveStart, moveTarget, moveLerp);
        }
    }

    private Vector3 GetValidMoveTarget()
    {
        float transit = Random.Range(2.5f, 25f);
        Vector3 temp = Random.insideUnitSphere * 3.0f;
        temp.y = 0;

        Collider[] array = PathingCast();

        List<GameObject> points = GetNearestPathPoint(array);
        if(points == null)
        {
            moveTimer = endMove;
            return Vector3.zero;
        }
        GameObject closest = FindClosestFromSet(points);
        int count = 1;

        moveTarget = closest.transform.position + temp;

        RaycastHit info;
        while (Physics.SphereCast(transform.position, 4.0f,  moveTarget - transform.position, out info, 4.0f, collisionMask) /*|| Vector3.Distance(moveTarget, targetObject.transform.position) > Vector3.Distance(transform.position, targetObject.transform.position)*/)
        {
            if(count > 50)
            {
                return points[0].transform.position;
            }
            transit = Random.Range(2.5f, 4f) * (1/count);
            temp = Random.insideUnitSphere * transit;
            temp.y = 0;
            points = GetNearestPathPoint(array);
            closest = FindClosestFromSet(points);
            moveTarget = closest.transform.position + temp;
            count++;
        }
        
        cube.transform.position = moveTarget;
        return moveTarget;
    }

    private GameObject FindClosestFromSet(List<GameObject> array)
    {
        if(array.Count > 0)
        {
            GameObject closest = array[0];

            for(int x = 0; x < array.Count; x++)
            {
                if(Vector3.Distance(targetObject.transform.position, array[x].transform.position) < Vector3.Distance(targetObject.transform.position, closest.transform.position) && !Physics.Raycast(transform.position, closest.transform.position - transform.position, 5.0f, collisionMask))
                {
                    closest = array[x];
                }
            }
            closest.GetComponent<MeshRenderer>().material.color = Color.green;
            lastMovePoint = closest;
            return closest;
        }
        else
        {
            return null;
        }
    }

    private List<GameObject> GetNearestPathPoint(Collider[] array)
    {
        foreach(Collider col in array)
        {
            col.gameObject.GetComponent<MeshRenderer>().material.color = Color.white;
        }
        List<GameObject> topsix = new List<GameObject>();
        int counter = 0;

        if(array.Length > 0)
        {
            for(int x = 0; x < array.Length && counter < 6 && counter < array.Length; x++)
            {
                GameObject closest = array[x].gameObject;
                for(int y = x + 1; y < array.Length; y++)
                {
                    if(Vector3.Distance(transform.position, closest.transform.position) > Vector3.Distance(transform.position, array[y].transform.position) && !topsix.Contains(array[y].gameObject))
                    {
                        closest = array[y].gameObject;
                    }
                }
                topsix.Add(closest);
                counter++;
            }

            foreach (GameObject go in topsix)
            {
                go.GetComponent<MeshRenderer>().material.color = Color.blue;
            }
            return topsix;
        }
        else
        {
            return null;
        }
    }

    private Collider[] PathingCast()
    {
        Collider[] choices = Physics.OverlapSphere(transform.position, 20f, pathingMask);
        return choices;
    }

    private Vector3 CalculateBezierTarget(List<Vector3> seq, float t)
    {
        t /= nextDive;

        Vector3 bezP = (3 * Mathf.Pow(1 - t, 2) * (seq[1] - seq[0])) + (6 * (1 - t) * t * (seq[2] - seq[1])) + (3 * t * t * (seq[3] - seq[2]));
        cube.transform.position = bezP;

        return bezP;
    }

    private void BankQuantity()
    {

        float bank = CalculateDot(transform.forward, transform.position + myRB.velocity, transform.up);
        if (bank < 0f)
        {
            Vector3 eulerDir = new Vector3(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 30f * myRB.velocity.magnitude / 5f);
            Quaternion quatdir = Quaternion.Euler(eulerDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, quatdir, Time.deltaTime * bankSpeed);
        }
        else
        {
            Vector3 eulerDir = new Vector3(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, -30f * myRB.velocity.magnitude / 5f);
            Quaternion quatdir = Quaternion.Euler(eulerDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, quatdir, Time.deltaTime * bankSpeed);
        }
    }

    private float CalculateDot(Vector3 fwd, Vector3 target, Vector3 up)
    {
        Vector3 perp = Vector3.Cross(fwd, target);
        float dir = Vector3.Dot(perp, up);

        if (dir < 0)
        {
            return -1f;
        }
        else if (dir > 0)
        {
            return 1f;
        }
        else
            return 0f;

    }

    private void TargetFacing()
    {
        if(mystate == state.moving)
        {
            Vector3 facing = moveTarget - transform.position;
            Quaternion dir = Quaternion.LookRotation(facing, Vector3.forward);
            Vector3 eulerdir = dir.eulerAngles;

            eulerdir.z = transform.rotation.eulerAngles.z;
            Quaternion quatdir = Quaternion.Euler(eulerdir);

            transform.rotation = Quaternion.Slerp(transform.rotation, quatdir, Time.deltaTime * 1.25f);
        }
        else
        {
            Vector3 facing = targetObject.transform.position - transform.position;
            Quaternion dir = Quaternion.LookRotation(facing, Vector3.forward);
            Vector3 eulerdir = dir.eulerAngles;

            eulerdir.z = transform.rotation.eulerAngles.z;
            Quaternion quatdir = Quaternion.Euler(eulerdir);

            transform.rotation = Quaternion.Slerp(transform.rotation, quatdir, Time.deltaTime * 2.5f);
        }

    }


    private GameObject DetectNearby()
    {
        //Debug.Log("checking");
        Collider[] array = Physics.OverlapSphere(transform.position, 5f, targettingMask);
        if(array.Length > 0)
        {
            return array[0].gameObject;
        }
        else
        {
            return null;
        }
    }

    private bool DampenAndReset()
    {
        //.Log("Checking Dampen");
        Debug.DrawRay(transform.position, myRB.velocity * 1.0f, Color.yellow);
        RaycastHit info;
        bool check = Physics.Raycast(transform.position, myRB.velocity, out info, myRB.velocity.magnitude * 2.0f,  collisionMask);
        if (check /*|| Vector3.Distance(transform.position, targetObject.transform.position) < 0.3f*/)
        {
            if(info.collider != GetComponent<SphereCollider>())
            {
                Debug.Log("Dampening", info.collider.gameObject);
                //myRB.drag = 10f;
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    private enum state
    {
        idle,
        moving,
        diving,
        divingToMoving
    }
}

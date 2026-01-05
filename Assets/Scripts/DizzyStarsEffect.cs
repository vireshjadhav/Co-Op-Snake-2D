using System.Collections.Generic;
using UnityEngine;


public class DizzyStarsEffect : MonoBehaviour
{
    [Header("Child Particle Systems")]
    [SerializeField] private Transform[] starTransforms;  
    private ParticleSystem[] particleSystems;

    [Header("Effect Settings")]
    [SerializeField] private float effectDuration = 3.0f;
    [SerializeField] private float orbitSpeed = 180f;
    [SerializeField] private float circleRadius = 0.75f;

    [Header("Individual Settings")]
    [SerializeField] private float[] speedMultiplier = {1.0f, 1.2f, 0.8f }; //Different speed
    [SerializeField] private float[] radiusMultiplier = {1.0f, 0.9f, 1.1f}; //Different radii
    [SerializeField] private float[] angleOffsets = {0f, 120f, 240f }; //Evenly spaced at 120 degree 
    [SerializeField] private float starRoatationSpeed = 360f; //Speed for star spinning

    private Transform targetShakeHead;
    private bool isActive = false;
    private float timer = 0f;
    private float orbitTimer = 0f; //Tracks orbit time

    private void Awake()
    {
        //Get all child particle systems
        particleSystems = GetComponentsInChildren<ParticleSystem>();

        //Initialize each system
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Stop();
                ps.Clear();
            }
        }

        //If starTrnsforms not assigned, try to find them
        if (starTransforms == null || starTransforms.Length == 0)
        {
            List<Transform> children = new List<Transform>();
            foreach (Transform child in starTransforms)
            {
                if (child.GetComponent<ParticleSystem>() != null)
                {
                    children.Add(child);
                }
            }
            starTransforms = children.ToArray();
        }

        foreach (Transform star in starTransforms)
        {
            if (star != null)
            {
                star.gameObject.SetActive(false);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isActive || targetShakeHead == null) return;

        //Position parent at snake head
        transform.position = targetShakeHead.position;

        //Update timers
        orbitTimer += Time.deltaTime;
        timer += Time.deltaTime;

        //Rotate each child star in its own orbit
        for (int i = 0; i < starTransforms.Length; i++)
        {
            if (starTransforms[i] == null) continue;

            //Calculate orbit position using orbitTimer (accumulated timer)
            float currentOrbitAngle = (orbitTimer * orbitSpeed * speedMultiplier[i] + angleOffsets[i]);
            float angleRad = currentOrbitAngle * Mathf.Deg2Rad;
            float radius = circleRadius * radiusMultiplier[i];

            //Calculate position in local space
            Vector3 orbitPosition = new Vector3(Mathf.Cos(angleRad) * radius, Mathf.Sin(angleRad) * radius, 0f);

            //Set the star's local position 
            starTransforms[i].localPosition = orbitPosition;

            //Rotate the star around its own Z-axis
            starTransforms[i].Rotate(0, 0, starRoatationSpeed * speedMultiplier[i] * Time.deltaTime);
        }

        if (timer >= effectDuration)
        {
            StopEffect();
        }
    }

    public void StartEffect(Transform snakeHead)
    {
        Debug.Log($"DizzyStarsEffect.StartEffect CALLED!");
        Debug.Log($"Snake head: {snakeHead}, Position: {snakeHead.position}");
        Debug.Log($"Snake count: {starTransforms.Length}");

        targetShakeHead = snakeHead;
        isActive = true;
        timer = 0f;
        orbitTimer = 0f;

        //Position at snake head
        transform.position = snakeHead.position;

        //initialize starting position based on angle offsets
        for (int i = 0; i<starTransforms.Length; i++)
        {
            if (starTransforms[i] == null) continue;

            float angleRad = angleOffsets[i] * Mathf.Deg2Rad;
            float radius = circleRadius * radiusMultiplier[i];

            Vector3 startPosition = new Vector3(Mathf.Cos(angleRad) * radius, Mathf.Sin(angleRad) * radius, 0f);

            starTransforms[i].localPosition = startPosition;
            starTransforms[i].gameObject.SetActive(true);
        }
    }

    public void StopEffect()
    {
        Debug.Log("DizzyStarsEffect.StopEffect()");

        isActive = false;
        timer = 0f;
        orbitTimer = 0f;


        //Hide stars
        foreach (var stars in particleSystems)
        {
            if (stars != null)
            {
                stars.gameObject.SetActive(false);
            }
        }
    }

    public bool IsEffectActive() => isActive;
}

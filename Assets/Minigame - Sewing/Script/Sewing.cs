using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class Sewing : MonoBehaviour
{
    public GameObject sewingPoint;
    public GameObject sewingImpact;

    [Header("UI Management")]
    public ScrollRect scrollRect;
    public GameObject table;
    public Slider objectiveValueSlider;
    public GameObject minigameNotificationPrefab;
    public GameObject impactContainer;
    public GameObject holeContainer;
    public Transform uiContainer;
    public Transform leftCorner;
    public Radishmouse.UILineRenderer lineRenderer;

    [Space(10)]
    public int objectivePinned = 5;
    public float targetScrollPosition = 1.0f;
    public float scrollDuration = 2.0f;
    public float stopDuration = 1.0f;

    private bool isScrolling = true;
    private bool isDown = false;
    public float incrementDuration = 1.0f;
    public Vector2 lineRendererSewingOffest;

    bool isIncrementing = false;

    public List<string> scores = new List<string>();
    public List<Sewing_Hole> holes = new List<Sewing_Hole>();

    Animator anim;

    bool isGameStart = false;
    bool isCountdown = false;


    private Vector3 currentRectTransform;

    public UnityEvent WinListener;
    public UnityEvent LoseListener;


    void Start()
    {
        anim = GetComponent<Animator>();
        SaveInitialRectTransform();
    }

    void Update()
    {
        //------------------------------INI BUAT MULAI GAMENYA ATAU RESTART GAMENYA--------------------------------------
        if(Input.GetKeyDown(KeyCode.R) && !isGameStart && !isCountdown)
        {
            StartCoroutine(StartCountdown());
        }


        if (isGameStart)
        {

            Vector3 localTargetPosition = impactContainer.transform.InverseTransformPoint(sewingPoint.transform.position);
            Vector3 localTargetLeftCorner = impactContainer.transform.InverseTransformPoint(leftCorner.transform.position);
            lineRenderer.points[lineRenderer.points.Count - 1] = new Vector2(localTargetPosition.x + lineRendererSewingOffest.x, localTargetPosition.y + lineRendererSewingOffest.y);

            for(int i = 0; i < impactContainer.transform.childCount; i++)
            {
                Transform impact = impactContainer.transform.GetChild(i);

                if(impact.position.x <= leftCorner.transform.position.x)
                {
                    lineRenderer.points[i] = new Vector2(localTargetLeftCorner.x, lineRenderer.points[i].y);
                }
                
            }

            lineRenderer.SetAllDirty();


            for (int i = 0; i < holes.Count; i++)
            {
                Sewing_Hole h = holes[i];

                if (sewingPoint.transform.position.x > h.rightPoint.position.x)
                {
                    h.hasPass = true;
                }
                else
                {
                    h.hasPass = false;
                }

                if(i == holes.Count - 1 && h.hasPass)
                {
                    isGameStart = false;
                    isScrolling = false;

                    StartCoroutine(SpawnNotification("YOU LOSE", 1.4f));

                    StopCoroutine(AutoScrollRoutine());
                    StopCoroutine(StopAndResumeScroll());

                    LoseListener.Invoke();
                }
            }

        }


        if (Input.GetKeyDown(KeyCode.Space) && isScrolling && isGameStart)
        { 
            isScrolling = false;
            anim.SetTrigger("Down");


            //Down Manager
            foreach(Sewing_Hole h in holes)
            {
                //CHECK IF SEWINGPOINT IS MIDDLE OF HOLE BETWEEN LEFT POINT AND RIGHT POINT
                if(!h.hasPinned && !h.hasPass)
                {
                    if (sewingPoint.transform.position.x >= h.leftPoint.position.x && sewingPoint.transform.position.x <= h.rightPoint.position.x)
                    {
                        float distance = Vector2.Distance(sewingPoint.transform.position, h.transform.position);

                        if (distance < 34)
                        {
                            if (scores.Count < objectivePinned - 1)
                                StartCoroutine(SpawnNotification("PERFECT"));

                            scores.Add("PERFECT");

                        }
                        else
                        {
                            if (scores.Count < objectivePinned - 1)
                                StartCoroutine(SpawnNotification("GOOD"));

                            scores.Add("GOOD");

                        }
                        h.hasPinned = true;

                        StartCoroutine(IncrementSliderSmoothly(1));
                    }
                    else
                    {
                        scores.Add("BAD");

                        if (scores.Count < objectivePinned - 1)
                            StartCoroutine(SpawnNotification("BAD"));

                        StartCoroutine(IncrementSliderSmoothly(1f));
                    }

                    break;
                }

            }
            Invoke("createHoleImpact", 0.12f);


            //CHECK IF GAME IS FINISH
            if (scores.Count >= objectivePinned)
            {
                isGameStart = false;
                isScrolling = false;

                StartCoroutine(SpawnNotification("GAME FINISH"));

                StopCoroutine(AutoScrollRoutine());
                StopCoroutine(StopAndResumeScroll());

                WinListener.Invoke();

            }


            StartCoroutine(StopAndResumeScroll());
        }



    }

    void createHoleImpact()
    {
        GameObject impact = Instantiate(sewingImpact, new Vector2(sewingPoint.transform.position.x, impactContainer.transform.position.y), Quaternion.identity, impactContainer.transform);

        Vector2 impactPosition = impact.GetComponent<RectTransform>().anchoredPosition;
        impactPosition.y = 23;

        lineRenderer.points.Add(impactPosition);
        lineRenderer.SetAllDirty();

    }

    void SaveInitialRectTransform()
    {
        currentRectTransform = holeContainer.transform.position;
    }

    void RestoreToInitialRectTransform()
    {
        holeContainer.transform.position = currentRectTransform;

    }

    IEnumerator StartCountdown()
    {
        lineRenderer.points.Clear();
        lineRenderer.points.Add(new Vector2(0, 0));
        lineRenderer.SetAllDirty();

        foreach(Transform child in impactContainer.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Sewing_Hole h in holes)
        {
            h.hasPass = false;
            h.hasPinned = false;
        }
        objectiveValueSlider.maxValue = objectivePinned;
        objectiveValueSlider.value = 0;
        scores.Clear();
        RestoreToInitialRectTransform();
        isCountdown = true;

        yield return new WaitForSeconds(1);
        StartCoroutine(SpawnNotification("Sewing | MiniGame", 1.4f));
        yield return new WaitForSeconds(2);
        StartCoroutine(SpawnNotification("3"));
        yield return new WaitForSeconds(1);
        StartCoroutine(SpawnNotification("2"));
        yield return new WaitForSeconds(1);
        StartCoroutine(SpawnNotification("1"));
        yield return new WaitForSeconds(1);

        isGameStart = true;
        isScrolling = true;
        isCountdown = false;

        StartCoroutine(SpawnNotification("GAME START"));
        StartCoroutine(AutoScrollRoutine());

    }

    IEnumerator SpawnNotification(string message, float delay = 0.4f)
    {
        GameObject notif = Instantiate(minigameNotificationPrefab, uiContainer);
        notif.GetComponent<TMP_Text>().text = message;

        yield return new WaitForSeconds(delay);
        notif.GetComponent<Animator>().SetTrigger("Hide");

        Destroy(notif, 2);

    }

    IEnumerator AutoScrollRoutine()
    {
        while (true)
        {

            float elapsedTime = 0f;
            float startScrollPos = scrollRect.horizontalNormalizedPosition;

            while (elapsedTime < scrollDuration)
            {
                if (isScrolling)
                {
                    float newPosition = Mathf.Lerp(startScrollPos, targetScrollPosition, elapsedTime / scrollDuration);

                    scrollRect.horizontalNormalizedPosition = newPosition;

                    elapsedTime += Time.deltaTime;

                }
                yield return null;

            }

            scrollRect.horizontalNormalizedPosition = targetScrollPosition;


            yield return new WaitForSecondsRealtime(stopDuration);


            yield return null;
        }
    }

    IEnumerator StopAndResumeScroll()
    {
        isDown = true;

        yield return new WaitForSecondsRealtime(stopDuration);
        isDown = false;

        if(isGameStart)
            isScrolling = true;
    }

    IEnumerator IncrementSliderSmoothly(float incrementAmount = 1)
    {
        isIncrementing = true;

        float startValue = objectiveValueSlider.value;
        float targetValue = Mathf.Clamp(startValue + incrementAmount, 0, objectiveValueSlider.maxValue);
        float currentTime = 0;

        while (currentTime < incrementDuration)
        {
            currentTime += Time.deltaTime;
            float newValue = Mathf.Lerp(startValue, targetValue, currentTime / incrementDuration);
            objectiveValueSlider.value = newValue;

            yield return null;
        }

        objectiveValueSlider.value = targetValue;
        isIncrementing = false;
    }
}

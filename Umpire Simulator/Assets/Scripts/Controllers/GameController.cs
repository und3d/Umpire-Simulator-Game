using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour
{
    public Camera cam;
    public Vector3 originalCamTransform;
    public Quaternion originalCamRotation;
    [SerializeField] private LayerMask ballLayer;
    [SerializeField] private TMP_Text pitchClock;
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform releasePoint;
    [SerializeField] private Transform strikezone;
    [SerializeField] private Transform catchZone;
    [SerializeField] private BoxCollider strikezoneCollider;
    [SerializeField] private UIController uiController;
    [SerializeField] private TMP_Text versionText;
    
    [Header("Gamemode Settings")]
    public bool isPracticeMode;
    public bool isEndlessMode;
    public bool isLevelMode;
    
    [Header("EndlessModeSettings")]
    [SerializeField] private int lives = 3;
    
    [Header("LevelModeSettings")]
    [SerializeField] public int level = -1;
    [SerializeField] private int pitchAmount = -1;
    [SerializeField] private int correctForOneStar = -1;
    [SerializeField] private int correctForTwoStars = -1;
    [SerializeField] private int correctForThreeStars = -1;
    
    [Header("Pitch Probabilities")]
    [SerializeField, Range(0f,1f)] private float strikeProbability            = 0.6f; // overall strike rate
    [SerializeField, Range(0f,1f)] private float cornerAmongBallsProbability  = 0.20f; // among balls, chance it's a corner
    [SerializeField, Range(0f,1f)] private float wildMissProbability          = 0.05f; // among balls, chance distance comes from the "wild" tail
    
    [Header("Pitch Variables")]
    [SerializeField] private float timeBetweenPitches = 5f;
    [SerializeField] private float pitchSpeedUpper = 44f;
    [SerializeField] private float pitchSpeedLower = 34f;
    
    // Near-edge overshoot caps (typical "nibbles")
    [SerializeField, Min(0f)] private float nearMaxBeyondEdgeY = 0.15f; // m above/below top/bottom
    [SerializeField, Min(0f)] private float nearMaxBeyondEdgeZ = 0.15f; // m left/right of sides

    // Wild overshoot caps (how far wild throws can go)
    [SerializeField, Min(0f)] private float wildMaxBeyondEdgeY = 0.50f;
    [SerializeField, Min(0f)] private float wildMaxBeyondEdgeZ = 0.50f;

    // Shape of the distance distributions
    [SerializeField, Min(1f)] private float nearEdgeBiasPower   = 1.375f; // >1 squeezes near 0 (closer to edge)
    [SerializeField, Min(0.1f)] private float wildTailPower     = 0.6f; // <1 pulls toward far end of [nearMax..wildMax]
    
    [Header("Sound Effects")]
    [SerializeField] private AudioSource pitchSound;
    [SerializeField] private AudioSource catchSound;
    [SerializeField] private AudioSource callSound;
    [SerializeField] private AudioClip strikeClip;
    [SerializeField] private AudioClip strikeClipTwo;
    [SerializeField] private AudioClip strikeClipThree;
    [SerializeField] private AudioClip strikeClipFour;
    [SerializeField] private AudioClip ballClip;

    public Vector3 nextPitchLocation;
    private GameObject currentBall;
    private Rigidbody currentBallRigidbody;

    private const float ballRadius = 0.0365f;
    private const bool anyPartCounts = true;
    private Vector3 prevCenter;
    private Vector3 lastPitchLocation;
    private bool hasPrev;
    private bool capturedThisPitch;
    private bool isAudioPaused;
    private int pitchCount;
    private int correctCalls;
    private int starsEarned;
    private InputAction clickAction;
    
    private List<Vector3> currentLevelsPitchLocations = new List<Vector3>();
    private List<bool> currentLevelsCallsCorrect = new List<bool>();
    private List<GameObject> visualPitches = new List<GameObject>();
    
    public Vector3 lastCenterAtEntry;
    public Vector3 lastContactPointWorld;
    public float lastEntryT;
    public bool countdownActive;
    public bool viewingPitch;
    public bool creatingPitches;

    public Vector3 pitchLocationToShow;
    
    private void Start()
    {
        pitchSound.volume = LevelLoader.Instance.sfxVolume;
        callSound.volume = LevelLoader.Instance.sfxVolume;
        catchSound.volume = LevelLoader.Instance.sfxVolume;
        
        originalCamTransform = cam.transform.position;
        originalCamRotation = cam.transform.rotation;
        clickAction = InputSystem.actions.FindAction("LeftClick");

        LevelLoader.Instance.versionText = versionText;
        LevelLoader.Instance.SetVersionText();
        
        var newSeed = (int) DateTime.Now.Ticks;
        Random.InitState(newSeed);
        
        UpdatePitchClock(timeBetweenPitches);
        ArmForNextPitch();

        if (isPracticeMode)
        {
            uiController.ShowPracticeModeUI();
        }
        else if (isEndlessMode)
        {
            uiController.ShowEndlessModeUI();
            uiController.UpdateLivesText(lives);
        }
        else if (isLevelMode)
        {
            LevelLoader.Instance.LoadLevel(out level, out pitchAmount, out correctForOneStar, out correctForTwoStars, out correctForThreeStars);
            
            uiController.UpdateStarScoreText(correctForOneStar, correctForTwoStars, correctForThreeStars);
            uiController.ShowLevelModeUI();
            uiController.UpdateRemainingPitches(pitchAmount);
        }
    }
    
    // Call this before launching a new pitch
    private void ArmForNextPitch()
    {
        capturedThisPitch = false;
        hasPrev = false; // re-bootstrap prevCenter on the next FixedUpdate
        currentBall = null;
        
        // Ran out of Pitches in Level Mode
        if (pitchCount >= pitchAmount && isLevelMode)
        {
            if (correctCalls >= correctForOneStar)      // Player Beat Level
            {
                starsEarned = 1;
                if (correctCalls >= correctForTwoStars)     // Check if player earned second star
                    starsEarned = 2;
                {
                    if (correctCalls >= correctForThreeStars)       // Check if player earned third star
                        starsEarned = 3;
                }
                uiController.GameOver(true, correctCalls, starsEarned);
                LevelLoader.Instance.UnlockLevel(level);    // Level number in list is -1 compared to actual level number
                return;
            }
            uiController.GameOver(false, correctCalls, starsEarned);
        }
        
        // Ran out of Lives in Endless Mode
        if (lives <= 0 && isEndlessMode)
        {
            uiController.GameOver(false, correctCalls, starsEarned);
            return;
        }
        
        countdownActive = true;
        
        pitchSound.Play();
        StartCoroutine(PitchClockTimer());
    }

    private void PitchBall()
    {
        countdownActive = false;
        pitchCount++;
        uiController.UpdatePitchCountText(pitchCount);
        uiController.UpdateRemainingPitches(pitchAmount - pitchCount);
        
        currentBall = Instantiate(ballPrefab, releasePoint.position, Quaternion.identity);
        var pitchVelocity = GetRandomPitchVelocity();
        var pitchLocation = GetRandomPitchLocation();
        var launchVelocity = GetLaunchVelocity(pitchVelocity, releasePoint.position, pitchLocation);
        currentBall.TryGetComponent(out currentBallRigidbody);
        currentBallRigidbody.linearVelocity = launchVelocity;

        hasPrev = true;
        prevCenter = currentBallRigidbody.position - currentBallRigidbody.linearVelocity * (0.5f * Time.fixedDeltaTime);
        
        currentBallRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }
    
    private IEnumerator PitchClockTimer()
    {
        var timer = timeBetweenPitches;
        while (timer >= 0.5)
        {
            yield return new WaitForSeconds(0.5f);
            timer -= 0.5f;
            UpdatePitchClock(timer);
        }
        
        PitchBall();
        yield return null;
    }

    private void FixedUpdate()
    {
        if (!currentBall || !strikezone || !catchZone) return;
        
        // Current sphere center from the physics engine (meters)
        var nowCenter = currentBallRigidbody.position;

        // First tick bootstrap
        if (!hasPrev)
        {
            hasPrev = true;
            
            var v = currentBallRigidbody.linearVelocity;
            prevCenter = nowCenter + v * (0.5f * Time.fixedDeltaTime);
        }
        if (capturedThisPitch) { prevCenter = nowCenter; return; }

        // Define the vertical strike plane: point p0 and unit normal n (yaw-aware)
        var p0Strike = strikezone.position;
        var p0Catch = catchZone.position;

        var nStrike = GetPlateNormalStrikezone();
        var step = nowCenter - prevCenter;
        if (Vector3.Dot(step, nStrike) < 0f) nStrike = -nStrike;
        
        var nCatch = GetPlateNormalCatchZone();
        if (Vector3.Dot(step, nCatch) < 0f) nCatch = -nCatch;
        
        // Check if we crossed the plane this physics step and recover exact entry
        if (TryGetPlaneEntryPoint(prevCenter, nowCenter, p0Strike, nStrike, ballRadius, anyPartCounts,
                out var centerAtEntry, out var contactPointOnPlane, out var t))
        {
            // Record results (meters, world space)
            lastCenterAtEntry     = centerAtEntry;
            lastContactPointWorld = contactPointOnPlane;
            lastEntryT            = t;
            
            // Where the ball center will cross the plane
            var sPrev0 = Vector3.Dot(prevCenter - p0Strike, nStrike);
            var sNow0  = Vector3.Dot(nowCenter  - p0Strike, nStrike);
            var denom0 = sNow0 - sPrev0;
            var centerOnPlane = centerAtEntry;
            if (Mathf.Abs(denom0) > 1e-6f)
            {
                var tCenter = Mathf.Clamp01((0f - sPrev0) / denom0);
                centerOnPlane = Vector3.Lerp(prevCenter, nowCenter, tCenter);
            }
            
            // Mark captured; if you want to spawn a frozen ball elsewhere, do it here
            
            lastPitchLocation = centerOnPlane;
            currentLevelsPitchLocations.Add(centerOnPlane);
        }

        if (TryGetPlaneEntryPoint(prevCenter, nowCenter, p0Catch, nCatch, ballRadius, anyPartCounts, out centerAtEntry,
                out contactPointOnPlane, out t))
        {
            capturedThisPitch = true;
            
            uiController.ShowGameButtons();
            uiController.gameButtonsActive = true;
            
            Debug.Log("Crossed catch zone.");
            
            catchSound.Play();
            Destroy(currentBall);
        }

        // Prepare for next physics step
        prevCenter = nowCenter;
    }

    private void Update()
    {
        if (clickAction.WasPressedThisFrame() && uiController.showingLevelPitches)
        {
            var pos = Mouse.current != null ? Mouse.current.position.ReadValue() : Touchscreen.current.primaryTouch.position.ReadValue();
            var ray = cam.ScreenPointToRay(pos);
            
            if (Physics.Raycast(ray, out var hit, 25f, ballLayer))
            {
                Debug.Log("Clicked on: " + hit.collider.gameObject.name);
                hit.collider.gameObject.GetComponent<Baseball>().ShowPitch();
            }
        }
    }

    private IEnumerator ShowPitchLocations()
    {
        creatingPitches = true;
        var index = 0;
        var pitchesToShow = currentLevelsPitchLocations.Count;
        var delayInterval = 10 / pitchesToShow;
        if (delayInterval > 1)
        {
            delayInterval = 1;
        }
        
        yield return new WaitForSecondsRealtime(delayInterval);
        
        foreach (var location in currentLevelsPitchLocations)
        {
            visualPitches.Add(CreateBallAtLocationlevels(location, index, currentLevelsCallsCorrect[index]));
            index++;
            
            yield return new WaitForSecondsRealtime(delayInterval);
        }
        creatingPitches = false;
    }

    public GameObject ShowLastPitchLocation()
    {
        return CreateBallAtLocation(pitchLocationToShow, IsStrike(pitchLocationToShow));
    }

    private GameObject CreateBallAtLocation(Vector3 location, bool isStrike)
    {
        var ball = Instantiate(ballPrefab, location, Quaternion.identity);
        ball.GetComponentInChildren<Renderer>().material.color = isStrike ? Color.indianRed : Color.cadetBlue;
        
        return ball;
    }
    
    private GameObject CreateBallAtLocationlevels(Vector3 location, int pitchNumber, bool correctCall)
    {
        var ball = Instantiate(ballPrefab, location, Quaternion.identity);
        ball.transform.rotation = Quaternion.Euler(0, -90, 0);
        ball.GetComponentInChildren<Renderer>().material.color = correctCall ? Color.lawnGreen : Color.indianRed;
        ball.TryGetComponent(out Baseball baseballScript);
        baseballScript.pitchNumText.enabled = true;
        baseballScript.pitchNumText.text = (pitchNumber + 1).ToString();
        
        return ball;
    }

    public void CallStrike()
    {
        PlayStrikeCall();
        
        if (IsStrike(lastPitchLocation))
        {
            correctCalls++;
            uiController.UpdateCorrectCallsText(correctCalls);
            if (isLevelMode)
            {
                currentLevelsCallsCorrect.Add(true);
            }
        }
        else
        {
            lives--;
            uiController.UpdateLivesText(lives);
            if (isLevelMode)
            {
                currentLevelsCallsCorrect.Add(false);
            }
        }

        pitchLocationToShow = lastPitchLocation;
        uiController.HideGameButtons();
        uiController.gameButtonsActive = false;
        ArmForNextPitch();
    }

    public void CallBall()
    {
        PlayBallCall();
        
        if (!IsStrike(lastPitchLocation))
        {
            correctCalls++;
            uiController.UpdateCorrectCallsText(correctCalls);
            if (isLevelMode)
            {
                currentLevelsCallsCorrect.Add(true);
            }
        }
        else
        {
            lives--;
            uiController.UpdateLivesText(lives);
            if (isLevelMode)
            {
                currentLevelsCallsCorrect.Add(false);
            }
        }
        
        pitchLocationToShow = lastPitchLocation;
        uiController.HideGameButtons();
        uiController.gameButtonsActive = false;
        ArmForNextPitch();
    }

    public void PauseAudio()
    {
        if (!pitchSound.isPlaying) 
            return;
        
        pitchSound.Pause();
        isAudioPaused = true;
    }

    public void ResumeAudio()
    {
        if (!isAudioPaused) 
            return;
        
        pitchSound.UnPause();
        isAudioPaused = false;
    }

    #region Helpers

    private void PlayStrikeCall()
    {
        var callID = Random.Range(0, 4);

        callSound.clip = callID switch
        {
            0 => strikeClip,
            1 => strikeClipTwo,
            2 => strikeClipThree,
            3 => strikeClipFour,
            _ => strikeClip
        };
        
        callSound.Play();
    }

    private void PlayBallCall()
    {
        callSound.clip = ballClip;
        callSound.Play();
    }
    
    private float GetRandomPitchVelocity()
    {
        return Random.Range(pitchSpeedLower, pitchSpeedUpper);
    }
    
    private bool IsStrike(Vector3 contactPoint)
    {
        var t = strikezoneCollider.transform;
        var c = t.TransformPoint(strikezoneCollider.center);

        var halfY = 0.5f * strikezoneCollider.size.y * Mathf.Abs(t.lossyScale.y);
        var halfZ = 0.5f * strikezoneCollider.size.z * Mathf.Abs(t.lossyScale.z);

        // Closest point on the rectangle (in the plate plane) to the circle center
        var closestY = Mathf.Clamp(contactPoint.y, c.y - halfY, c.y + halfY);
        var closestZ = Mathf.Clamp(contactPoint.z, c.z - halfZ, c.z + halfZ);

        var dy = contactPoint.y - closestY;
        var dz = contactPoint.z - closestZ;

        // Intersects if distance from center to rect ≤ ball radius
        return (dy * dy + dz * dz) <= (ballRadius * ballRadius);
    }
    
    static int RandSign() => (Random.value < 0.5f) ? -1 : +1;

    // 0..nearMax with mass near 0 (edge nibbles)
    float SampleNear(float nearMax) {
        return Mathf.Pow(Random.value, nearEdgeBiasPower) * Mathf.Max(0f, nearMax);
    }

    // [nearMax..wildMax] with mass toward wildMax (big misses)
    float SampleWild(float nearMax, float wildMax) {
        float u = Random.value;
        // 1 - (1-u)^p biases toward 1 when p<1
        float t = 1f - Mathf.Pow(1f - u, wildTailPower);
        return Mathf.Lerp(Mathf.Min(nearMax, wildMax), Mathf.Max(nearMax, wildMax), t);
    }

    // Pick distance past an edge using the mixture
    float SampleOvershoot(float nearMax, float wildMax) {
        return (Random.value < wildMissProbability)
            ? SampleWild(nearMax, wildMax)
            : SampleNear(nearMax);
    }

    #endregion

    #region Math Methods

    private Vector3 GetPlateNormalStrikezone()
    {
        var n = strikezone.position - releasePoint.position;
        n.y = 0f;
        return n.sqrMagnitude < 1e-6f ? Vector3.left : n.normalized;
    }
    
    private Vector3 GetPlateNormalCatchZone()
    {
        var n = catchZone.position - releasePoint.position;
        n.y = 0f;
        return n.sqrMagnitude < 1e-6f ? Vector3.left : n.normalized;
    }
    
    private Vector3 GetRandomPitchLocation()
    { 
        var plateX = strikezone.position.x;
        
        // Strike zone center & half-extents (WORLD)
        var t = strikezoneCollider.transform;
        var c = t.TransformPoint(strikezoneCollider.center);
        var halfY = 0.5f * strikezoneCollider.size.y * Mathf.Abs(t.lossyScale.y);
        var halfZ = 0.5f * strikezoneCollider.size.z * Mathf.Abs(t.lossyScale.z);
        
        var extraY = 0.005f;
        var extraZ = 0.005f;
        
        var clearanceY = (ballRadius + extraY);
        var clearanceZ = (ballRadius + extraZ);
        
        // Overall clamp region using the WILD caps
        var yMin = c.y - halfY - (clearanceY + wildMaxBeyondEdgeY);
        var yMax = c.y + halfY + (clearanceY + wildMaxBeyondEdgeY);
        var zMin = c.z - halfZ - (clearanceZ + wildMaxBeyondEdgeZ);
        var zMax = c.z + halfZ + (clearanceZ + wildMaxBeyondEdgeZ);

        float y, z;

        // 1) STRIKE or BALL?
        if (Random.value < strikeProbability)
        {
            y = Random.Range(c.y - halfY, c.y + halfY);
            z = Random.Range(c.z - halfZ, c.z + halfZ);
        }
        else
        {
            // 2) BALL → corner (both axes out) or edge (one axis out)?
            var corner = (Random.value < cornerAmongBallsProbability);

            if (corner)
            {
                var sY = RandSign();                         // high / low
                var sZ = RandSign();                         // right / left
                var dY = SampleOvershoot(nearMaxBeyondEdgeY, wildMaxBeyondEdgeY);
                var dZ = SampleOvershoot(nearMaxBeyondEdgeZ, wildMaxBeyondEdgeZ);

                y = c.y + sY * (halfY + clearanceY + dY);
                z = c.z + sZ * (halfZ + clearanceZ + dZ);
            }
            else
            {
                var vertical = (Random.value < 0.5f);       // equal chance: high/low OR left/right

                if (vertical)
                {
                    var sY = RandSign();
                    var dY = SampleOvershoot(nearMaxBeyondEdgeY, wildMaxBeyondEdgeY);
                    y = c.y + sY * (halfY + clearanceY + dY);
                    z = Random.Range(c.z - halfZ, c.z + halfZ);
                }
                else
                {
                    var sZ = RandSign();
                    var dZ = SampleOvershoot(nearMaxBeyondEdgeZ, wildMaxBeyondEdgeZ);
                    z = c.z + sZ * (halfZ + clearanceZ + dZ);
                    y = Random.Range(c.y - halfY, c.y + halfY);
                }
            }

            // Keep within reasonable bounds
            y = Mathf.Clamp(y, yMin, yMax);
            z = Mathf.Clamp(z, zMin, zMax);
        }

        nextPitchLocation = new Vector3(plateX, y, z);
        return nextPitchLocation;
    }
    
    private static Vector3 GetLaunchVelocity(float pitchVelocity, Vector3 startpoint, Vector3 endpoint)
    {
        var deltaX = endpoint.x - startpoint.x;
        var deltaY = endpoint.y - startpoint.y;
        var deltaZ = endpoint.z - startpoint.z;
        var pitchDistanceHorizontal = Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        var groundDistance = new Vector3(deltaX, 0, deltaZ) / pitchDistanceHorizontal;
        var gravity = -Physics.gravity.y;
        var pitchVelocitySquared = pitchVelocity * pitchVelocity;
        var pitchVelocityFourthP = pitchVelocitySquared * pitchVelocitySquared;
        var pitchDistanceHorizSquared = pitchDistanceHorizontal * pitchDistanceHorizontal;

        var tan = (pitchVelocitySquared - Mathf.Sqrt(pitchVelocityFourthP - gravity * (gravity * pitchDistanceHorizSquared + 2 * deltaY * pitchVelocitySquared))) / (gravity * pitchDistanceHorizontal);
        var cos = 1 / Mathf.Sqrt(1 + tan * tan);
        var sin = tan * cos;
        
        var launchVelocity = (pitchVelocity * cos) * groundDistance + (pitchVelocity * sin) * Vector3.up;
        
        return launchVelocity;
    }
    
    private static bool TryGetPlaneEntryPoint(
        Vector3 prevCenter, Vector3 nowCenter,
        Vector3 planePointP0, Vector3 planeNormalN,
        float sphereRadius, bool anyPartCountsBool,
        out Vector3 centerAtEntry, out Vector3 contactPointOnPlane, out float fraction01)
    {
        // Ensure a vertical plane normal (no Y) and normalize.
        planeNormalN.y = 0f;
        if (planeNormalN.sqrMagnitude < 1e-6f)
        {
            centerAtEntry = contactPointOnPlane = default;
            fraction01 = 0f;
            return false;
        }
        var n = planeNormalN.normalized;

        // Signed distances of the sphere center to the plane at prev/now.
        var sPrev = Vector3.Dot(prevCenter - planePointP0, n);
        var sNow  = Vector3.Dot(nowCenter  - planePointP0, n);

        // Choose what "counts" as crossing.
        var threshold = anyPartCountsBool ? -sphereRadius : 0f;

        // Did we cross the threshold this step? (allow either direction of travel)
        var a = sPrev - threshold;
        var b = sNow  - threshold;
        var denom = sNow - sPrev;
        if ((a > 0f && b > 0f) || (a < 0f && b < 0f) || Mathf.Abs(denom) < 1e-6f)
        {
            centerAtEntry = contactPointOnPlane = default;
            fraction01 = 0f;
            return false;
        }

        // Exact fraction along the prev→now segment where s(t) == threshold.
        fraction01 = Mathf.Clamp01((threshold - sPrev) / denom);
        centerAtEntry = Vector3.Lerp(prevCenter, nowCenter, fraction01);

        // Contact point on the plane (the sphere's front-most point at first touch).
        contactPointOnPlane = anyPartCountsBool ? centerAtEntry + n * sphereRadius
            : centerAtEntry;

        return true;
    }

    #endregion
    
    #region UI Handlers

    private void UpdatePitchClock(float timeLeft)
    {
        if (timeLeft >= 1)
        {
            timeLeft = Mathf.FloorToInt(timeLeft % 60);

            pitchClock.text = $"{timeLeft:0}";
        }
        else
        {
            pitchClock.text = " ";
        }
    }

    public void NextLevel()
    {
        LevelLoader.Instance.AdvanceLevel(level);
    }

    public void ShowPitches()
    {
        foreach (var ball in visualPitches)
            Destroy(ball);
        
        uiController.showingLevelPitches = true;
        uiController.DisableViews();
        strikezone.rotation = Quaternion.Euler(0f, 180f, 0f);
        StartCoroutine(ShowPitchLocations());
    }

    #endregion
}

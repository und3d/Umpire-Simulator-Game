using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Baseball : MonoBehaviour
{
    public TMP_Text pitchNumText;
    public Transform cameraTransform;
    
    public GameController gameController;
    private InputAction goBack;

    private bool isViewing;
    
    private void Awake()
    {
        gameController = FindAnyObjectByType(typeof(GameController)).GetComponent<GameController>();
        pitchNumText.enabled = false;
        goBack = InputSystem.actions.FindAction("Pause");
    }

    private void Update()
    {
        if (goBack.WasPressedThisFrame() && isViewing)
        {
            isViewing = false;
            gameController.viewingPitch = false;
            StopAllCoroutines();
            StartCoroutine(MoveOverSeconds(gameController.cam.transform, gameController.cam.transform.position, gameController.originalCamTransform, gameController.cam.transform.rotation, gameController.originalCamRotation, 1f));
        }
    }

    public void ShowPitch()
    {
        if (gameController.creatingPitches)
            return;
        
        StopAllCoroutines();
        isViewing = true;
        gameController.viewingPitch = true;
        StartCoroutine(MoveOverSeconds(gameController.cam.transform, gameController.cam.transform.position, cameraTransform.position, gameController.cam.transform.rotation, cameraTransform.rotation, 1f));
    }

    private IEnumerator MoveOverSeconds(Transform target, Vector3 from, Vector3 to, Quaternion rotFrom, Quaternion rotTo, float seconds)
    {
        var elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            var lerpPos = Mathf.Clamp01(elapsed / seconds);
            var lerpRot = Quaternion.Lerp(rotFrom, rotTo, lerpPos);
            target.position = Vector3.Lerp(from, to, lerpPos);
            target.rotation = lerpRot;
            yield return null;
        }
    }
}

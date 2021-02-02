﻿using System;
using UnityEngine;
using Mirror;

public class PlayerLook : NetworkBehaviourNonAlloc
{
    [Header("Components")]
    public IKHandling ik;
    public PlayerCharacterControllerMovement movement;
    public Health health;
#pragma warning disable CS0109 // member does not hide accessible member
    new Camera camera;
#pragma warning restore CS0109 // member does not hide accessible member

    float lastClientSendTime;

    [Header("Camera")]
    public float XSensitivity = 2;
    public float YSensitivity = 2;
    public float MinimumX = -90;
    public float MaximumX = 90;

    // head position is useful for raycasting etc.
    public Transform firstPersonParent;
    public Vector3 headPosition => firstPersonParent.position;
    public Transform freeLookParent;

    Vector3 originalCameraPosition;

    public KeyCode freeLookKey = KeyCode.LeftAlt;

    // need to sync free looking state so that we don't move other player's
    // arms and bodies while they are free looking
    [SyncVar] bool syncedIsFreeLooking;

    // the layer mask to use when trying to detect view blocking
    // (this way we dont zoom in all the way when standing in another entity)
    // (-> create a entity layer for them if needed)
    public LayerMask viewBlockingLayers;
    public float zoomSpeed = 0.5f;
    public float distance = 0;
    public float minDistance = 0;
    public float maxDistance = 7;

    [Header("Physical Interaction")]
    [Tooltip("Layers to use for raycasting. Check Default, Walls, Player, Monster, Doors, Interactables, Item, etc. Uncheck IgnoreRaycast, AggroArea, Water, UI, etc.")]
    public LayerMask raycastLayers = Physics.DefaultRaycastLayers;

    // camera offsets. Vector2 because we only want X (left/right) and Y (up/down)
    // to be modified. Z (forward/backward) should NEVER be modified because
    // then we could look through walls when tilting our head forward to look
    // downwards, etc. This can be avoided in the camera positioning logic, but
    // is way to complex and not worth it at all.
    [Header("Offsets - Standing")]
    public Vector2 firstPersonOffsetStanding = Vector2.zero;
    public Vector2 thirdPersonOffsetStanding = Vector2.up;
    public Vector2 thirdPersonOffsetStandingMultiplier = Vector2.zero;

    [Header("Offsets - Crouching")]
    public Vector2 firstPersonOffsetCrouching = Vector2.zero;
    public Vector2 thirdPersonOffsetCrouching = Vector2.up / 2;
    public Vector2 thirdPersonOffsetCrouchingMultiplier = Vector2.zero;
    // scale offset by distance so that 100% zoom in = first person and
    // zooming out puts camera target slightly above head for easier aiming
    public float crouchOriginMultiplier = 0.65f;

    [Header("Offsets - Crawling")]
    public Vector2 firstPersonOffsetCrawling = Vector2.zero;
    public Vector2 thirdPersonOffsetCrawling = Vector2.up;
    public Vector2 thirdPersonOffsetCrawlingMultiplier = Vector2.zero;
    // scale offset by distance so that 100% zoom in = first person and
    // zooming out puts camera target slightly above head for easier aiming
    public float crawlOriginMultiplier = 0.65f;

    [Header("Offsets - Swimming")]
    public Vector2 firstPersonOffsetSwimming = Vector2.zero;
    public Vector2 thirdPersonOffsetSwimming = Vector2.up;
    public Vector2 thirdPersonOffsetSwimmingMultiplier = Vector2.zero;
    // scale offset by distance so that 100% zoom in = first person and
    // zooming out puts camera target slightly above head for easier aiming
    public float swimOriginMultiplier = 0.65f;

    // look directions /////////////////////////////////////////////////////////
    // * for first person, all we need is the camera.forward
    //
    // * for third person, we need to raycast where the camera looks and then
    //   calculate the direction from the eyes.
    //   BUT for animations we actually only want camera.forward because it
    //   looks strange if we stand right in front of a wall, camera aiming above
    //   a player's head (because of head offset) and then the players arms
    //   aiming at that point above his head (on the wall) too.
    //     => he should always appear to aim into the far direction
    //     => he should always fire at the raycasted point
    //   in other words, if we want 1st and 3rd person WITH camera offsets, then
    //   we need both the FAR direction and the RAYCASTED direction
    //
    // * we also need to sync it over the network to animate other players.
    //   => we compress it as far as possible to save bandwidth. syncing it via
    //      rotation bytes X and Y uses 2 instead of 12 bytes per observer(!)
    //
    // * and we can't only calculate and store the values in Update because
    //   ShoulderLookAt needs them live in LateUpdate, Update is too far behind
    //   and would cause the arms to be lag behind a bit.
    //
    [SyncVar, HideInInspector] public byte syncedLookDirectionFarXrot;
    [SyncVar, HideInInspector] public byte syncedLookDirectionFarYrot;
    Vector3 syncedLookDirectionFar
    {
        get
        {
            // reconstruct it based on the one synced bytes
            return UncompressDirection(syncedLookDirectionFarXrot, syncedLookDirectionFarYrot);
        }
    }

    public Vector3 lookDirectionFar
    {
        get
        {
            return isLocalPlayer ? camera.transform.forward : syncedLookDirectionFar;
        }
    }

    //[SyncVar, HideInInspector] Vector3 syncedLookDirectionRaycasted; not needed atm, see lookPositionRaycasted comment
    public Vector3 lookDirectionRaycasted
    {
        get
        {
            // same for local and other players
            // (positionRaycasted uses camera || syncedDirectionRaycasted anyway)
            return (lookPositionRaycasted - headPosition).normalized;
        }
    }

    // the far position, directionFar projected into nirvana
    public Vector3 lookPositionFar
    {
        get
        {
            Vector3 position = isLocalPlayer ? camera.transform.position : headPosition;
            return position + lookDirectionFar * 9999f;
        }
    }

    // the raycasted position is needed for lookDirectionRaycasted calculation
    // and for firing, so we might as well reuse it here
    public Vector3 lookPositionRaycasted
    {
        get
        {
            if (isLocalPlayer)
            {
                // raycast based on position and direction, project into nirvana if nothing hit
                // (not * infinity because might overflow depending on position)
                // -> without self to ignore own body parts etc.
                return Utils.RaycastWithout(camera.transform.position, camera.transform.forward, out RaycastHit hit, Mathf.Infinity, gameObject, raycastLayers)
                       ? hit.point
                       : lookPositionFar;
            }
            else
            {
                // the only person to need the raycast direction is the local player (right now).
                // we use the far direction for other player's animations and we pass a the
                // raycast direction to the server when using items. there should be no reason to
                // sync those 12 extra bytes over the network, so let's just show an error here
                // => can still use the below code if necessary some day
                //return Utils.RaycastWithout(firstPersonParent.position, syncedLookDirectionRaycasted, out hit, Mathf.Infinity, gameObject, raycastLayers)
                //       ? hit.point
                //       : lookPositionFar;
                Debug.LogError("PlayerLook.lookPositionRaycasted isn't synced so you can only call it on the local player right now.\n" + Environment.StackTrace);
                return Vector3.zero;
            }
       }
    }

    void Awake()
    {
        camera = Camera.main;
    }

    void Start()
    {
        if (isLocalPlayer) // TODO use OnStartLocalPlayer instead?
        {
            // set camera parent to player
            camera.transform.SetParent(transform, false);

            // look into player forward direction, which was loaded from the db
            camera.transform.rotation = transform.rotation;

            // set camera to head position
            camera.transform.position = headPosition;
        }

        // remember original camera position
        originalCameraPosition = camera.transform.localPosition;
    }

    [Command]
    void CmdSetLookDirection(byte rotX, byte rotY)//, Vector3 directionRaycasted)
    {
        //syncedLookDirectionFar = directionFar;
        //syncedLookDirectionRaycasted = directionRaycasted; <- not needed atm, see syncPositionRaycasted comment
        syncedLookDirectionFarXrot = rotX;
        syncedLookDirectionFarYrot = rotY;
    }

    [Command]
    void CmdSetFreeLooking(bool state)
    {
        syncedIsFreeLooking = state;
    }

    // lookDirection compression ///////////////////////////////////////////////
    // all we really need on the server is the Y (right/left) and X (up/down)
    // rotation to calculate the direction, since head can't be tilted
    // (X is only needed if we want to sync free look too, otherwise we could
    //  use transform.rotation.y)
    // (syncing the direction with x,y,z is easier, but needs more bytes)
    static void CompressDirection(Vector3 direction, out byte xRotation, out byte yRotation)
    {
        // make a rotation from the direction
        Quaternion rotation = Quaternion.LookRotation(direction);

        // convert to euler
        Vector3 euler = rotation.eulerAngles;

        // compress into two bytes
        xRotation = FloatBytePacker.ScaleFloatToByte(euler.x, 0, 360, byte.MinValue, byte.MaxValue);
        yRotation = FloatBytePacker.ScaleFloatToByte(euler.y, 0, 360, byte.MinValue, byte.MaxValue);
    }

    static Vector3 UncompressDirection(byte xRotation, byte yRotation)
    {
        // uncompress the bytes
        float x = FloatBytePacker.ScaleByteToFloat(xRotation, byte.MinValue, byte.MaxValue, 0, 360);
        float y = FloatBytePacker.ScaleByteToFloat(yRotation, byte.MinValue, byte.MaxValue, 0, 360);

        // make rotation
        Quaternion rotation = Quaternion.Euler(x, y, 0);

        // convert back to direction
        return rotation * Vector3.forward;
    }

    ////////////////////////////////////////////////////////////////////////////
    void Update()
    {
        if (!isLocalPlayer) return;

        // send only each 'sendinterval', otherwise we send at whatever
        // the player's tick rate is, which is like DDOS
        // (SendInterval doesn't seem to apply to Cmd, so we have to do
        //  it manually)
        if (Time.time - lastClientSendTime >= syncInterval)
        {
            // sync direction if changed
            CompressDirection(lookDirectionFar, out byte x, out byte y);
            if (x != syncedLookDirectionFarXrot || y != syncedLookDirectionFarYrot)
                CmdSetLookDirection(x, y); //, lookDirectionRaycasted);

            // sync free look state if changed
            bool freeLooking = IsFreeLooking();
            if (freeLooking != syncedIsFreeLooking)
                CmdSetFreeLooking(freeLooking);

            lastClientSendTime = Time.time;
        }

        // only while alive and while cursor is locked, otherwise we are in a UI
        if (health.current > 0 && Cursor.lockState == CursorLockMode.Locked)
        {
            // calculate horizontal and vertical rotation steps
            float xExtra = Input.GetAxis("Mouse X") * XSensitivity;
            float yExtra = Input.GetAxis("Mouse Y") * YSensitivity;

            // use mouse to rotate character
            // (but use camera freelook parent while climbing so player isn't rotated
            //  while climbing)
            // (no free look in first person)
            if (movement.state == MoveState.CLIMBING ||
                (Input.GetKey(freeLookKey) && !UIUtils.AnyInputActive() && distance > 0))
            {
                // set to freelook parent already?
                if (camera.transform.parent != freeLookParent)
                    InitializeFreeLook();

                // rotate freelooktarget for horizontal, rotate camera for vertical
                freeLookParent.Rotate(new Vector3(0, xExtra, 0));
                camera.transform.Rotate(new Vector3(-yExtra, 0, 0));
            }
            else
            {
                // set to player parent already?
                if (camera.transform.parent != transform)
                    InitializeForcedLook();

                // rotate character for horizontal, rotate camera for vertical
                transform.Rotate(new Vector3(0, xExtra, 0));
                camera.transform.Rotate(new Vector3(-yExtra, 0, 0));
            }
        }
    }

    // Update camera position after everything else was updated
    void LateUpdate()
    {
        if (!isLocalPlayer) return;

        // clamp camera rotation automatically. this way we can rotate it to
        // whatever we like in Update, and LateUpdate will correct it.
        camera.transform.localRotation = Utils.ClampRotationAroundXAxis(camera.transform.localRotation, MinimumX, MaximumX);

        // zoom after rotating, otherwise it won't be smooth and would overwrite
        // each other.

        // zoom should only happen if not in a UI right now
        if (!Utils.IsCursorOverUserInterface())
        {
            float step = Utils.GetZoomUniversal() * zoomSpeed;
            distance = Mathf.Clamp(distance - step, minDistance, maxDistance);
        }

        // calculate target and zoomed position
        if (distance == 0) // first person
        {
            // we use the current head bone position as origin here
            // -> gets rid of the idle->run head change effect that was odd
            // -> gets rid of upper body culling issues when looking downwards
            Vector3 headLocal = transform.InverseTransformPoint(headPosition);
            Vector3 origin = Vector3.zero;
            Vector3 offset = Vector3.zero;

            if (movement.state == MoveState.CROUCHING)
            {
                origin = headLocal * crouchOriginMultiplier;
                offset = firstPersonOffsetCrouching;
            }
            else if (movement.state == MoveState.CRAWLING)
            {
                origin = headLocal * crawlOriginMultiplier;
                offset = firstPersonOffsetCrawling;
            }
            else if (movement.state == MoveState.SWIMMING)
            {
                origin = headLocal;
                offset = firstPersonOffsetSwimming;
            }
            else
            {
                origin = headLocal;
                offset = firstPersonOffsetStanding;
            }

            // set final position
            Vector3 target = transform.TransformPoint(origin + offset);
            camera.transform.position = target;
        }
        else // third person
        {
            Vector3 origin = Vector3.zero;
            Vector3 offsetBase = Vector3.zero;
            Vector3 offsetMult = Vector3.zero;

            if (movement.state == MoveState.CROUCHING)
            {
                origin = originalCameraPosition * crouchOriginMultiplier;
                offsetBase = thirdPersonOffsetCrouching;
                offsetMult = thirdPersonOffsetCrouchingMultiplier;
            }
            else if (movement.state == MoveState.CRAWLING)
            {
                origin = originalCameraPosition * crawlOriginMultiplier;
                offsetBase = thirdPersonOffsetCrawling;
                offsetMult = thirdPersonOffsetCrawlingMultiplier;
            }
            else if (movement.state == MoveState.SWIMMING)
            {
                origin = originalCameraPosition * swimOriginMultiplier;
                offsetBase = thirdPersonOffsetSwimming;
                offsetMult = thirdPersonOffsetSwimmingMultiplier;
            }
            else
            {
                origin = originalCameraPosition;
                offsetBase = thirdPersonOffsetStanding;
                offsetMult = thirdPersonOffsetStandingMultiplier;
            }

            Vector3 target = transform.TransformPoint(origin + offsetBase + offsetMult * distance);
            Vector3 newPosition = target - (camera.transform.rotation * Vector3.forward * distance);

            // avoid view blocking (only third person, pointless in first person)
            // -> always based on original distance and only overwrite if necessary
            //    so that we dont have to zoom out again after view block disappears
            // -> we cast exactly from cam to target, which is the crosshair position.
            //    if anything is inbetween then view blocking changes the distance.
            //    this works perfectly.
            float finalDistance = distance;
            Debug.DrawLine(target, camera.transform.position, Color.white);
            if (Physics.Linecast(target, newPosition, out RaycastHit hit, viewBlockingLayers))
            {
                // calculate a better distance (with some space between it)
                finalDistance = Vector3.Distance(target, hit.point) - 0.1f;
                Debug.DrawLine(target, hit.point, Color.red);
            }
            else Debug.DrawLine(target, newPosition, Color.green);

            // set final position
            camera.transform.position = target - (camera.transform.rotation * Vector3.forward * finalDistance);
        }
    }

    public bool InFirstPerson()
    {
        return distance == 0;
    }

    // free look mode //////////////////////////////////////////////////////////
    public bool IsFreeLooking()
    {
        if (isLocalPlayer)
        {
            return camera != null && // camera isn't initialized while loading players in charselection
                   camera.transform.parent == freeLookParent;
        }
        return syncedIsFreeLooking;
    }

    public void InitializeFreeLook()
    {
        camera.transform.SetParent(freeLookParent, false);
        freeLookParent.localRotation = Quaternion.identity; // initial rotation := where we look at right now

        // disable body ik while free looking. only the head should turn.
        // feels best, especially when holding weapons
        ik.lookAtBodyWeightActive = false;
    }

    public void InitializeForcedLook()
    {
        camera.transform.SetParent(transform, false);
        ik.lookAtBodyWeightActive = true;
    }

    // debugging ///////////////////////////////////////////////////////////////
    void OnDrawGizmos()
    {
        if (!isLocalPlayer) return;

        // draw camera forward
        Gizmos.color = Color.white;
        Gizmos.DrawLine(headPosition, camera.transform.position + camera.transform.forward * 9999f);

        // draw all the different look positions
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(headPosition, lookPositionFar);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(headPosition, lookPositionRaycasted);
    }
}

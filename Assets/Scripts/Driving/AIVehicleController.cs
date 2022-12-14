/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum LanePositionType
{
    Center,
    Left,
    Right
};

public class AIVehicleController : AgentController
{
    #region vars
    // input
    public float SteerInput;
    public float AccelBrakeInput;

    private float actualLinVel = 0f; //same unit as target velocity
    private float actualAngVel = 0f; //same unit as target angular velocity
    private Vector3 simLinVel;
    private Vector3 simAngVel;
    private Vector3 projectedLinVec;
    private Vector3 projectedAngVec;
    private float lastUpdate = 0f;

    //all car wheel info
    public List<AxleInfo> axles;

    private Transform carCenter;

    //public WheelController steerWheel;

    //torque at peak of torque curve
    public float maxMotorTorque;

    //torque at max brake
    public float maxBrakeTorque;

    //steering range is [-maxSteeringAngle, maxSteeringAngle]
    public float maxSteeringAngle;

    //idle rpm
    public float minRPM = 1500;

    //maxiumum RPM
    public float maxRPM = 8000;

    //gearbox ratios
    public float finalDriveRatio = 2.56f;
    public float[] gearRatios;

    //minimum time between any 2 gear changes
    public float shiftDelay = 1.0f;

    //time it takes for a gear shift to complete (interpolated)
    public float shiftTime = 0.4f;

    //torque curve that gives torque at specific percentage of max RPM;
    public AnimationCurve rpmCurve;

    //curves controlling whether to shift up/down at specific rpm, based on throttle position
    public AnimationCurve shiftUpCurve;
    public AnimationCurve shiftDownCurve;

    //drag coefficients
    public float airDragCoeff = 2.0f;
    public float airDownForceCoeff = 4.0f;
    public float tireDragCoeff = 500.0f;

    //wheel collider damping rate
    public float wheelDamping = 2f;

    //autosteer helps the car maintain its heading
    [Range(0, 1)]
    public float autoSteerAmount;

    //traction control limits torque based on wheel slip - traction reduced by amount when slip exceeds the tractionControlSlipLimit
    [Range(0, 1)]
    public float tractionControlAmount;
    public float tractionControlSlipLimit;

    //rigidbody center of mass
    public Transform centerOfMassT;
    private Vector3 centerOfMass;

    //how much to smooth out real RPM
    public float RPMSmoothness = 2f;

    //combined throttle and brake input
    public float accellInput = 0f;

    //steering input
    public float steerInput = 0f;

    //turn signals
    public bool leftTurnSignal = false;
    public bool rightTurnSignal = false;
    private bool resetTurnSignal = false;

    //handbrake
    public bool handbrakeApplied = false;

    public CommandFlags commandFlags;
    public IgnitionStatus ignitionStatus = IgnitionStatus.On;
    public DriveMode driveMode = DriveMode.Controlled;

    public float cruiseTargetSpeed;

    public bool EngineRunning
    {
        get { return ignitionStatus == IgnitionStatus.On; }
        set { ignitionStatus = value ? IgnitionStatus.On : IgnitionStatus.Off; }
    }

    public float RPM { get { return currentRPM; } }
    public float currentRPM { get; private set; }
    public int Gear { get; private set; } = 1;
    public bool IsShifting { get; private set; } = false;
    public WheelCollider WheelFL { get { return axles[0].left; } }
    public WheelCollider WheelFR { get { return axles[0].right; } }
    public WheelCollider WheelRL { get { return axles[1].left; } }
    public WheelCollider WheelRR { get { return axles[1].right; } }

    public float MotorWheelsSlip
    {
        get
        {
            float slip = 0f;
            int i = 0;
            foreach (var axle in axles)
            {
                if (axle.motor)
                {
                    i += 2;
                    if (axle.isGroundedLeft)
                        slip += axle.hitLeft.forwardSlip;
                    if (axle.isGroundedRight)
                        slip += axle.hitRight.forwardSlip;
                }
            }
            return slip / i;
        }
    }

    public RoadSurface CurrentSurface { get; private set; }
    public float CurrentSpeed { get; private set; } = 0.0f;

    private float lastShift = 0.0f;

    

    private Rigidbody rb;
    private int numberOfDrivingWheels;

    private float oldRotation;
    private float tractionControlAdjustedMaxTorque;
    private float currentTorque = 0;

    private const float LOW_SPEED = 5f;
    private const float LARGE_FACING_ANGLE = 50f;

    private float currentGear = 1;
    private int lastGear = 1;
    public float traction = 0f;
    public float tractionR = 0f;
    public float rtraction = 0f;
    public float rtractionR = 0f;
    public string roadType;
    private float fuelCapacity = 60.0f;
    private float fuelLevel = 60.0f;

    private const float zeroK = 273.15f;
    public float ambientTemperatureK;
    public float engineTemperatureK;
    public bool coolingMalfunction = false;

    private CarHeadlights headlights;
    public int headlightMode;

    public float lowBeamAngle = 30f;
    public float highBeamAngle = 10f;
    public float lowBeamIntensity = 1f;
    public float highBeamIntensity = 2f;
    public LightMode lightMode = LightMode.LOWBEAM;

    public float separationAngle = 5f;
    public Light left;
    public Light right;
    public Light left_tail;
    public Light right_tail;

    public float Odometer { get; private set; } = 0.0f;
    public float ConsumptionDistance { get; private set; } = 0.0f;
    public float ConsumptionTime { get; private set; } = 0.0f;

    public float FuelLevelFraction
    {
        get { return fuelLevel / fuelCapacity; }
        set { fuelLevel = fuelCapacity * value; }
    }

    public bool InReverse { get; private set; }

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public bool isScenario = false;
    public int currentLane;
    public LanePositionType lanePosition = LanePositionType.Center;
    private int laneChangeStartLane;

    public bool isLaneChange = false;
    public int laneChange = 0;

    public float distToTestVehicle = 0;
    public float dot;
    #endregion

    #region mono
    private void Awake()
    {
        lastUpdate = Time.time;
        if (left)
        {
            left.enabled = false;
            left.gameObject.SetActive(false);
        }
        if (right)
        {
            right.enabled = false;
            right.gameObject.SetActive(false);
        }
        if (left_tail)
        {
            left_tail.enabled = false;
            left_tail.gameObject.SetActive(false);
        }
        if (right_tail)
        {
            right_tail.enabled = false;
            right_tail.gameObject.SetActive(false);
        }

        centerOfMass = centerOfMassT.localPosition;
    }

    private void OnEnable()
    {
        rb = gameObject.GetComponent<Rigidbody>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        RecalcDrivingWheels();

        tractionControlAdjustedMaxTorque = maxMotorTorque - (tractionControlAmount * maxMotorTorque);

        axles[0].left.ConfigureVehicleSubsteps(5.0f, 30, 10);
        axles[0].right.ConfigureVehicleSubsteps(5.0f, 30, 10);
        axles[1].left.ConfigureVehicleSubsteps(5.0f, 30, 10);
        axles[1].right.ConfigureVehicleSubsteps(5.0f, 30, 10);

        foreach (var axle in axles)
        {
            axle.left.wheelDampingRate = wheelDamping;
            axle.right.wheelDampingRate = wheelDamping;
        }

        ambientTemperatureK = zeroK + 22.0f; // a pleasant 22° celsius ambient
        engineTemperatureK = zeroK + 22.0f;
    }

    private void Update()
    {
        // get inputs
        //GetAIInput();

        // scenario
        GetLane();
        GetDistanceToTestVehicle();
        GetDot();

        CheckForEvent();

        if (isLaneChange)
            LaneChange(laneChange);
        else
        {
            laneChangeStartLane = currentLane;
            CenterStraight();
        }
        
        // apply inputs
        ApplyAIInput();

        // lights
        if (GetState())
        {
            left.transform.localRotation = Quaternion.Euler(lightMode == LightMode.LOWBEAM ? lowBeamAngle : highBeamAngle, -separationAngle, 0f);
            right.transform.localRotation = Quaternion.Euler(lightMode == LightMode.LOWBEAM ? lowBeamAngle : highBeamAngle, separationAngle, 0f);

            left.intensity = lightMode == LightMode.LOWBEAM ? lowBeamIntensity : highBeamIntensity;
            right.intensity = lightMode == LightMode.LOWBEAM ? lowBeamIntensity : highBeamIntensity;
        }

        // control
        if (rb.centerOfMass != centerOfMass)
            rb.centerOfMass = centerOfMass;

        if (axles[0].left.wheelDampingRate != wheelDamping)
        {
            foreach (var axle in axles)
            {
                axle.left.wheelDampingRate = wheelDamping;
                axle.right.wheelDampingRate = wheelDamping;
            }
        }

        //update wheel visual component rotations
        foreach (var axle in axles)
        {
            ApplyLocalPositionToVisuals(axle.left, axle.leftVisual);
            ApplyLocalPositionToVisuals(axle.right, axle.rightVisual);
        }

        if (driveMode == DriveMode.Cruise)
        {
            accellInput = GetAccel(CurrentSpeed, cruiseTargetSpeed, Time.deltaTime);
        }

        ProcessIncomingCommandFlags();
    }

    public void FixedUpdate()
    {
        //ResetInputs();

        //air drag (quadratic)
        rb.AddForce(-airDragCoeff * rb.velocity * rb.velocity.magnitude);

        //downforce (quadratic)
        rb.AddForce(-airDownForceCoeff * rb.velocity.sqrMagnitude * transform.up);

        //tire drag (Linear)
        rb.AddForceAtPosition(-tireDragCoeff * rb.velocity, transform.position);

        //calc current gear ratio
        float gearRatio = Mathf.Lerp(gearRatios[Mathf.FloorToInt(currentGear) - 1], gearRatios[Mathf.CeilToInt(currentGear) - 1], currentGear - Mathf.Floor(currentGear));
        if (InReverse) gearRatio = -1.0f * gearRatios[0];

        //calc engine RPM from wheel rpm
        float wheelsRPM = (axles[1].right.rpm + axles[1].left.rpm) / 2f;
        if (wheelsRPM < 0)
            wheelsRPM = 0;

        // if the engine is on, the fuel injectors are going to be triggered at minRPM
        // to keep the engine running.  If the engine is OFF, then the engine will eventually
        // go all the way down to 0, because there's nothing keeping it spinning.
        var minPossibleRPM = ignitionStatus == IgnitionStatus.On ? minRPM : 0.0f;
        currentRPM = Mathf.Lerp(currentRPM, minPossibleRPM + (wheelsRPM * finalDriveRatio * gearRatio), Time.fixedDeltaTime * RPMSmoothness);
        // I don't know why, but logging RPM while engine is off and we're not moving, is showing
        // a constant drift between 0.0185 and 0.0192 or so .. so just clamp it down to 0 at that
        // point.
        if (currentRPM < 0.02f)
        {
            currentRPM = 0.0f;
        }

        //find out which wheels are on the ground
        foreach (var axle in axles)
        {
            axle.isGroundedLeft = axle.left.GetGroundHit(out axle.hitLeft);
            axle.isGroundedRight = axle.right.GetGroundHit(out axle.hitRight);
        }

        //convert inputs to torques
        float steer = maxSteeringAngle * steerInput;
        currentTorque = rpmCurve.Evaluate(currentRPM / maxRPM) * gearRatio * finalDriveRatio * tractionControlAdjustedMaxTorque;

        foreach (var axle in axles)
        {
            if (axle.steering)
            {
                axle.left.steerAngle = steer;
                axle.right.steerAngle = steer;
            }
        }

        if (handbrakeApplied)
        {
            //Make the accellInput negative so that brakes are applied in ApplyTorque()
            accellInput = -1.0f;
        }

        // No autodrive while engine is off.
        if (ignitionStatus == IgnitionStatus.On)
        {
            AutoSteer();
        }

        ApplyTorque();
        TractionControl();

        //shift if need be. No auto shifting while engine is off.
        if (ignitionStatus == IgnitionStatus.On)
        {
            AutoGearBox();
        }

        //record current speed in MPH
        CurrentSpeed = rb.velocity.magnitude * 2.23693629f;

        float deltaDistance = wheelsRPM / 60.0f * (axles[1].left.radius * 2.0f * Mathf.PI) * Time.fixedDeltaTime;
        Odometer += deltaDistance;

        /*
        // why does this not work :(
        float currentRPS = currentRPM / 60.0f;

        float accel = Mathf.Max(0.0f, accellInput);
        float angularV = currentRPS * Mathf.PI * 2.0f;

        float power = currentTorque //(rpmCurve.Evaluate(currentRPM / maxRPM) * accel * maxMotorTorque)  //Nm
            * angularV; // Watt

        float energy = power * Time.fixedDeltaTime  // w/s
            / 1000.0f * 3600.0f;                    // kw/h

        print("p:" + power + " e:" + energy);
        //approximation function for
        // https://en.wikipedia.org/wiki/Brake_specific_fuel_consumption#/media/File:Brake_specific_fuel_consumption.svg
        // range ~ 200-400 g/kWh
        float bsfc = (206.0f + Mathf.Sqrt(Mathf.Pow(currentRPM - 2200, 2.0f)
                            + Mathf.Pow((accel - 0.9f) * 10000.0f, 2.0f)) / 80 + currentRPM / 4500); // g/kWh

        float gasolineDensity = 1f / .75f;      // cm^3/g

        float deltaConsumption = bsfc * energy  // g
            * gasolineDensity                   // cm^3
            / 1000.0f;                          // l
        */

        // FIXME fix the more correct method above...
        float deltaConsumption = currentRPM * 0.00000001f
                               + currentTorque * 0.000000001f * Mathf.Max(0.0f, accellInput);

        // if engine is not powered up, or there's zero acceleration, AND we're not idling
        // the engine to keep it on, then the fuel injectors are off, and no fuel is being used
        // idling == non-scientific calculation of "minRPM + 25%".
        if (ignitionStatus != IgnitionStatus.On || (accellInput <= 0.0f && currentRPM > minRPM + (minRPM * 0.25)))
        {
            deltaConsumption = 0.0f;
        }

        ConsumptionDistance = deltaConsumption / deltaDistance;     // l/m
        ConsumptionTime = deltaConsumption / Time.fixedDeltaTime;   // l/s

        fuelLevel -= deltaConsumption;

        float engineWeight = 200.0f; // kg
        float energyDensity = 34.2f * 1000.0f * 1000.0f; // J/l fuel
        float specificHeatIron = 448.0f; // J/kg for 1K temperature increase

        engineTemperatureK += (deltaConsumption * energyDensity) / (specificHeatIron * engineWeight);

        float coolFactor = 0.00002f; //ambient exchange
        if (engineTemperatureK > zeroK + 90.0f && !coolingMalfunction)
            coolFactor += 0.00002f + 0.0001f * Mathf.Max(0.0f, CurrentSpeed); // working temperature reached, start cooling

        engineTemperatureK = Mathf.Lerp(engineTemperatureK, ambientTemperatureK, coolFactor);

        //find current road surface type
        WheelHit hit;
        if (axles[0].left.GetGroundHit(out hit))
        {
            traction = hit.forwardSlip;
            var roadObject = hit.collider.transform.parent == null ? hit.collider.transform : hit.collider.transform.parent;
            CurrentSurface = roadType == "roads" || roadObject.CompareTag("Road") ? RoadSurface.Tarmac : RoadSurface.Offroad;
        }
        else
        {
            traction = 0f;
            CurrentSurface = RoadSurface.Airborne;
        }

        //find traction values for audio
        tractionR = axles[0].right.GetGroundHit(out hit) ? hit.forwardSlip : 0f;
        rtractionR = axles[1].right.GetGroundHit(out hit) ? hit.forwardSlip : 0f;
        rtraction = axles[1].left.GetGroundHit(out hit) ? hit.forwardSlip : 0f;

        // apply turn signal logic
        const float turnSignalTriggerThreshold = 0.2f;
        const float turnSignalOffThreshold = 0.1f;

        if (leftTurnSignal)
        {
            if (resetTurnSignal)
            {
                if (steerInput > -turnSignalOffThreshold)
                {
                    leftTurnSignal = false;
                }
            }
            else
            {
                if (steerInput < -turnSignalTriggerThreshold)
                {
                    // "click"
                    resetTurnSignal = true;
                }
            }
        }

        if (rightTurnSignal)
        {
            if (resetTurnSignal)
            {
                if (steerInput < turnSignalOffThreshold)
                {
                    rightTurnSignal = false;
                }
            }
            else
            {
                if (steerInput > turnSignalTriggerThreshold)
                {
                    // "click"
                    resetTurnSignal = true;
                }
            }
        }
    }
    #endregion

    #region light methods
    public void SetRange(float newRange)
    {
        left.range = newRange;
        right.range = newRange;
    }

    public bool GetState()
    {
        if (left)
            return left.gameObject.activeInHierarchy;
        return false;
    }

    public bool Headlights
    {
        get { return left.enabled; }
        set
        {
            left.enabled = value;
            right.enabled = value;
            left.gameObject.SetActive(value);
            right.gameObject.SetActive(value);
        }
    }

    public bool Reverselights
    {
        get { return left_tail.enabled; }
        set
        {
            //left_tail.enabled = value;
            //right_tail.enabled = value;
            //left_tail.gameObject.SetActive(value);
            //right_tail.gameObject.SetActive(value);
        }
    }

    public void SetMode(LightMode lm)
    {
        lightMode = lm;
    }
    #endregion

    #region vcontroller
    public void RecalcDrivingWheels()
    {
        //calculate how many wheels are driving
        numberOfDrivingWheels = axles.Where(a => a.motor).Count() * 2;
    }

    private void AutoGearBox()
    {
        //check delay so we cant shift up/down too quick
        //FIXME lock gearbox for certain amount of time if user did override
        if (Time.time - lastShift > shiftDelay)
        {
            //shift up
            if (currentRPM / maxRPM > shiftUpCurve.Evaluate(accellInput) && Mathf.RoundToInt(currentGear) < gearRatios.Length)
            {
                //don't shift up if we are just spinning in 1st
                if (Mathf.RoundToInt(currentGear) > 1 || CurrentSpeed > 15f)
                {
                    GearboxShiftUp();
                }
            }
            //else down
            if (currentRPM / maxRPM < shiftDownCurve.Evaluate(accellInput) && Mathf.RoundToInt(currentGear) > 1)
            {
                GearboxShiftDown();
            }

        }

        if (IsShifting)
        {
            float lerpVal = (Time.time - lastShift) / shiftTime;
            currentGear = Mathf.Lerp(lastGear, Gear, lerpVal);
            if (lerpVal >= 1f)
                IsShifting = false;
        }

        //clamp to gear range
        if (currentGear >= gearRatios.Length)
        {
            currentGear = gearRatios.Length - 1;
        }
        else if (currentGear < 1)
        {
            currentGear = 1;
        }
    }

    // Ignition 3-way conditional toggle:
    // if off, if user has brake down, or the engine is turning (coasting) go straight to Start, else go to On
    // if on, go to Start
    // if Start, go to Off
    public void ToggleIgnition()
    {
        switch (ignitionStatus)
        {
            case IgnitionStatus.Off:
                StartEngine();
                //if (accellInput < 0.0f || currentRPM > 1.0f)
                //{
                //    StartIgnition();
                //}
                //else
                //{
                //    IgnitionOn();
                //}
                break;
            //case IgnitionStatus.On:
            //    StartIgnition();
            //    break;
            case IgnitionStatus.On:
                StopEngine();
                break;
            default:
                Debug.LogWarning("**** Somehow reached default in ToggleIgnition");
                break;
        }
    }

    // call AudioController if available to play ignition sounds, which will call back to StartEngine to start the engine.
    //public void StartIgnition()
    //{
    //    if (ignitionStatus == IgnitionStatus.EngineRunning) return;
    //    StartEngine();
    //}

    // You probably want to call StartIgnition(), so that the ignition startup sounds are played
    // before the engine actually starts.
    public void StartEngine()
    {
        ignitionStatus = IgnitionStatus.On;
    }

    public void StopEngine()
    {
        ignitionStatus = IgnitionStatus.Off;
    }

    // "On" or "Accessory" state without starting engine.
    //public void IgnitionOn()
    //{
    //    ignitionStatus = IgnitionStatus.On;
    //}

    public void ToggleCruiseMode()
    {
        if (driveMode == DriveMode.Cruise)
        {
            driveMode = DriveMode.Controlled;
        }
        else
        {
            driveMode = DriveMode.Cruise;
            cruiseTargetSpeed = CurrentSpeed;
            accellInput = 0.0f;
        }
    }

    public void ForceReset()
    {
        currentGear = 1;
        currentRPM = 0.0f;
        CurrentSpeed = 0.0f;
        currentTorque = 0.0f;
        accellInput = 0.0f;
    }

    public void EnableLeftTurnSignal()
    {
        if (leftTurnSignal)
        {
            leftTurnSignal = false;
        }
        else
        {
            rightTurnSignal = false;
            leftTurnSignal = true;
            resetTurnSignal = false;
        }
    }

    public void EnableRightTurnSignal()
    {
        if (rightTurnSignal)
        {
            rightTurnSignal = false;
        }
        else
        {
            rightTurnSignal = true;
            leftTurnSignal = false;
            resetTurnSignal = false;
        }
    }

    public void GearboxShiftUp()
    {
        //Debug.Log("GearboxShiftUp");
        if (InReverse)
        {
            InReverse = false;
            Reverselights = InReverse;
            return;
        }
        lastGear = Mathf.RoundToInt(currentGear);
        Gear = lastGear + 1;
        lastShift = Time.time;
        IsShifting = true;
    }

    public void GearboxShiftDown()
    {
        //Debug.Log("GearboxShiftDown");
        if (Mathf.RoundToInt(currentGear) == 1)
        {
            InReverse = true;
            driveMode = DriveMode.Controlled;
            Reverselights = InReverse;
            return;
        }

        lastGear = Mathf.RoundToInt(currentGear);
        Gear = lastGear - 1;
        lastShift = Time.time;
        IsShifting = true;
    }

    public void EnableHandbrake()
    {
        handbrakeApplied = !handbrakeApplied;
    }


    public void ChangeHeadlightMode()
    {
        if (!GetState())
        { // if not on, then turn on, otherwise toggle high/low beam
            Headlights = true;
            SetMode(LightMode.LOWBEAM);
            headlightMode = 1;
        }
        else
        {
            if (lightMode == LightMode.LOWBEAM)
            {
                SetMode(LightMode.HIGHBEAM);
                headlightMode = 2;
            }
            else
            {
                Headlights = false;
                headlightMode = 0;
            }
        }
    }

    public void ForceHeadlightsOn()
    {
        Headlights = true;
        headlightMode = 1;
    }

    public void ForceHeadlightsOff()
    {
        Headlights = false;
        headlightMode = 0;
    }

    private void AutoSteer()
    {
        //bail if a wheel isn't on the ground
        foreach (var axle in axles)
        {
            if (axle.isGroundedLeft == false || axle.isGroundedRight == false)
                return;
        }

        var yawRate = oldRotation - transform.eulerAngles.y;

        //don't adjust if the yaw rate is super high
        if (Mathf.Abs(yawRate) < 10f)
            rb.velocity = Quaternion.AngleAxis(yawRate * autoSteerAmount, Vector3.up) * rb.velocity;

        oldRotation = transform.eulerAngles.y;
    }

    private void ApplyTorque()
    {
        // acceleration is ignored when engine is not running, brakes are available still.
        if (accellInput >= 0)
        {
            //motor
            float torquePerWheel = ignitionStatus == IgnitionStatus.On ? accellInput * (currentTorque / numberOfDrivingWheels) : 0f;
            foreach (var axle in axles)
            {
                if (axle.motor)
                {
                    if (axle.isGroundedLeft)
                        axle.left.motorTorque = torquePerWheel;
                    else
                        axle.left.motorTorque = 0f;

                    if (axle.isGroundedRight)
                        axle.right.motorTorque = torquePerWheel;
                    else
                        axle.left.motorTorque = 0f;
                }

                axle.left.brakeTorque = 0f;
                axle.right.brakeTorque = 0f;
            }

        }
        // TODO: to get brake + accelerator working at the same time, modify this area.
        // You'll need to do some work to separate the brake and accel pedal inputs, though.
        // TODO: handBrake should apply full braking to rear axle (possibly all axles), without
        // changing the accelInput
        else
        {
            //brakes
            foreach (var axle in axles)
            {
                var brakeTorque = maxBrakeTorque * accellInput * -1 * axle.brakeBias;
                axle.left.brakeTorque = brakeTorque;
                axle.right.brakeTorque = brakeTorque;
                axle.left.motorTorque = 0f;
                axle.right.motorTorque = 0f;
            }
        }
    }

    private void TractionControl()
    {
        foreach (var axle in axles)
        {
            if (axle.motor)
            {
                if (axle.left.isGrounded)
                    AdjustTractionControlTorque(axle.hitLeft.forwardSlip);

                if (axle.right.isGrounded)
                    AdjustTractionControlTorque(axle.hitRight.forwardSlip);
            }
        }
    }

    private void AdjustTractionControlTorque(float forwardSlip)
    {
        if (forwardSlip >= tractionControlSlipLimit && tractionControlAdjustedMaxTorque >= 0)
        {
            tractionControlAdjustedMaxTorque -= 10 * tractionControlAmount;
            if (tractionControlAdjustedMaxTorque < 0)
                tractionControlAdjustedMaxTorque = 0f;
        }
        else
        {
            tractionControlAdjustedMaxTorque += 10 * tractionControlAmount;
            if (tractionControlAdjustedMaxTorque > maxMotorTorque)
                tractionControlAdjustedMaxTorque = maxMotorTorque;
        }
    }

    void StreamTextureChanged(Texture texture, float x, float y)
    {
    }

    private void ProcessIncomingCommandFlags()
    {
        if (commandFlags.headlightsOn)
        {
            ForceHeadlightsOn();
        }
        if (commandFlags.headlightsOff)
        {
            ForceHeadlightsOff();
        }
        if (commandFlags.remoteStart)
        {
            StartEngine();
        }

        // Make sure that you reset all values to their defaults as soon as you are done with them,
        // otherwise they will be handled again on the next update!
        commandFlags.headlightsOff = false;
        commandFlags.headlightsOn = false;
        commandFlags.remoteStart = false;
        commandFlags.centerConsoleStreamSource = null;
    }

    private void ApplyLocalPositionToVisuals(WheelCollider collider, GameObject visual)
    {
        Transform visualWheel = visual.transform;

        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);

        visualWheel.transform.position = position;
        visualWheel.transform.rotation = rotation;
    }

    public override void SetWheelScale(float value)
    {
    }

    public override void ResetPosition()
    {
        rb.position = initialPosition;
        rb.rotation = initialRotation;
    }

    public override void ResetSavedPosition(Vector3 pos, Quaternion rot)
    {
        rb.position = pos == Vector3.zero ? initialPosition : pos;
        rb.rotation = rot == Quaternion.identity ? initialRotation : rot;
    }
    #endregion

    #region cInputController
    private void ApplyAIInput()
    {
        float k = 0.4f + 0.6f * CurrentSpeed / 30.0f;
        //float steerPow = 1.0f + Mathf.Min(1.0f, k);

        if (driveMode == DriveMode.Controlled)
        {
            accellInput = AccelBrakeInput < 0.0f ? AccelBrakeInput : AccelBrakeInput * Mathf.Min(1.0f, k);
        }
        //convert inputs to torques
        steerInput = SteerInput; // Mathf.Sign(steerInput) * Mathf.Pow(Mathf.Abs(steerInput), steerPow);

        simLinVel = rb.velocity;
        simAngVel = rb.angularVelocity;

        projectedLinVec = Vector3.Project(simLinVel, transform.forward);
        actualLinVel = projectedLinVec.magnitude * (Vector3.Dot(simLinVel, transform.forward) > 0 ? 1.0f : -1.0f);

        projectedAngVec = Vector3.Project(simAngVel, transform.up);
        actualAngVel = projectedAngVec.magnitude * (projectedAngVec.y > 0 ? -1.0f : 1.0f);
    }
    #endregion

    #region cruise
    public float sensitivity = 1.0f;
    //accel input range from -1 to 1
    public float GetAccel(float currentSpeed, float targetSpeed, float deltaTime)
    {
        return Mathf.Clamp((targetSpeed - currentSpeed) * deltaTime * sensitivity * 20f, -1f, 1f);
    }
    #endregion

    #region scenario
    private void GetLane()
    {
        if (!isScenario) return;

        int layerMask = 1 << 13;

        RaycastHit hit;
        if (Physics.Raycast(transform.position + new Vector3(0f, 1f, 0f), -transform.up, out hit, Mathf.Infinity, layerMask))
        {
            RoadSegmentDataComponent tempC;
            if (tempC = hit.collider.gameObject.GetComponent<RoadSegmentDataComponent>())
            {
                currentLane = tempC.lane;
                // TODO if mesh is rotated this is opposite so needs adj
                if (tempC.transform.InverseTransformPoint(hit.point).x < -0.01)
                    lanePosition = LanePositionType.Left;
                else if (tempC.transform.InverseTransformPoint(hit.point).x > 0.01)
                    lanePosition = LanePositionType.Right;
                else
                    lanePosition = LanePositionType.Center;
            }
        }
    }

    public void LaneChange(int dir = 0)
    {
        // 1 left -1 right
        if (!isScenario) return;
        if (currentLane != laneChangeStartLane + dir)
        {
            SteerInput = -dir * 0.005f;
            //Debug.Log("Turn");
        }
        else if (rb.velocity.x != 0f)
        {
            SteerInput = -Mathf.Sign(rb.velocity.x) * 0.025f;
            if (rb.velocity.x > -0.0001 && rb.velocity.x < 0.0001)
                rb.velocity = new Vector3(0f, 0f, rb.velocity.z);
        }
        else
        {
            //Debug.Log("Finished");

            SteerInput = 0;
            isLaneChange = false;
        }
        //Debug.Log("Start Lane: " + laneChangeStartLane + " Lane to get to: " + (laneChangeStartLane + dir).ToString() + "Lane: " + currentLane + " LanePos: " + lanePosition + " velX: " + rb.velocity.x);
    }

    public void CenterStraight()
    {
        if (!isScenario) return;

        if (rb.velocity.x != 0f)
        {
            SteerInput = -Mathf.Sign(rb.velocity.x) * 0.025f;
            if (rb.velocity.x > -0.0001 && rb.velocity.x < 0.0001)
                rb.velocity = new Vector3(0f, 0f, rb.velocity.z);
        }

        if (lanePosition != LanePositionType.Center)
        {
            if (lanePosition == LanePositionType.Left)
            {
                SteerInput = 1 * 0.001f;
            }
            else if (lanePosition == LanePositionType.Right)
            {
                SteerInput = -1 * 0.001f;
            }
            else
            {
                if (rb.velocity.x != 0f)
                {
                    SteerInput = -Mathf.Sign(rb.velocity.x) * 0.025f;
                    if (rb.velocity.x > -0.0001 && rb.velocity.x < 0.0001)
                        rb.velocity = new Vector3(0f, 0f, rb.velocity.z);
                }
            }
        }
    }

    public void GetDistanceToTestVehicle()
    {
        if (!isScenario) return;
        if (VehicleManager.Instance.currentTestVehicle == null) return;

        distToTestVehicle = Vector3.Distance(VehicleManager.Instance.currentTestVehicle.transform.position, this.transform.position);
        
    }

    public void GetDot()
    {
        if (!isScenario) return;

        dot = Vector3.Dot(VehicleManager.Instance.currentTestVehicle.transform.forward, VehicleManager.Instance.currentTestVehicle.transform.InverseTransformPoint(this.transform.position));
        //Debug.Log(dot);
    }

    public void CheckForEvent()
    {
        if (!isScenario) return;

        for (int i = 0; i < DataManager.Instance.storyEvents.Count; i++)
        {
            if (DataManager.Instance.storyEvents[i].eventName == "MyLaneChangeLeftEvent")
            {
                float tempDist;
                int tempDir;
                if (float.TryParse(DataManager.Instance.storyEvents[i].eventActionConditionEntityDistanceValue, out tempDist) && int.TryParse(DataManager.Instance.storyEvents[i].storyboardStoryActManeuverEventActions[0].actionLateralTypeTargetValue, out tempDir))
                {
                    if (distToTestVehicle <= tempDist && dot < 0)
                    {
                        laneChange = tempDir;
                        isLaneChange = true;
                        DataManager.Instance.storyEvents.RemoveAt(i);
                    }
                }
            }

            if (DataManager.Instance.storyEvents[i].eventName == "MyLaneChangeRightEvent")
            {
                float tempDist;
                int tempDir;
                if (float.TryParse(DataManager.Instance.storyEvents[i].eventActionConditionEntityDistanceValue, out tempDist) && int.TryParse(DataManager.Instance.storyEvents[i].storyboardStoryActManeuverEventActions[0].actionLateralTypeTargetValue, out tempDir))
                {
                    if (distToTestVehicle >= tempDist && dot > 0)
                    {
                        laneChange = tempDir;
                        isLaneChange = true;
                        DataManager.Instance.storyEvents.RemoveAt(i);
                    }
                }
            }
        }
    }
    #endregion
}



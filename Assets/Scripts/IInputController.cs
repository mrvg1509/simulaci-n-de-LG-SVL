/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */


using System;

interface IInputController
{
    event Action<InputEvent> TriggerDown;
    event Action<InputEvent> TriggerPress;
    event Action<InputEvent> TriggerRelease;

    float SteerInput { get; }
    float AccelBrakeInput { get; }

    void Init();
    void OnUpdate();
    void CleanUp();
    void Enable();
    void Disable();
}

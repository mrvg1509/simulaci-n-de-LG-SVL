/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;
using System.Collections;

public class OnBecameVisiblePass : MonoBehaviour
{
    public System.Action<bool> onVisbilityChange;

    void OnBecameInvisible()
    {
        onVisbilityChange(false);
    }

    void OnBecameVisible()
    {
        onVisbilityChange(true);
    }
}

/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HDMapSignalLightBuilder)), CanEditMultipleObjects]
public class HDMapSignalLightBuilderEditor : MapSignalLightBuilderEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }

    protected override void OnSceneGUI()
    {
        base.OnSceneGUI();
    }
}

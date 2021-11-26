/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using UnityEditor;
using UnityEditor.AnimatedValues;

// For manteinance, every new [SerializeField] variable in Scroller must be declared here

namespace FancyScrollView
{
    [CustomEditor(typeof(Scroller))]
    [CanEditMultipleObjects]
    public class ScrollerEditor : Editor
    {
        private SerializedProperty viewport;
        private SerializedProperty scrollDirection;
        private SerializedProperty movementType;
        private SerializedProperty elasticity;
        private SerializedProperty scrollSensitivity;
        private SerializedProperty inertia;
        private SerializedProperty decelerationRate;
        private SerializedProperty snap;
        private SerializedProperty draggable;
        private SerializedProperty scrollbar;
        private SerializedProperty movementLimitation;

        private AnimBool showElasticity;
        private AnimBool showInertiaRelatedValues;

        private void OnEnable()
        {
            viewport = serializedObject.FindProperty("viewport");
            scrollDirection = serializedObject.FindProperty("scrollDirection");
            movementType = serializedObject.FindProperty("movementType");
            elasticity = serializedObject.FindProperty("elasticity");
            scrollSensitivity = serializedObject.FindProperty("scrollSensitivity");
            inertia = serializedObject.FindProperty("inertia");
            decelerationRate = serializedObject.FindProperty("decelerationRate");
            snap = serializedObject.FindProperty("snap");
            draggable = serializedObject.FindProperty("draggable");
            scrollbar = serializedObject.FindProperty("scrollbar");
            movementLimitation = serializedObject.FindProperty("movementLimitation");

            showElasticity = new AnimBool(Repaint);
            showInertiaRelatedValues = new AnimBool(Repaint);
            SetAnimBools(true);
        }

        private void OnDisable()
        {
            showElasticity.valueChanged.RemoveListener(Repaint);
            showInertiaRelatedValues.valueChanged.RemoveListener(Repaint);
        }

        private void SetAnimBools(bool instant)
        {
            SetAnimBool(showElasticity, !movementType.hasMultipleDifferentValues && movementType.enumValueIndex == (int)MovementType.Elastic, instant);
            SetAnimBool(showInertiaRelatedValues, !inertia.hasMultipleDifferentValues && inertia.boolValue, instant);
        }

        private void SetAnimBool(AnimBool a, bool value, bool instant)
        {
            if (instant)
            {
                a.value = value;
            }
            else
            {
                a.target = value;
            }
        }

        public override void OnInspectorGUI()
        {
            SetAnimBools(false);

            serializedObject.Update();
            EditorGUILayout.PropertyField(viewport);
            EditorGUILayout.PropertyField(scrollDirection);
            EditorGUILayout.PropertyField(movementType);
            DrawMovementTypeRelatedValue();
            EditorGUILayout.PropertyField(scrollSensitivity);
            EditorGUILayout.PropertyField(inertia);
            DrawInertiaRelatedValues();
            EditorGUILayout.PropertyField(snap);
            EditorGUILayout.PropertyField(movementLimitation);
            EditorGUILayout.PropertyField(draggable);
            EditorGUILayout.PropertyField(scrollbar);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMovementTypeRelatedValue()
        {
            using var group = new EditorGUILayout.FadeGroupScope(showElasticity.faded);
            if (!group.visible)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(elasticity);
            }
        }

        private void DrawInertiaRelatedValues()
        {
            using var group = new EditorGUILayout.FadeGroupScope(showInertiaRelatedValues.faded);
            if (!group.visible)
            {
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(decelerationRate);
            }
        }
    }
}
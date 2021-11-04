using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Tilia;

[CustomEditor(typeof(TiliaPay))]
public class TiliaPayEditor : Editor
{
    SerializedProperty StagingEnvironment;

    SerializedProperty ProductionClientID;
    SerializedProperty ProductionClientSecret;
    SerializedProperty ProductionURI;
    SerializedProperty ProductionWidgetURL;

    SerializedProperty StagingClientID;
    SerializedProperty StagingClientSecret;
    SerializedProperty StagingURI;
    SerializedProperty StagingWidgetURL;
    SerializedProperty LoggingEnabled;

    SerializedProperty WebBrowser;

    public void OnEnable()
    {
        StagingEnvironment = serializedObject.FindProperty("StagingEnvironment");

        ProductionClientID = serializedObject.FindProperty("ProductionClientID");
        ProductionClientSecret = serializedObject.FindProperty("ProductionClientSecret");
        ProductionURI = serializedObject.FindProperty("ProductionURI");
        ProductionWidgetURL = serializedObject.FindProperty("ProductionWidgetURL");

        StagingClientID = serializedObject.FindProperty("StagingClientID");
        StagingClientSecret = serializedObject.FindProperty("StagingClientSecret");
        StagingURI = serializedObject.FindProperty("StagingURI");
        StagingWidgetURL = serializedObject.FindProperty("StagingWidgetURL");
        LoggingEnabled = serializedObject.FindProperty("LoggingEnabled");

        WebBrowser = serializedObject.FindProperty("WebBrowser");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var _TiliaPay = target as TiliaPay;

        EditorStyles.label.wordWrap = true;

        //EditorGUILayout.PropertyField(StagingEnvironment);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current Environment: " + (_TiliaPay.StagingEnvironment ? "STAGING" : "PRODUCTION"), EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (_TiliaPay.StagingEnvironment)
        {
            EditorGUILayout.LabelField("The TiliaPay SDK is currently operating on the staging / sandbox environment. You should use the staging environment for developing and testing your application.");
            EditorGUILayout.Space();
            if (GUILayout.Button("Switch to Production environment"))
            {
                _TiliaPay.StagingEnvironment = false;
            }
            EditorGUILayout.PropertyField(StagingClientID);
            EditorGUILayout.PropertyField(StagingClientSecret);
            EditorGUILayout.PropertyField(StagingURI);
            EditorGUILayout.PropertyField(StagingWidgetURL);
            EditorGUILayout.PropertyField(LoggingEnabled);
        }
        else
        {
            EditorGUILayout.LabelField("The TiliaPay SDK is currently operating on the production environment. Only use this environment when you are ready to deploy your application for real use.");
            EditorGUILayout.Space();
            if (GUILayout.Button("Switch to Staging environment"))
            {
                _TiliaPay.StagingEnvironment = true;
            }
            EditorGUILayout.PropertyField(ProductionClientID);
            EditorGUILayout.PropertyField(ProductionClientSecret);
            EditorGUILayout.PropertyField(ProductionURI);
            EditorGUILayout.PropertyField(ProductionWidgetURL);
        }

        EditorGUILayout.PropertyField(WebBrowser);

        serializedObject.ApplyModifiedProperties();
    }
}

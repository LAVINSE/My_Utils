using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(MonoBehaviour), true)]
public class CustomEditorAttributeStyled : Editor
{
    private Dictionary<string, object[]> parameterValues = new Dictionary<string, object[]>();

    // 커스텀 스타일 정의
    private GUIStyle boxStyle;
    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;

    private void InitializeStyles()
    {
        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                //fontSize = 11,
                //fontStyle = FontStyle.Bold
            };
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        InitializeStyles();

        MonoBehaviour mono = (MonoBehaviour)target;
        var type = mono.GetType();
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        foreach (var method in methods)
        {
            var buttonAttributes = method.GetCustomAttributes(typeof(ButtonAttribute), false);
            if (buttonAttributes.Length > 0)
            {
                var buttonAttribute = buttonAttributes[0] as ButtonAttribute;

                if (buttonAttribute.Space > 0)
                {
                    GUILayout.Space(buttonAttribute.Space);
                }

                string buttonName = buttonAttribute.DisplayName ?? method.Name;
                var parameters = method.GetParameters();

                // Action 매개변수가 있는 경우 처리
                bool hasUnsupportedParams = false;
                List<ParameterInfo> supportedParams = new List<ParameterInfo>();

                foreach (var param in parameters)
                {
                    if (IsActionType(param.ParameterType))
                    {
                        continue;
                    }
                    else if (IsSupportedType(param.ParameterType))
                    {
                        supportedParams.Add(param);
                    }
                    else
                    {
                        hasUnsupportedParams = true;
                    }
                }

                if (hasUnsupportedParams)
                {
                    continue;
                }

                // 커스텀 스타일 박스로 전체를 감싸기
                EditorGUILayout.BeginVertical(boxStyle);

                if (supportedParams.Count > 0)
                {
                    if (!parameterValues.ContainsKey(method.Name))
                    {
                        parameterValues[method.Name] = new object[parameters.Length];
                    }

                    // 헤더 스타일로 메소드 이름 표시
                    EditorGUILayout.LabelField(method.Name, headerStyle);

                    // 구분선
                    EditorGUILayout.Space(3);
                    var rect = EditorGUILayout.GetControlRect(false, 1);
                    EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                    EditorGUILayout.Space(5);

                    // 매개변수 입력 필드들
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        var param = parameters[i];

                        if (IsActionType(param.ParameterType))
                        {
                            parameterValues[method.Name][i] = null;
                            continue;
                        }

                        if (!IsSupportedType(param.ParameterType))
                        {
                            parameterValues[method.Name][i] = GetDefaultValue(param.ParameterType);
                            continue;
                        }

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(param.Name, GUILayout.Width(80));

                        if (param.ParameterType == typeof(int))
                        {
                            parameterValues[method.Name][i] = EditorGUILayout.IntField((int)(parameterValues[method.Name][i] ?? 0));
                        }
                        else if (param.ParameterType == typeof(float))
                        {
                            parameterValues[method.Name][i] = EditorGUILayout.FloatField((float)(parameterValues[method.Name][i] ?? 0f));
                        }
                        else if (param.ParameterType == typeof(string))
                        {
                            parameterValues[method.Name][i] = EditorGUILayout.TextField((string)(parameterValues[method.Name][i] ?? ""));
                        }
                        else if (param.ParameterType == typeof(bool))
                        {
                            parameterValues[method.Name][i] = EditorGUILayout.Toggle((bool)(parameterValues[method.Name][i] ?? false));
                        }

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.Space(2);
                    }

                    EditorGUILayout.Space(5);
                }
                else if (parameters.Length > 0)
                {
                    if (!parameterValues.ContainsKey(method.Name))
                    {
                        parameterValues[method.Name] = new object[parameters.Length];
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (IsActionType(parameters[i].ParameterType))
                            {
                                parameterValues[method.Name][i] = null;
                            }
                            else
                            {
                                parameterValues[method.Name][i] = GetDefaultValue(parameters[i].ParameterType);
                            }
                        }
                    }
                }

                // 커스텀 스타일 버튼
                if (GUILayout.Button(buttonName, buttonStyle, GUILayout.Height(35f)))
                {
                    if (parameters.Length > 0)
                    {
                        try
                        {
                            method.Invoke(mono, parameterValues[method.Name]);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error invoking {method.Name}: {e.Message}");
                        }
                    }
                    else
                    {
                        method.Invoke(mono, null);
                    }
                }

                EditorGUILayout.EndVertical();
                //EditorGUILayout.Space(8);
            }
        }
    }

    private bool IsActionType(Type type)
    {
        if (type == typeof(Action))
            return true;

        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            return genericTypeDef == typeof(Action<>) ||
                   genericTypeDef == typeof(Action<,>) ||
                   genericTypeDef == typeof(Action<,,>) ||
                   genericTypeDef == typeof(Action<,,,>);
        }

        return false;
    }

    private bool IsSupportedType(Type type)
    {
        return type == typeof(int) ||
               type == typeof(float) ||
               type == typeof(string) ||
               type == typeof(bool);
    }

    private object GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }
}

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif // UNITY_EDITOR

[AttributeUsage(AttributeTargets.Method)]
public class ButtonAttribute : PropertyAttribute
{
    public string DisplayName { get; private set; }
    public float Space { get; private set; }

    public ButtonAttribute(string displayName = null)
    {
        DisplayName = displayName;
        Space = 0f;
    }

    public ButtonAttribute(float space)
    {
        DisplayName = null;
        Space = space;
    }

    public ButtonAttribute(string displayName, float space)
    {
        DisplayName = displayName;
        Space = space;
    }
}

[System.AttributeUsage(System.AttributeTargets.Field)]
public class ReadOnlyAttribute : PropertyAttribute { }
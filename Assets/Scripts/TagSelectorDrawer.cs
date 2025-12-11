using UnityEngine;
using UnityEditor;

public class TagSelectorAttribute : PropertyAttribute { }

[CustomPropertyDrawer(typeof(TagSelectorAttribute))]
public class TagSelectorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.String)
        {
            EditorGUI.BeginProperty(position, label, property);

            string[] tags = UnityEditorInternal.InternalEditorUtility.tags;
            int index = Mathf.Max(0, System.Array.IndexOf(tags, property.stringValue));
            int newIndex = EditorGUI.Popup(position, label.text, index, tags);
            property.stringValue = tags[newIndex];
            EditorGUI.EndProperty();
        }
        else
        {
            EditorGUI.PropertyField(position, property, label);
        }
    }
}
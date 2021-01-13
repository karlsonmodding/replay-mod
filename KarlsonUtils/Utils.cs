using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace ReplayMod
{
    class Utils
    {
        public static void CreateNewButton(string Name, Transform parent, float x, float y, Vector3 scale, UnityAction action)
        {
            GameObject btn = UnityEngine.Object.Instantiate(GameObject.Find("Managers (1)").transform.Find("UI/Game/DeadUI/MenuBtn").gameObject, parent);
            btn.name = Name;
            btn.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = Name;
            btn.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().enableWordWrapping = false;
            btn.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().enableAutoSizing = true;
            btn.transform.localScale = scale;
            btn.transform.localEulerAngles = new Vector3(0, 0, 0);
            btn.transform.localPosition = new Vector3(0f + x * 185.102f, -125.965f + y * 83.976f, 0);
            btn.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
            btn.GetComponent<Button>().onClick.AddListener(action);
        }

        public static FieldInfo GetPrivate(string field, object script)
        {
            FieldInfo fieldInfo = script.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            return fieldInfo;
        }
    }
}

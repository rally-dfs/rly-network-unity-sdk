using System.Collections;

using UnityEngine;
using UnityEngine.UI;

namespace RlyNetwork.Example
{
    public class UIAutoRefresh : MonoBehaviour
    {
        IEnumerator Start()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }
}
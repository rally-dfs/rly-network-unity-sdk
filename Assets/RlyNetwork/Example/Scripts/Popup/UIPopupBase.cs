using System;

using TMPro;

using UnityEngine;

namespace RlyNetwork.Example
{
    public class UIPopupBase : MonoBehaviour
    {
        [SerializeField] protected TextMeshProUGUI titleText;
        [SerializeField] protected TextMeshProUGUI descriptionText;

        protected Action<string> action;

        public virtual void Initialize(string title, string description, Action<string> action)
        {
            titleText.text = title;
            descriptionText.text = description;
            this.action = action;
        }

        public virtual void OnButtonClicked(string id)
        {
            action?.Invoke(id);
            Close();
        }

        public virtual void Close()
        {
            PopupManager.Instance.ActivePopups.Remove(this);
            Destroy(gameObject);
        }
    }
}

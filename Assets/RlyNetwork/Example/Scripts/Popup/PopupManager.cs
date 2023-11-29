using System;
using System.Collections.Generic;

using UnityEngine;

namespace RlyNetwork.Example
{
    public enum PopupType
    {
        Information,
        AddToken,
        SendToken,
        TwoOptions
    }

    public class PopupManager : MonoBehaviour
    {
        [SerializeField] RectTransform popupBaseParent;

        [SerializeField] UIPopupBase informationPopupPrefab;
        [SerializeField] UIAddTokenPopup addTokenPopupPrefab;
        [SerializeField] UISendTokensPopup sendTokenPopupPrefab;
        [SerializeField] UITwoOptionsPopup twoOptionsPopupPrefab;

        public static PopupManager Instance { get; private set; }

        public List<UIPopupBase> ActivePopups { get; private set; } = new();

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void ShowPopup(PopupType type, string title, string description, Action<string> action)
        {
            UIPopupBase popup = null;
            switch (type)
            {
                case PopupType.Information:
                    popup = Instantiate(informationPopupPrefab, popupBaseParent);
                    break;
                case PopupType.AddToken:
                    popup = Instantiate(addTokenPopupPrefab, popupBaseParent);
                    break;
                case PopupType.SendToken:
                    popup = Instantiate(sendTokenPopupPrefab, popupBaseParent);
                    break;
                case PopupType.TwoOptions:
                    popup = Instantiate(twoOptionsPopupPrefab, popupBaseParent);
                    break;
            }

            ActivePopups.Add(popup);

            popup.Initialize(title, description, action);
        }

        public void CloseAllPopups()
        {
            foreach (var popup in ActivePopups)
                popup.Close();

            ActivePopups.Clear();
        }
    }
}

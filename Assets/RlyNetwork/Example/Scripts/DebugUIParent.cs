using UnityEngine;

namespace RlyNetwork.Example
{
    public class DebugUIParent : MonoBehaviour
    {
        [SerializeField] GameObject walletSDKPanel;
        [SerializeField] GameObject walletManagementPanel;

        public void OnWalletSDKValueChanged(bool state)
        {
            if (state)
            {
                walletManagementPanel.SetActive(false);
                walletSDKPanel.SetActive(true);
            }
        }

        public void OnWalletManagementValueChanged(bool state)
        {
            if (state)
            {
                walletSDKPanel.SetActive(false);
                walletManagementPanel.SetActive(true);
            }
        }
    }
}
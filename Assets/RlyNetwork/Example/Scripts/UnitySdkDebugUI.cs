using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace RlyNetwork.Example
{
    public class UnitySdkDebugUI : MonoBehaviour
    {
        public Button bundleIdButton;
        public TMP_Text bundleIdText;

        public TMP_InputField mnemonicInput;
        public Button saveMnemonicButton;

        public Button getMnemonicButton;
        public TMP_Text mnemonicText;

        public Button generateMnemonicButton;
        public TMP_Text generatedMnemonicText;

        public Button deleteMnemonicButton;

        public TMP_InputField privateKeyInputField;
        public Button getPrivateKeyButton;
        public TMP_Text privateKeyText;

        public void CopyTextToClipboard(TMP_Text text)
        {
            GUIUtility.systemCopyBuffer = text.text;
        }

        void Start()
        {
            bundleIdButton.onClick.AddListener(GetBundleId);
            saveMnemonicButton.onClick.AddListener(SaveMnemonic);
            getMnemonicButton.onClick.AddListener(GetMnemonic);
            generateMnemonicButton.onClick.AddListener(GenerateMnemonic);
            deleteMnemonicButton.onClick.AddListener(DeleteMnemonic);
            getPrivateKeyButton.onClick.AddListener(GetPrivateKey);
        }

        async void GetBundleId()
        {
            bundleIdText.text = await UnitySdkPlugin.GetBundleId();
        }

        async void SaveMnemonic()
        {
            await UnitySdkPlugin.SaveMnemonic(mnemonicInput.text, false, false);
        }

        async void GetMnemonic()
        {
            mnemonicText.text = await UnitySdkPlugin.GetMnemonic();
        }

        async void GenerateMnemonic()
        {
            generatedMnemonicText.text = await UnitySdkPlugin.GenerateNewMnemonic();
        }

        async void DeleteMnemonic()
        {
            await UnitySdkPlugin.DeleteMnemonic();
        }

        async void GetPrivateKey()
        {
            privateKeyText.text = await UnitySdkPlugin.GetPrivateKeyFromMnemonic(privateKeyInputField.text);
        }
    }
}
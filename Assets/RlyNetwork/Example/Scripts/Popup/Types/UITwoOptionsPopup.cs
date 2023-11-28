namespace RlyNetwork.Example
{
    public class UITwoOptionsPopup : UIPopupBase
    {
        public virtual void OnCancelButtonClicked()
        {
            action?.Invoke(string.Empty);
            Close();
        }
    }
}
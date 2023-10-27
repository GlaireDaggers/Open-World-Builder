namespace OpenWorldBuilder
{
    public class DialogBox
    {
        public string title;
        public string message;
        public string[] buttons;
        public Action<int>? callback;

        public DialogBox(string title, string message, string[] buttons, Action<int>? callback)
        {
            this.title = title;
            this.message = message;
            this.buttons = buttons;
            this.callback = callback;
        }
    }
}

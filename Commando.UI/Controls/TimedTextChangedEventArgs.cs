using System;

namespace twomindseye.Commando.UI.Controls
{
    sealed class TimedTextChangedEventArgs : EventArgs
    {
        public TimedTextChangedEventArgs(string text, int caretIndex)
        {
            Text = text;
            CaretIndex = caretIndex;
        }

        public string Text { get; private set; }
        public int CaretIndex { get; private set; }
    }
}
using System;

namespace Elise.Sequencing
{
    public class ProgressUpdatedEventArgs : EventArgs
    {
        public int Progress { get; }
        public bool Cancel { get; set; }

        public ProgressUpdatedEventArgs(int progress)
        {
            Progress = progress;
            Cancel = false;
        }
    }
}

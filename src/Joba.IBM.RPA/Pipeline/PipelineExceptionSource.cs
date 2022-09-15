namespace Joba.Pipeline
{
    /// <summary>
    /// Defines the exception structure of a pipeline.
    /// </summary>
    public sealed class PipelineExceptionSource
    {
        public PipelineExceptionSource(Exception ex)
        {
            Exception = ex;
            Handled = false;
        }

        public Exception Exception { get; }
        internal bool Handled { get; private set; }

        public void MarkAsHandled() => Handled = true;
    }
}

namespace Joba.IBM.RPA
{
    public class TemplateException : Exception
    {
        public TemplateException() { }
        public TemplateException(string message) : base(message) { }
        public TemplateException(string message, Exception inner) : base(message, inner) { }
        protected TemplateException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
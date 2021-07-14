using System;

namespace DocumentUploader.Exceptions
{

    [Serializable]
    public class ParameterIsRequired : BaseException
    {
        public ParameterIsRequired() { }
        public ParameterIsRequired(string message) : base(message) { }
        public ParameterIsRequired(string message, Exception inner) : base(message, inner) { }
        protected ParameterIsRequired(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}

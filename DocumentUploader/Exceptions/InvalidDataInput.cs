using System;

namespace DocumentUploader.Exceptions
{

    [Serializable]
    public class InvalidDataInput : BaseException
    {
        public InvalidDataInput() { }
        public InvalidDataInput(string message) : base(message) { }
        public InvalidDataInput(string message, Exception inner) : base(message, inner) { }
        protected InvalidDataInput(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}

using System;

namespace UnistadDocumentLibrary.Exceptions
{

    [Serializable]
    public class FileDuplicatedException : BaseException
    {
        public FileDuplicatedException() { }
        public FileDuplicatedException(string message) : base(message) { }
        public FileDuplicatedException(string message, Exception inner) : base(message, inner) { }
        protected FileDuplicatedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}

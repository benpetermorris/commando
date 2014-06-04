using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace twomindseye.Commando.API1
{
    // ensures serializability of most exception details
    [Serializable]
    public sealed class AssemblyDecoupledException : Exception
    {
        public AssemblyDecoupledException(Exception copyFrom) : base(copyFrom.Message)
        {
            ExceptionType = copyFrom.GetType().FullName;
            OriginalToString = copyFrom.ToString();
            OriginalStackTrace = copyFrom.StackTrace;
        }

        protected AssemblyDecoupledException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ExceptionType = info.GetString("ExceptionType");
            OriginalToString = info.GetString("OriginalToString");
            OriginalStackTrace = info.GetString("OriginalStackTrace");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        
            info.AddValue("ExceptionType", ExceptionType);
            info.AddValue("OriginalToString", OriginalToString);
            info.AddValue("OriginalStackTrace", OriginalStackTrace);
        }

        public override string ToString()
        {
            return OriginalToString;
        }

        public override string StackTrace
        {
            get
            {
                return OriginalStackTrace;
            }
        }

        public string ExceptionType { get; set; }
        public string OriginalToString { get; set; }
        public string OriginalStackTrace { get; set; }
    }
}

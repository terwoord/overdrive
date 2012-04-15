using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TerWoord.OverDriveStorage.Tests.TestUtilities
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ExpectedExceptionMessageAttribute : ExpectedExceptionBaseAttribute
    {
        public Type ExceptionType
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;
        }

        protected override void Verify(Exception exception)
        {
            if(ExceptionType!=null)
            {
                if(!ExceptionType.IsAssignableFrom(exception.GetType()))
                {
                    throw new Exception(string.Format("Exception expected of type '{0}', but exception of type '{1}' thrown!",
                                                      ExceptionType.FullName, exception.GetType().FullName), exception);
                }
            }

            if(exception.Message==Message)
            {
                return;
            }
            base.RethrowIfAssertException(exception);
            Assert.AreEqual(Message, exception.Message);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TerWoord.OverDriveStorage.Tests.TestUtilities
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ExpectedArgumentNullExceptionAttribute : ExpectedExceptionBaseAttribute
    {
        public string Argument
        {
            get;
            set;
        }
        protected override void Verify(Exception exception)
        {
            var xExceptionType = exception.GetType();
            var xArgumentNullException = exception as ArgumentNullException;
            if(xArgumentNullException!=null)
            {
                if(xArgumentNullException.ParamName==Argument)
                {
                    return;
                }
                throw new Exception("Wrong argument is null!");
            }
            base.RethrowIfAssertException(exception);
            throw new Exception(string.Format("Expected ArgumentNullException on argument '{0}'!", Argument));
        }
    }
}

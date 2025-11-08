using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Overlook
{
    internal static class AssertUtils
    {
        public static void CatchDebugAssertAndOtherExceptions(TestDelegate code, int assertionTimes = 1)
        {
#if UNITY_2022_3_OR_NEWER
            while (assertionTimes > 0)
            {
                UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Assert, new Regex(".*"));
                assertionTimes--;
            }
#endif
            Assert.Catch(code);
        }

        public static void CatchDebugAssert(TestDelegate code, int assertionTimes = 1)
        {
#if UNITY_2022_3_OR_NEWER
            while (assertionTimes > 0)
            {
                UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Assert, new Regex(".*"));
                assertionTimes--;
            }
            code();
#else
            Assert.Catch(code);
#endif
        }
    }
}

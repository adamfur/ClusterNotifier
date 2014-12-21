using System;

namespace Prislistan.Framework
{
    public class SystemTime
    {
        public static Func<DateTime> Method;

        static SystemTime()
        {
            Reset();
        }

        public static DateTime Now
        {
            get { return Method(); }
        }

        public static DateTime Today
        {
            get { return Now.Date; }
        }

        public static void Reset()
        {
            Method = () => DateTime.Now;
        }
    }
}
using System;

namespace Red.Wine.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class IgnoreWhileCopyAttribute : Attribute
    {
    }
}

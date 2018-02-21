using System;

namespace Red.Wine.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class CopyToAttribute : Attribute
    {
        public CopyToAttribute(Type navigationPropertyType, Relationship relationship, bool isForeignKey = false, string name = null)
        {
            To = navigationPropertyType;
            Name = name;
            Relationship = relationship;
            IsForeignKey = isForeignKey;
        }

        public CopyToAttribute(Relationship relationship, Type to, Type from, string name)
        {
            Relationship = relationship;
            To = to;
            From = from;
            Name = name;
        }

        public Type To { get; private set; }
        public Type From { get; private set; }
        public Relationship Relationship { get; private set; }
        public bool IsForeignKey { get; private set; }
        public string Name { get; private set; }
    }
}
